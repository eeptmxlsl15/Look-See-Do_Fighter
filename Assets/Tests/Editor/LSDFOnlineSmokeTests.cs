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
    IDisposable inputListener = null;
    IDisposable frameListener = null;
    IDisposable checksumListener = null;
    string stage = "prepare";
    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation)) {
      timeout.CancelAfter(TimeSpan.FromSeconds(90));
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
          if (relativeTick < 120) {
            bool localIsZero = false;
            foreach (var playerRef in c.Game.GetLocalPlayers()) {
              localIsZero = playerRef == (PlayerRef)0;
              break;
            }
            input.Right = localIsZero;
            input.Left = !localIsZero;
          } else {
            int phase = (relativeTick - 120 + owner * 12) % 720;
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
        TestContext.Progress.WriteLine("ONLINE PASS: two clients, distinct loadouts, combat damage, repeated AddPlayer, " +
          comparisons + " matching verified-frame checksums, LeftUpper, jab chains and both hits of Narak/Lars. No view/reconnect/induced rollback assertion.");
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

  private static unsafe int ReadSkill(LSDF_Player player, int slot) {
    return player.CommandSkillMap[slot];
  }
}
