using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using UnityEngine.TestTools;

/// <summary>Two actual Photon clients, no Unity views. Not a cross-platform or UI test.</summary>
public class LSDFOnlineSmokeTests {
  [UnityTest]
  [Timeout(360000)] // Five real rounds plus replay/restore exceed Unity's default 180s.
  [Category("LSDFOnline")]
  public IEnumerator TwoClientsAgreeOnLoadoutCombatAndRepeatedAddPlayer() {
    yield return new EnterPlayMode();
    bool previousBackground = UnityEngine.Application.runInBackground;
    string error = null;
    bool canceled = false;
    UnityEngine.Application.runInBackground = true;
    using var runCancellation = new CancellationTokenSource();
    try {
      var task = RunOnline(runCancellation.Token);
      while (!task.IsCompleted) yield return null;
      error = task.IsFaulted ? task.Exception.ToString() : null;
      canceled = task.IsCanceled;
    } finally {
      runCancellation.Cancel(); // Also clean up if Test Runner aborts the iterator on an unhandled log.
      UnityEngine.Application.runInBackground = previousBackground;
    }
    yield return new ExitPlayMode();
    Assert.IsFalse(canceled, "Online test canceled before completion.");
    Assert.IsNull(error, error);
  }

  private static SessionRunner.Arguments RunnerArguments(RealtimeClient client, RuntimeConfig config,
      string id, CancellationToken token) {
    return new SessionRunner.Arguments {
      RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
      GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
      RuntimeConfig = config,
      SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
      GameMode = DeterministicGameMode.Multiplayer,
      PlayerCount = 2,
      Communicator = new QuantumNetworkCommunicator(client),
      RunnerId = id,
      ClientId = id,
      CancellationToken = token,
      StartGameTimeoutInSeconds = 60,
      RecordingFlags = RecordingFlags.Input | RecordingFlags.Checksums,
    };
  }

  private static async Task RunOnline(CancellationToken cancellation) {
    // Validate Quantum bootstrap before opening connections. The SDK expects a global LUT asset.
    QuantumRunnerUnityFactory.Init();
    if (!FPLut.IsLoaded)
      throw new InvalidOperationException("Quantum lookup tables are not loaded; check QuantumLookupTables.asset.");
    var clients = new RealtimeClient[2];
    var runners = new QuantumRunner[2];
    var starts = new Task<SessionRunner>[2];
    var samples = new[] { new Dictionary<int, ulong>(), new Dictionary<int, ulong>() };
    var ids = new[] { "LSDF-test-A-" + Guid.NewGuid(), "LSDF-test-B-" + Guid.NewGuid() };
    var room = "lsdf-smoke-" + Guid.NewGuid().ToString("N");
    var players = new[] {
      new RuntimePlayer { PlayerNickname = "LSDF-test-A", CommandSkills = new int[28] },
      new RuntimePlayer { PlayerNickname = "LSDF-test-B", CommandSkills = new int[28] }
    };
    // Config from the menu; avatar ID is a pinned fixture from LSDF_Menu's RuntimePlayer.
    var menuConfig = AssetDatabase.LoadAssetAtPath<Quantum.Menu.QuantumMenuConfig>(
      "Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
    var config = new QuantumUnityJsonSerializer().CloneConfig(menuConfig.AvailableScenes[0].RuntimeConfig);
    var simulation = QuantumUnityDB.GetGlobalAsset<SimulationConfig>(config.SimulationConfig);
    if (simulation == null) throw new InvalidOperationException("SimulationConfig is not available for the online test.");
    var previousSceneMode = simulation.AutoLoadSceneFromMap;
    config.Seed = 12345;
    players[0].PlayerAvatar.Id = new AssetGuid(1502267863199495006L);
    players[1].PlayerAvatar = players[0].PlayerAvatar;
    players[0].CommandSkills[4] = 2;
    players[1].CommandSkills[4] = 1;
    players[0].CommandSkills[0] = 1; // A: flash chain; B: jab-to-two chain.
    string observedError = null;
    bool damageObserved = false;
    int rosterSamples = 0;
    var leftUpperHits = new int[2];
    var jabHits = new int[2];
    var chainHits = new int[2];
    var timedHits = new int[2, 4]; // Narak first/second, Lars first/second.
    int combatStartTick = int.MaxValue;
    int bufferQuietTick = int.MaxValue;
    int bufferProbeTick = int.MaxValue;
    var bufferPendingSeen = new bool[2];
    var bufferDispatchTicks = new int[2];
    var bufferUpperSeen = new bool[2];
    int finishAttackTick = int.MaxValue;
    var match = new[] { new MatchObservation(), new MatchObservation() };
    IDisposable inputListener = null;
    IDisposable frameListener = null;
    IDisposable checksumListener = null;
    string stage = "prepare";
    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation)) {
      timeout.CancelAfter(TimeSpan.FromSeconds(300)); // Leave replay/restore time within the 360s outer test limit.
      try {
        // Simulation-only test: don't load Unity gameplay scenes into Test Runner's unnamed scene.
        // The setting is changed in memory, never saved, and restored after both runners stop.
        simulation.AutoLoadSceneFromMap = SimulationConfig.AutoLoadSceneFromMapMode.Disabled;
        var sourceSettings = PhotonServerSettings.Global.AppSettings;
        if (string.IsNullOrEmpty(sourceSettings.AppIdQuantum))
          throw new InvalidOperationException("NOT RUN: Quantum AppId is not configured.");
        for (int i = 0; i < clients.Length; ++i) {
          stage = "matchmaking client " + i;
          var settings = new AppSettings(sourceSettings) {
            AppVersion = "lsdf-smoke-" + LSDFLoadout.ProtocolVersion,
            FixedRegion = "asia"
          };
          clients[i] = new RealtimeClient();
          await MatchmakingExtensions.ConnectToRoomAsync(new MatchmakingArguments {
            PhotonSettings = settings,
            PluginName = "QuantumPlugin",
            MaxPlayers = 2,
            UserId = ids[i],
            RoomName = room,
            CanOnlyJoin = i != 0,
            IsRoomVisible = false,
            NetworkClient = clients[i],
            AsyncConfig = new AsyncConfig {
              TaskFactory = AsyncConfig.CreateUnityTaskFactory(),
              CancellationToken = timeout.Token,
              OperationTimeoutSec = 25
            }
          });
          // Matchmaking's service task stops on return. Start this client's runner now,
          // so it is serviced while the next client connects instead of sitting idle.
          starts[i] = SessionRunner.StartAsync(RunnerArguments(clients[i], config, ids[i], timeout.Token));
          TestContext.Progress.WriteLine("ONLINE: matched client " + i + "; starting its session.");
        }
        inputListener = QuantumCallback.SubscribeManual<CallbackPollInput>(c => {
          int owner = IndexOf(runners, c.Game);
          if (owner < 0) return;
          var input = new Quantum.Input();
          int relativeTick = c.Frame - combatStartTick;
          if (relativeTick < 0) {
            c.SetInput(input, DeterministicInputFlags.Repeatable);
            return;
          }
          if (c.Frame >= finishAttackTick) {
            // Alternate the attacker after each decisive round to reach 2-2, then 3-2.
            // Continue sending input during result/intro/final locks, without test-only damage.
            int completedRounds = 0;
            var verified = c.Game.Frames.Verified;
            if (verified != null && LSDFRoster.TryPair(verified, out var first, out var second))
              completedRounds = verified.Get<LSDF_Player>(first).loseRound + verified.Get<LSDF_Player>(second).loseRound;
            bool attacking = owner == completedRounds % 2;
            int phase = (c.Frame - finishAttackTick) % 90;
            input.Down = !attacking || phase < 3;
            input.Right = attacking;
            input.LeftPunch = attacking && phase < 3;
            c.SetInput(input, DeterministicInputFlags.Repeatable);
            return;
          }
          if (c.Frame >= bufferQuietTick && c.Frame < bufferProbeTick + 120) {
            int probePhase = c.Frame - bufferProbeTick;
            // A single LP starts a jab; a second, released down-forward LP arrives near its end.
            // B stays neutral so that damage from B cannot interrupt the probe's source attack.
            if (owner == 0) {
              input.LeftPunch = probePhase == 0 || probePhase == 28;
              input.Down = probePhase == 28;
              input.Right = probePhase == 28;
            }
            c.SetInput(input, DeterministicInputFlags.Repeatable);
            return;
          }
          if (relativeTick < 120) {
            bool localIsZero = false;
            foreach (var playerRef in c.Game.GetLocalPlayers()) {
              localIsZero = playerRef == (PlayerRef)0;
              break;
            }
            input.Right = localIsZero;
            input.Left = !localIsZero;
          } else {
            int phase = (relativeTick - 360 + owner * 12) % 720;
            // Jab chain -> LeftUpper -> NarakTwo -> LarsCutKick, with recovery gaps.
            bool upper = phase >= 240 && phase < 244;
            bool narak = phase >= 360 && phase < 364;
            bool lars = phase >= 540 && phase < 544;
            input.LeftPunch = phase < 3 || upper ||
              (owner == 0 && phase >= 8 && phase < 12);
            input.RightPunch = owner == 0 ? phase >= 30 && phase < 34 : phase >= 8 && phase < 12;
            input.Down = upper || narak;
            input.Right = upper;
            input.Left = narak;
            input.RightKick = narak;
            input.Up = lars;
            input.LeftKick = lars;
          }
          c.SetInput(input, DeterministicInputFlags.Repeatable);
        });
        frameListener = QuantumCallback.SubscribeManual<CallbackSimulateFinished>(c => {
          int owner = IndexOf(runners, c.Game);
          if (owner < 0 || !c.Frame.IsVerified) return;
          ObserveResult(c.Frame, match[owner], ref observedError);
          if (bufferProbeTick != int.MaxValue)
            ObserveBufferProbe(c.Frame, owner, bufferProbeTick + 28, bufferPendingSeen, bufferDispatchTicks, ref observedError);
          foreach (var pair in c.Frame.GetComponentIterator<LSDF_HitboxInfo>()) {
            var hit = c.Frame.Get<LSDF_HitboxInfo>(pair.Entity);
            int timedIndex = hit.startFrame == 18 ? (hit.attackDamage == 15 ? 0 : hit.attackDamage == 27 ? 1 : -1) :
              hit.startFrame == 16 ? (hit.attackDamage == 12 ? 2 : hit.attackDamage == 14 ? 3 : -1) : -1;
            if (timedIndex >= 0) {
              timedHits[owner, timedIndex]++;
              bool lars = timedIndex >= 2;
              bool low = timedIndex == 0;
              int expectedHitTicks = lars ? 28 : low ? 20 : 29;
              if (hit.enemyGuardTime != (lars ? 21 : 20) || hit.enemyHitTime != expectedHitTicks ||
                  hit.enemyCountTime != expectedHitTicks ||
                  hit.AttackType != (low ? HitboxAttackType.Low : HitboxAttackType.Mid) ||
                  hit.CountType != (lars ? CountAttackType.Air : CountAttackType.Normal) ||
                  (bool)hit.launcher != lars || (bool)hit.jumpAttack != lars || (bool)hit.dodgeHigh != (lars || low))
                observedError = "Migrated timed-hit payload changed";
              if (!c.Frame.TryGet<TickToDestroy>(pair.Entity, out var timedExpiry) ||
                  timedExpiry.TickToDestroyAt != c.Frame.Number + 1)
                observedError = "Migrated timed-hit lifetime changed";
              FPVector2 expectedEffect = timedIndex == 2 ? FPVector2.Zero : c.Frame.Get<Transform2D>(pair.Entity).Position;
              if (hit.position != expectedEffect) observedError = "Migrated timed-hit effect position changed";
            }
            if (hit.startFrame == 10 && hit.attackDamage == 7 && hit.AttackType == HitboxAttackType.High)
              jabHits[owner]++;
            if ((hit.startFrame == 8 && hit.attackDamage == 12) ||
                (hit.startFrame == 5 && hit.attackDamage == 7))
              chainHits[owner]++;
            if (hit.startFrame != 13 || hit.attackDamage != 12) continue;
            leftUpperHits[owner]++;
            if (bufferProbeTick != int.MaxValue && c.Frame.Number >= bufferProbeTick + 28 &&
                c.Frame.TryGet<PlayerLink>(hit.AttackerEntity, out var probeLink) &&
                c.Frame.GetPlayerData(probeLink.PlayerRef).PlayerNickname == "LSDF-test-A")
              bufferUpperSeen[owner] = true;
            if (hit.AttackType != HitboxAttackType.Mid || hit.enemyGuardTime != 19 ||
                hit.enemyHitTime != 25 || hit.enemyCountTime != 25)
              observedError = "Migrated LeftUpper payload changed";
            if (!c.Frame.TryGet<TickToDestroy>(pair.Entity, out var expiry) ||
                expiry.TickToDestroyAt != c.Frame.Number + 1)
              observedError = "Migrated LeftUpper lifetime changed";
          }
          if (c.Frame.Number % 30 != 0) return;
          int count = 0;
          foreach (var pair in c.Frame.GetComponentIterator<LSDF_Player>()) {
            if (!c.Frame.TryGet<PlayerLink>(pair.Entity, out var link)) continue;
            var player = c.Frame.Get<LSDF_Player>(pair.Entity);
            var data = c.Frame.GetPlayerData(link.PlayerRef);
            int expected = data.PlayerNickname == "LSDF-test-A" ? 2 : 1;
            if (ReadSkill(player, 4) != expected) observedError = "Per-player payload mismatch";
            if (player.playerHp < 170) damageObserved = true;
            count++;
          }
          if (count > 2) observedError = "Duplicate player entity after AddPlayer";
          if (count == 2) {
            samples[owner][c.Frame.Number] = c.Frame.CalculateChecksum();
            rosterSamples++;
          }
        });
        checksumListener = QuantumCallback.SubscribeManual<CallbackChecksumError>(c => {
          if (IndexOf(runners, c.Game) >= 0) observedError = "Quantum checksum error callback";
        });
        stage = "session start handshake";
        await Task.WhenAll(starts);
        runners[0] = (QuantumRunner)starts[0].Result;
        runners[1] = (QuantumRunner)starts[1].Result;
        combatStartTick = Math.Max(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) + 30;
        // Probe before lethal combat: a terminal KO deliberately never heals for a later probe.
        bufferQuietTick = combatStartTick + 120;
        bufferProbeTick = combatStartTick + 240;
        stage = "combat and repeated AddPlayer";
        runners[0].Game.AddPlayer(0, players[0]);
        runners[1].Game.AddPlayer(0, players[1]);
        await Task.Delay(16000, timeout.Token);
        runners[0].Game.AddPlayer(0, players[0]);
        await Task.Delay(10000, timeout.Token);
        stage = "verified-frame assertions";
        TestContext.Progress.WriteLine("ONLINE observations: samples=" + samples[0].Count + "/" + samples[1].Count +
          ", jab=" + jabHits[0] + "/" + jabHits[1] + ", chain=" + chainHits[0] + "/" + chainHits[1] +
          ", upper=" + leftUpperHits[0] + "/" + leftUpperHits[1]);
        if (observedError != null) throw new InvalidOperationException(observedError);
        int comparisons = 0;
        foreach (var sample in samples[0]) {
          if (!samples[1].TryGetValue(sample.Key, out var other)) continue;
          if (sample.Value != other) throw new InvalidOperationException("Checksum mismatch at tick " + sample.Key);
          comparisons++;
        }
        if (comparisons < 10 || rosterSamples < 20)
          throw new InvalidOperationException("Insufficient common verified frames: " + comparisons);
        if (!damageObserved) throw new InvalidOperationException("Attack scenario did not observe damage.");
        if (leftUpperHits[0] == 0 || leftUpperHits[1] == 0)
          throw new InvalidOperationException("Migrated LeftUpper hitbox not observed on both clients.");
        if (jabHits[0] == 0 || jabHits[1] == 0 || chainHits[0] == 0 || chainHits[1] == 0)
          throw new InvalidOperationException("Migrated jab/chain hitboxes not observed on both clients.");
        for (int client = 0; client < 2; ++client)
          for (int hit = 0; hit < 4; ++hit)
            if (timedHits[client, hit] == 0)
              throw new InvalidOperationException("Timed hit not observed: client=" + client + ", hit=" + hit);
        stage = "released pre-input probe";
        if (observedError != null) throw new InvalidOperationException(observedError);
        for (int client = 0; client < 2; client++) {
          if (!bufferPendingSeen[client] || bufferDispatchTicks[client] == 0 || !bufferUpperSeen[client])
            throw new InvalidOperationException("Released input buffer was not captured, dispatched and executed on client " + client);
        }
        if (bufferDispatchTicks[0] != bufferDispatchTicks[1])
          throw new InvalidOperationException("Input buffer dispatched on different verified ticks.");
        TestContext.Out.WriteLine("INPUT BUFFER PASS: press=" + (bufferProbeTick + 28) + ", dispatch=" +
          bufferDispatchTicks[0] + "; both clients captured the locked input and produced LeftUpper after key release.");
        stage = "five decisive rounds, natural crouch, and persistent match end";
        finishAttackTick = Math.Max(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) + 30;
        var finishDeadline = DateTime.UtcNow.AddSeconds(180);
        while (match[0].MatchEndTick == 0 || match[1].MatchEndTick == 0 ||
               runners[0].Game.Frames.Verified.Number - match[0].MatchEndTick < 240 ||
               runners[1].Game.Frames.Verified.Number - match[1].MatchEndTick < 240) {
          if (observedError != null) throw new InvalidOperationException(observedError);
          if (DateTime.UtcNow >= finishDeadline) throw new InvalidOperationException("No best-of-five match end with 240 stable ticks on both clients.");
          await Task.Delay(250, timeout.Token);
        }
        if (observedError != null) throw new InvalidOperationException(observedError);
        for (int client = 0; client < 2; client++) {
          var observed = match[client];
          if (observed.RoundEnds != 5 || observed.Intros != 4 || observed.CrouchEntries != 5 || observed.CrouchSettles != 5 ||
              Math.Max(observed.Losses[0], observed.Losses[1]) != 3 || Math.Min(observed.Losses[0], observed.Losses[1]) != 2)
            throw new InvalidOperationException("Incomplete best-of-five lifecycle on client " + client +
              ": rounds=" + observed.RoundEnds + ", intros=" + observed.Intros + ", crouch=" + observed.CrouchEntries + "/" + observed.CrouchSettles);
        }
        if (match[0].MatchEndTick != match[1].MatchEndTick || match[0].Hp[0] != match[1].Hp[0] || match[0].Hp[1] != match[1].Hp[1] ||
            match[0].Losses[0] != match[1].Losses[0] || match[0].Losses[1] != match[1].Losses[1])
          throw new InvalidOperationException("Clients disagree on final match tick, score or HP.");
        TestContext.Out.WriteLine("MATCH PASS: tick=" + match[0].MatchEndTick + ", losses=" + match[0].Losses[0] + "/" + match[0].Losses[1] +
          "; five KO rounds, four resets/intros, five Sit Enter-to-Siting sequences; final result stable for at least 240 verified ticks on both clients.");
        stage = "recorded input replay";
        int replayTarget = 0;
        foreach (var sample in samples[0])
          if (samples[1].ContainsKey(sample.Key)) replayTarget = Math.Max(replayTarget, sample.Key);
        if (replayTarget < match[0].MatchEndTick + 180) throw new InvalidOperationException("Replay target did not include the final match result.");
        var replayFile = runners[0].Game.GetRecordedReplay(includeChecksums: true, includeDb: false);
        if (replayFile == null)
          throw new InvalidOperationException("No recorded replay was produced.");
        await VerifyReplay(replayFile, samples[0], replayTarget, bufferProbeTick + 29, "A", timeout.Token);
        await VerifyReplay(replayFile, samples[0], replayTarget, bufferProbeTick + 29, "B", timeout.Token);
        TestContext.Progress.WriteLine("ONLINE PASS: two clients, distinct loadouts, combat damage, repeated AddPlayer, " +
          comparisons + " matching verified-frame checksums, LeftUpper, jab chains and both hits of Narak/Lars, plus released input buffering, best-of-five lifecycle and two recorded-input replays. No view/reconnect/induced rollback assertion.");
      } catch (Exception e) {
        throw new InvalidOperationException("ONLINE stage=" + stage + "; client states=" +
          clients[0]?.State + "/" + clients[1]?.State, e);
      } finally {
        try {
          inputListener?.Dispose();
          frameListener?.Dispose();
          checksumListener?.Dispose();
          // A start may have created a runner before the awaited pair failed.
          var cleanupErrors = new List<Exception>();
          for (int i = 0; i < runners.Length; ++i) {
            try {
              var runner = runners[i] ?? QuantumRunner.FindRunner(ids[i]);
              if (runner != null) await runner.ShutdownAsync();
            } catch (Exception e) { cleanupErrors.Add(e); }
            try {
              if (clients[i] != null) await clients[i].DisconnectAsync();
            } catch (Exception e) { cleanupErrors.Add(e); }
          }
          if (cleanupErrors.Count > 0) throw new AggregateException("Online test cleanup failed", cleanupErrors);
        } finally {
          simulation.AutoLoadSceneFromMap = previousSceneMode;
        }
      }
    }
  }

  private static int IndexOf(QuantumRunner[] runners, QuantumGame game) {
    for (int i = 0; i < runners.Length; ++i)
      if (runners[i] != null && ReferenceEquals(runners[i].Game, game)) return i;
    return -1;
  }

  private static async Task VerifyReplay(QuantumReplayFile file, Dictionary<int, ulong> expected,
      int targetTick, int restoreSnapshotTick, string suffix, CancellationToken token) {
    var serializer = new QuantumUnityJsonSerializer();
    QuantumRunner replay = null;
    var provider = file.CreateInputProvider();
    if (provider == null) throw new InvalidOperationException("No replay input provider was produced.");
    IDisposable listener = null;
    string mismatch = null;
    int comparisons = 0;
    byte[] snapshot = null;
    int snapshotTick = 0;
    bool restoring = false;
    try {
      var started = await SessionRunner.StartAsync(new SessionRunner.Arguments {
        RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
        GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
        RuntimeConfig = serializer.ConfigFromByteArray<RuntimeConfig>(file.RuntimeConfigData.Decode(), compressed: true),
        SessionConfig = file.DeterministicConfig,
        ReplayProvider = provider,
        GameMode = DeterministicGameMode.Replay,
        InitialTick = file.InitialTick,
        FrameData = file.InitialFrameData,
        PlayerCount = file.DeterministicConfig.PlayerCount,
        RunnerId = "LSDF-input-replay-" + suffix,
        CancellationToken = token
      });
      replay = (QuantumRunner)started;
      replay.IsSessionUpdateDisabled = true;
      listener = QuantumCallback.SubscribeManual<CallbackSimulateFinished>(callback => {
        if (callback.Game != replay.Game || !callback.Frame.IsVerified || callback.Frame.Number > targetTick) return;
        if (!restoring && snapshot == null && callback.Frame.Number == restoreSnapshotTick) {
          bool pendingFound = false;
          foreach (var pair in callback.Frame.GetComponentIterator<LSDF_CommandBuffer>()) {
            if (callback.Frame.TryGet<PlayerLink>(pair.Entity, out var link) &&
                callback.Frame.GetPlayerData(link.PlayerRef).PlayerNickname == "LSDF-test-A" &&
                callback.Frame.Get<LSDF_CommandBuffer>(pair.Entity).HasPending) pendingFound = true;
          }
          if (!pendingFound) mismatch = "The replay snapshot did not contain the pending input buffer.";
          snapshot = callback.Frame.Serialize(DeterministicFrameSerializeMode.Blit);
          snapshotTick = callback.Frame.Number;
        }
        if (!expected.TryGetValue(callback.Frame.Number, out var checksum)) return;
        if (callback.Frame.CalculateChecksum() != checksum) mismatch = "Replay checksum mismatch at tick " + callback.Frame.Number;
        comparisons++;
      });
      // Bound the manual driver; an empty/broken recording cannot pass as a replay.
      for (int step = 0; step < targetTick + 180; step++) {
        token.ThrowIfCancellationRequested();
        replay.Session.Update(1.0f / file.DeterministicConfig.UpdateFPS);
        if (mismatch != null) throw new InvalidOperationException(mismatch);
        if (replay.Game.Frames.Verified != null && replay.Game.Frames.Verified.Number >= targetTick) break;
      }
      if (replay.Game.Frames.Verified == null || replay.Game.Frames.Verified.Number < targetTick || comparisons < 10)
        throw new InvalidOperationException("Replay failed to reach common verified ticks: " + comparisons);
      TestContext.Progress.WriteLine("REPLAY " + suffix + ": " + comparisons + " matching verified checksums through " + targetTick);
      if (snapshot == null || snapshotTick >= targetTick - 300)
        throw new InvalidOperationException("No usable intermediate frame snapshot was captured.");
      // The SDK's instant replay uses ResetReplay for backward seeking. Exercise the same
      // Frame restoration path, then re-simulate recorded inputs instead of claiming network jitter.
      restoring = true;
      comparisons = 0;
      replay.Session.ResetReplay(snapshot, snapshotTick);
      if (replay.Game.Frames.Verified == null || replay.Game.Frames.Verified.Number >= targetTick)
        throw new InvalidOperationException("Snapshot restore did not rewind the verified frame.");
      for (int step = 0; step < targetTick - snapshotTick + 180; step++) {
        token.ThrowIfCancellationRequested();
        replay.Session.Update(1.0 / file.DeterministicConfig.UpdateFPS);
        if (mismatch != null) throw new InvalidOperationException(mismatch);
        if (replay.Game.Frames.Verified != null && replay.Game.Frames.Verified.Number >= targetTick) break;
      }
      if (replay.Game.Frames.Verified == null || replay.Game.Frames.Verified.Number < targetTick || comparisons < 10)
        throw new InvalidOperationException("Restored frame resimulation was incomplete: " + comparisons);
      TestContext.Progress.WriteLine("RESTORE " + suffix + ": rewound to " + snapshotTick + ", " + comparisons +
        " matching verified checksums through " + targetTick);
    } finally {
      listener?.Dispose();
      if (replay != null) await replay.ShutdownAsync();
      (provider as IDisposable)?.Dispose();
    }
  }

  private static unsafe int ReadSkill(LSDF_Player player, int slot) {
    return player.CommandSkillMap[slot];
  }

  private sealed class MatchObservation {
    public LSDFRoundPhase PreviousPhase;
    public int RoundEnds, Intros, CrouchEntries, CrouchSettles, RoundEndTick, MatchEndTick;
    public bool Entered, Settled;
    public readonly int[] Hp = new int[2], Losses = new int[2];
  }

  private static unsafe void ObserveResult(Frame frame, MatchObservation observed, ref string error) {
    if (!LSDFRoster.TryPair(frame, out var first, out var second) ||
        !frame.TryGet<LSDF_Player>(first, out var a) || !frame.TryGet<LSDF_Player>(second, out var b)) return;
    var phase = LSDFRoundPolicy.Phase(true, a, b, frame.Number);
    if (observed.MatchEndTick != 0 && phase != LSDFRoundPhase.MatchEnd) error = "Final match result resumed combat.";
    var previous = observed.PreviousPhase;
    observed.PreviousPhase = phase;
    if (phase == LSDFRoundPhase.Fighting) return;
    if (phase == LSDFRoundPhase.Intro) {
      if (previous != phase) {
        observed.Intros++;
        var pa = frame.Get<Transform2D>(first).Position;
        var pb = frame.Get<Transform2D>(second).Position;
        var offset = LSDFBalance.SpawnOffset(frame);
        if (pa != new FPVector2(-offset, 0) || pb != new FPVector2(offset, 0)) error = "Next round did not teleport to spawn positions.";
      }
      if (a.playerHp != LSDFBalance.MaximumHealth(frame) || b.playerHp != LSDFBalance.MaximumHealth(frame) ||
          !a.isRoundEnd || !b.isRoundEnd || a.isHealing || b.isHealing || a.isSit || b.isSit ||
          a.hitCount != 0 || b.hitCount != 0 || a.wallCount != 0 || b.wallCount != 0 ||
          a.loseRound != observed.Losses[0] || b.loseRound != observed.Losses[1] ||
          LSDFInputBuffer.CanExecute(a, frame.Number) || LSDFInputBuffer.CanExecute(b, frame.Number))
        error = "Round intro did not reset HP/combat state while retaining scores and input lock.";
      return;
    }
    if (!a.isRoundEnd || !b.isRoundEnd || LSDFRoundPolicy.Outcome(a, b) == LSDFRoundOutcome.None) {
      error = "KO did not finish both fighters with an outcome."; return;
    }
    if (previous != LSDFRoundPhase.RoundEnd && previous != LSDFRoundPhase.MatchEnd) {
      observed.RoundEnds++;
      observed.RoundEndTick = frame.Number;
      observed.Entered = observed.Settled = false;
      observed.Hp[0] = a.playerHp; observed.Hp[1] = b.playerHp;
      observed.Losses[0] = a.loseRound; observed.Losses[1] = b.loseRound;
      if (phase == LSDFRoundPhase.MatchEnd) observed.MatchEndTick = frame.Number;
      TestContext.Out.WriteLine("ROUND " + observed.RoundEnds + ": tick=" + frame.Number + ", losses=" + a.loseRound + "/" + b.loseRound);
    }
    var fighters = new[] { first, second };
    for (int i = 0; i < fighters.Length; i++) {
      var player = frame.Get<LSDF_Player>(fighters[i]);
      if (player.playerHp != observed.Hp[i] || player.loseRound != observed.Losses[i] || player.playerHp < 0 ||
          player.isHealing || player.isAttack || !player.isCantMove || (bool)player.isSit != (player.playerHp == 0))
        error = "KO HP, loss count or stance changed after finalization.";
      if (frame.TryGet<LSDF_CommandBuffer>(fighters[i], out var buffer) && (buffer.HasPending || buffer.Dispatching))
        error = "KO retained an attack command.";
      if (frame.Get<PhysicsBody2D>(fighters[i]).Velocity.X != 0) error = "KO kept horizontal movement.";
      var animator = frame.Unsafe.GetPointer<AnimatorComponent>(fighters[i]);
      var graph = frame.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
      var layer = frame.ResolveList(animator->Layers).GetPointer(0);
      var state = graph.GetState(layer->CurrentStateId);
      var target = layer->ToStateId == 0 ? state : graph.GetState(layer->ToStateId);
      if (player.playerHp == 0) {
        if (!observed.Entered && target != null && target.Name == "Sit Enter") {
          observed.Entered = true; observed.CrouchEntries++;
        }
        bool seated = state != null && state.Name == "Siting" && layer->ToStateId == 0 && layer->IgnoreTransitions;
        if (!observed.Settled && seated) {
          if (!observed.Entered) error = "Loser jumped directly to Siting without Sit Enter.";
          observed.Settled = true; observed.CrouchSettles++;
        }
        if (frame.Number - observed.RoundEndTick >= 40 && !seated) error = "Loser did not finish the sitting animation.";
      } else if (frame.Number - observed.RoundEndTick >= 15 &&
                 (!layer->IgnoreTransitions || layer->ToStateId != 0 || state == null || state.Name != "Idle")) {
        error = "Round winner left the standing pose.";
      }
    }
    foreach (var hit in frame.GetComponentIterator<LSDF_HitboxInfo>())
      if (frame.Get<LSDF_HitboxInfo>(hit.Entity).AttackerEntity == first || frame.Get<LSDF_HitboxInfo>(hit.Entity).AttackerEntity == second)
        error = "KO left an active fighter hitbox.";
  }

  private static void ObserveBufferProbe(Frame frame, int owner, int pressTick, bool[] pending,
      int[] dispatchTicks, ref string error) {
    foreach (var pair in frame.GetComponentIterator<LSDF_CommandBuffer>()) {
      if (!frame.TryGet<PlayerLink>(pair.Entity, out var link) ||
          frame.GetPlayerData(link.PlayerRef).PlayerNickname != "LSDF-test-A") continue;
      var buffer = frame.Get<LSDF_CommandBuffer>(pair.Entity);
      var player = frame.Get<LSDF_Player>(pair.Entity);
      if (buffer.HasPending && buffer.InputTick == pressTick) {
        pending[owner] = true;
        if (LSDFInputBuffer.CanExecute(player, frame.Number) && LSDFCommandProcessor.CanStartCommand(frame, pair.Entity))
          error = "Buffered command missed the first actionable command tick.";
      }
      if (!buffer.Dispatching || buffer.ActiveInputTick != pressTick || buffer.DispatchTick != frame.Number) continue;
      dispatchTicks[owner] = frame.Number;
      if (frame.Number <= pressTick || frame.Number - pressTick > LSDFBalance.InputBufferFrames(frame) ||
          buffer.ActiveDirection != (int)CommandDirection.DownRight || buffer.ActiveButton != (int)CommandButton.LeftPunch)
        error = "Buffered command lost its timestamp, direction or button.";
    }
  }
}
