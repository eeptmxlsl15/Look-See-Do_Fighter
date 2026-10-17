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
  public IEnumerator AirComboClientsAgreeOnHoverRehitsLandingAndFrameRestore() {
    return RunScenario(false, airComboProbe: true);
  }

  private sealed class AirObservation {
    public bool Rose, Hovered, Rehit, Fell, Landed;
    public int FirstHoverTick, HoverTicks;
    public int PreviousCount;
    public LSDFAirComboPhase Previous;
    public FP PreviousY, MaximumHeight;
  }

  private static async Task VerifyAirCombo(QuantumRunner[] runners, RuntimePlayer[] players, CancellationToken token) {
    var samples = new[] { new Dictionary<int, ulong>(), new Dictionary<int, ulong>() };
    var observed = new AirObservation[2, 2];
    for (int observer = 0; observer < 2; observer++)
      for (int episode = 0; episode < 2; episode++) observed[observer, episode] = new AirObservation();
    int start = Math.Max(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) + 300;
    string error = null;
    using var input = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
      if (IndexOf(runners, callback.Game) < 0 ||
          !callback.Game.Session.TryGetLocalPlayer(callback.PlayerSlot, out var local)) return;
      var value = new Quantum.Input();
      int elapsed = callback.Frame - start;
      if (elapsed >= 0 && elapsed < 960) {
        int episode = elapsed / 480, phase = elapsed % 480;
        bool attacking = local == (PlayerRef)episode;
        // Approach using the normal movement input, then expose the defender's standing hurtbox.
        if (phase < 90) value.Right = true;
        else if (!attacking) value.Up = true; // Law's homing launcher beats this existing parry rule.
        if (attacking) {
          value.RightPunch = phase >= 120 && phase < 123 || phase >= 210 && phase < 213;
          if (value.RightPunch) value.Down = value.Right = true;
        }
      }
      callback.SetInput(value, DeterministicInputFlags.Repeatable);
    });
    using var frames = QuantumCallback.SubscribeManual<CallbackSimulateFinished>(callback => {
      int observer = IndexOf(runners, callback.Game);
      if (observer < 0 || !callback.Frame.IsVerified) return;
      int elapsed = callback.Frame.Number - start;
      if (elapsed >= 0 && elapsed < 960) {
        int episode = elapsed / 480;
        var defender = (PlayerRef)(1 - episode);
        if (LSDFRoster.TryFind(callback.Frame, defender, out var entity) &&
            callback.Frame.TryGet<LSDF_AirCombo>(entity, out var air)) {
          var result = observed[observer, episode];
          var body = callback.Frame.Get<PhysicsBody2D>(entity);
          var player = callback.Frame.Get<LSDF_Player>(entity);
          FP y = callback.Frame.Get<Transform2D>(entity).Position.Y;
          if (air.Phase != LSDFAirComboPhase.Inactive) {
            result.MaximumHeight = FPMath.Max(result.MaximumHeight, y - air.BaseY);
            if (result.MaximumHeight > LSDFBalance.Config(callback.Frame).AirCombo.MaximumHeight + FP._0_01)
              error = "Juggle rose beyond the configured reachable-height cap.";
          }
          if (air.Phase == LSDFAirComboPhase.Rising) {
            result.Rose = true;
          }
          if (air.HitCount > result.PreviousCount) {
            result.Rehit = true;
            if (air.Phase != LSDFAirComboPhase.Hovering || body.Velocity.Y != FP._0 ||
                FPMath.Abs(y - (air.BaseY + LSDFBalance.Config(callback.Frame).AirCombo.ComboHeight)) > FP._0_01)
              error = "Rehit did not reset to the same configured height and zero vertical speed.";
          }
          if (air.Phase == LSDFAirComboPhase.Hovering) {
            result.Hovered = true; result.HoverTicks++;
            if (result.FirstHoverTick == 0) result.FirstHoverTick = callback.Frame.Number;
            if (body.GravityScale != FP._0 || !player.isAir)
              error = "Hover lost airborne ownership or gravity suppression.";
            if (result.Previous == LSDFAirComboPhase.Hovering && air.HitCount == result.PreviousCount &&
                FPMath.Abs(y - result.PreviousY) > FP._0_01)
              error = "The hovering fighter drifted vertically.";
          }
          if (air.Phase == LSDFAirComboPhase.Falling) {
            result.Fell = true;
            if (body.GravityScale != air.SavedGravityScale) error = "Falling did not restore the body's original gravity.";
          }
          if (result.Fell && air.Phase == LSDFAirComboPhase.Inactive && !player.isAir && player.isGround) {
            result.Landed = true;
            if (body.GravityScale != FP._1) error = "Ground recovery retained the temporary hover gravity.";
          }
          result.Previous = air.Phase; result.PreviousY = y;
          result.PreviousCount = air.HitCount;
        }
      }
      if (callback.Frame.Number >= start && callback.Frame.Number % 15 == 0)
        samples[observer][callback.Frame.Number] = callback.Frame.CalculateChecksum();
    });
    using var checksum = QuantumCallback.SubscribeManual<CallbackChecksumError>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Air-combo checksum error.";
    });
    using var disconnect = QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Air-combo plugin disconnect: " + callback.Reason;
    });
    runners[0].Game.AddPlayer(0, players[0]); runners[1].Game.AddPlayer(0, players[1]);
    var deadline = DateTime.UtcNow.AddSeconds(40);
    while (Math.Min(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) < start + 1080) {
      if (error != null) throw new InvalidOperationException(error);
      if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("Air-combo scenario did not finish.");
      await Task.Delay(20, token);
    }
    if (error != null) throw new InvalidOperationException(error);
    for (int observer = 0; observer < 2; observer++) for (int episode = 0; episode < 2; episode++) {
      var result = observed[observer, episode];
      if (!result.Rose || !result.Hovered || !result.Rehit || !result.Fell || !result.Landed || result.HoverTicks < 5)
        throw new InvalidOperationException($"Incomplete air combo: observer={observer}, defender={1 - episode}, rise={result.Rose}, hover={result.Hovered}/{result.HoverTicks}, rehit={result.Rehit}, fall={result.Fell}, land={result.Landed}.");
      TestContext.Out.WriteLine($"AIR COMBO observer={observer}, defender={1 - episode}, firstHover={result.FirstHoverTick}, rehitInput={start + episode * 480 + 210}, hoverTicks={result.HoverTicks}, peakRaw={result.MaximumHeight.RawValue}.");
    }
    int comparisons = 0, target = 0;
    foreach (var sample in samples[0]) if (samples[1].TryGetValue(sample.Key, out var other)) {
      if (sample.Value != other) throw new InvalidOperationException("Air-combo verified checksum mismatch at " + sample.Key);
      comparisons++; target = Math.Max(target, sample.Key);
    }
    if (comparisons < 60) throw new InvalidOperationException("Insufficient air-combo common verified frames.");
    var replay = runners[0].Game.GetRecordedReplay(includeChecksums: true, includeDb: false);
    if (replay == null) throw new InvalidOperationException("No air-combo replay was recorded.");
    int restoreTick = observed[0, 0].FirstHoverTick;
    await VerifyReplay(replay, samples[0], target, restoreTick, "air-A", token, requirePendingBuffer: false, requireAirHover: true);
    await VerifyReplay(replay, samples[0], target, restoreTick, "air-B", token, requirePendingBuffer: false, requireAirHover: true);
    TestContext.Out.WriteLine($"AIR COMBO PASS: both defenders reset to fixed rehit height/zero vertical speed, {comparisons} matching verified checksums, two recorded replays and Frame restore during active hover.");
  }
}
