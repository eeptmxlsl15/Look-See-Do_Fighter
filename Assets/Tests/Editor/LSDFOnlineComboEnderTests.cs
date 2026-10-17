using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEngine.TestTools;

public partial class LSDFOnlineSmokeTests {
  [UnityTest, Timeout(180000), Category("LSDFOnline")]
  public IEnumerator ClientsAgreeOnFifthHitFinisherAndTerminalFrameRestore() {
    return RunScenario(false, comboEnderProbe: true);
  }

  private sealed class ComboEnderFixture : IDisposable {
    private readonly LSDF_GameConfig config;
    private readonly LSDFJuggleSettings savedAir;
    private readonly LawRightUpperWindowEvent upper;
    private readonly DeathFistWindowEvent fist;
    private readonly bool savedUpper, savedFist;
    public ComboEnderFixture(RuntimeConfig runtime, bool enableFinisher) {
      config = QuantumUnityDB.GetGlobalAsset<LSDF_GameConfig>(runtime.GameConfig);
      upper = QuantumUnityDB.GetGlobalAsset<LawRightUpperWindowEvent>(new AssetGuid(444649954622301535L));
      fist = QuantumUnityDB.GetGlobalAsset<DeathFistWindowEvent>(new AssetGuid(329226335990073971L));
      if (config == null || upper == null || fist == null) throw new InvalidOperationException("Missing combo-ender fixture assets.");
      savedAir = config.AirCombo; savedUpper = upper.Hit.ComboFinisher; savedFist = fist.Hit.ComboFinisher;
      // Keep the target reachable through normal attack recovery; isolate the count/ender contract.
      config.AirCombo = new LSDFJuggleSettings { LaunchHoldTicks = enableFinisher ? 90 : 120,
        SlowFallSpeed = enableFinisher ? FP._0_02 : FP._0_20,
        SlowFallAcceleration = enableFinisher ? FP._0 : FP._0_10,
        HitPushSpeed = FP._0, HitPushTicks = 0 };
      upper.Hit.ComboFinisher = false; fist.Hit.ComboFinisher = enableFinisher;
    }
    public void Dispose() {
      config.AirCombo = savedAir; upper.Hit.ComboFinisher = savedUpper; fist.Hit.ComboFinisher = savedFist;
    }
  }

  private sealed class EnderObservation {
    public int MaximumCount, EndTick, EndCount, RecoveryTicks, HpAtEnd;
    public bool Recovered, StarterObserved;
  }

  private static async Task VerifyComboEnders(QuantumRunner[] runners, RuntimePlayer[] players, CancellationToken token) {
    var samples = new[] { new Dictionary<int, ulong>(), new Dictionary<int, ulong>() };
    var observed = new EnderObservation[2, 2];
    for (int observer = 0; observer < 2; observer++)
      for (int episode = 0; episode < 2; episode++) observed[observer, episode] = new EnderObservation();
    int start = Math.Max(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) + 300;
    string error = null;
    using var input = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
      if (IndexOf(runners, callback.Game) < 0 ||
          !callback.Game.Session.TryGetLocalPlayer(callback.PlayerSlot, out var local)) return;
      var value = new Quantum.Input();
      int elapsed = callback.Frame - start;
      if (elapsed >= 0 && elapsed < 1440) {
        int episode = elapsed / 720, phase = elapsed % 720;
        bool attacking = local == (PlayerRef)episode;
        // Closing again after an ender needs more travel than the initial spawn gap.
        if (phase < 120) value.Right = true;
        else if (!attacking) value.Up = true;
        // Down-forward RP (Law skill 1) + five followups; then launcher -> forward RP finisher.
        if (attacking && phase >= 150 && phase < (episode == 0 ? 603 : 333)) {
          value.RightPunch = (phase - 150) % 90 < 3;
          if (value.RightPunch) {
            value.Right = true;
            value.Down = episode == 0 || phase < 240 || phase >= 330;
          }
        }
      }
      callback.SetInput(value, DeterministicInputFlags.Repeatable);
    });
    using var frames = QuantumCallback.SubscribeManual<CallbackSimulateFinished>(callback => {
      int observer = IndexOf(runners, callback.Game);
      if (observer < 0 || !callback.Frame.IsVerified) return;
      int elapsed = callback.Frame.Number - start;
      if (elapsed >= 0 && elapsed < 1440) {
        int episode = elapsed / 720;
        if (LSDFRoster.TryFind(callback.Frame, (PlayerRef)(1 - episode), out var entity) &&
            callback.Frame.TryGet<LSDF_AirCombo>(entity, out var air)) {
          var result = observed[observer, episode];
          var body = callback.Frame.Get<PhysicsBody2D>(entity);
          var player = callback.Frame.Get<LSDF_Player>(entity);
          if (air.ComboActive && air.HitCount == 0 && !LSDFJuggle.BlocksHit(air)) result.StarterObserved = true;
          result.MaximumCount = Math.Max(result.MaximumCount, air.HitCount);
          if (LSDFJuggle.BlocksHit(air)) {
            if (result.EndTick == 0) {
              result.EndTick = callback.Frame.Number; result.EndCount = air.HitCount; result.HpAtEnd = player.playerHp;
              if (body.Velocity.X != (episode == 0 ? FP._1 : -FP._1) * LSDFBalance.Config(callback.Frame).AirCombo.EnderSpeed)
                error = "Finisher did not push away from the attacker.";
            }
            if (air.HitCount != result.EndCount || player.playerHp != result.HpAtEnd)
              error = "Terminal combo accepted another hit.";
            if (body.GravityScale != FP._1 || !player.isAir) error = "Terminal flight lost gravity or airborne lock.";
            if (air.Phase == LSDFAirComboPhase.Recovering) result.RecoveryTicks++;
          } else if (result.EndTick > 0 && player.isGround && !player.isAir && !air.ComboActive) result.Recovered = true;
        }
      }
      if (callback.Frame.Number >= start && callback.Frame.Number % 15 == 0)
        samples[observer][callback.Frame.Number] = callback.Frame.CalculateChecksum();
    });
    using var checksum = QuantumCallback.SubscribeManual<CallbackChecksumError>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Combo-ender checksum error.";
    });
    using var disconnect = QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Combo-ender plugin disconnect: " + callback.Reason;
    });
    runners[0].Game.AddPlayer(0, players[0]); runners[1].Game.AddPlayer(0, players[1]);
    var deadline = DateTime.UtcNow.AddSeconds(60);
    while (Math.Min(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) < start + 1560) {
      if (error != null) throw new InvalidOperationException(error);
      if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("Combo-ender scenario did not finish.");
      await Task.Delay(20, token);
    }
    if (error != null) throw new InvalidOperationException(error);
    for (int observer = 0; observer < 2; observer++) for (int episode = 0; episode < 2; episode++) {
      var result = observed[observer, episode]; int expected = episode == 0 ? 5 : 1;
      if (result.EndTick == 0 || result.EndCount != expected || result.MaximumCount != expected ||
          !result.StarterObserved || result.RecoveryTicks < 24 || !result.Recovered)
        throw new InvalidOperationException($"Incomplete ender: observer={observer}, episode={episode}, count={result.EndCount}/{result.MaximumCount}, endTick={result.EndTick}, recovery={result.RecoveryTicks}, recovered={result.Recovered}.");
      TestContext.Out.WriteLine($"COMBO ENDER observer={observer}, episode={episode}, count={result.EndCount}, endTick={result.EndTick}, recovery={result.RecoveryTicks}.");
    }
    int comparisons = 0, target = 0;
    foreach (var sample in samples[0]) if (samples[1].TryGetValue(sample.Key, out var other)) {
      if (sample.Value != other) throw new InvalidOperationException("Combo-ender checksum mismatch at " + sample.Key);
      comparisons++; target = Math.Max(target, sample.Key);
    }
    if (comparisons < 90) throw new InvalidOperationException("Insufficient combo-ender common verified frames.");
    var replay = runners[0].Game.GetRecordedReplay(includeChecksums: true, includeDb: false);
    if (replay == null) throw new InvalidOperationException("No combo-ender replay recorded.");
    await VerifyReplay(replay, samples[0], target, observed[0, 0].EndTick, "ender-A", token,
      requirePendingBuffer: false, requireAirEnder: true);
    await VerifyReplay(replay, samples[0], target, observed[0, 0].EndTick, "ender-B", token,
      requirePendingBuffer: false, requireAirEnder: true);
    TestContext.Out.WriteLine($"COMBO ENDER PASS: {comparisons} common verified checksums, starter excluded + five followups, first-followup finisher, two replays and terminal Frame restore.");
  }
}
