using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFJuggleTests {
  private static FP Delta => FP._1 / 60;
  private static PhysicsBody2D Body(FP? gravity = null) {
    var body = new PhysicsBody2D { GravityScale = gravity ?? FP._1, Drag = FP._0_50 };
    body.Velocity.X = FP._2;
    return body;
  }
  private static void Integrate(ref PhysicsBody2D body, ref FP y) {
    body.Velocity.Y -= 10 * body.GravityScale * Delta;
    body.Velocity.Y *= FP._1 - body.Drag * Delta;
    y += body.Velocity.Y * Delta;
  }
  private static void Advance(ref LSDF_AirCombo air, ref PhysicsBody2D body, ref FP y, int tick,
      LSDFJuggleSettings config = null) {
    Integrate(ref body, ref y);
    LSDFJuggle.Step(ref air, ref body, y, tick, Delta, config);
  }
  private static void Near(FP expected, FP actual, int tolerance = 16) {
    Assert.That(FPMath.Abs(expected - actual).RawValue, Is.LessThanOrEqualTo(tolerance));
  }

  [Test]
  public void UntouchedLauncherKeepsItsHoldThenAcceleratesDownWithOriginalGravity() {
    var config = new LSDFJuggleSettings(); var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    Assert.AreEqual(112, air.RiseUntilTick); Assert.AreEqual(136, air.HoldUntilTick);
    for (int tick = 101; tick <= 112; tick++) Advance(ref air, ref body, ref y, tick);
    Assert.AreEqual(LSDFAirComboPhase.Hovering, air.Phase); Near(config.LaunchHeight, y);
    FP peak = y;
    for (int tick = 113; tick < 136; tick++) Advance(ref air, ref body, ref y, tick);
    Assert.AreEqual(peak, y); Assert.AreEqual(FP._0, body.GravityScale);
    Advance(ref air, ref body, ref y, 136);
    Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase);
    Assert.AreEqual(FP._1, body.GravityScale); Assert.AreEqual(FP._0, body.Velocity.Y);
    for (int tick = 137; tick <= 142; tick++) {
      FP previousY = y, previousSpeed = body.Velocity.Y;
      Advance(ref air, ref body, ref y, tick);
      Assert.IsTrue(y < previousY); Assert.IsTrue(body.Velocity.Y < previousSpeed);
      Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase);
    }
    Assert.AreEqual(FP._2, body.Velocity.X);
    LSDFJuggle.Cancel(ref air, ref body); Assert.AreEqual(FP._1, body.GravityScale);
  }

  [Test]
  public void RehitDuringNaturalFallStillStartsTheArcAndPreservesOriginalGravity() {
    var config = new LSDFJuggleSettings { LaunchHoldTicks = 0 };
    var air = new LSDF_AirCombo(); var body = Body(FP._0_75); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    for (int tick = 101; tick <= 116; tick++) Advance(ref air, ref body, ref y, tick);
    Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase); Assert.IsTrue(body.Velocity.Y < FP._0);
    FP contactY = y;
    Assert.IsFalse(LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 116, Delta));
    Assert.AreEqual(contactY, y); Assert.AreEqual(1, air.HitCount);
    Assert.AreEqual(LSDFAirComboPhase.RehitRising, air.Phase);
    Assert.AreEqual(FP._0, body.GravityScale); Assert.AreEqual(FP._0_75, air.SavedGravityScale);
    Assert.IsTrue(body.Velocity.Y > FP._0);
  }

  [TestCase(LSDFAirComboPhase.Rising)] [TestCase(LSDFAirComboPhase.Hovering)]
  [TestCase(LSDFAirComboPhase.Returning)] [TestCase(LSDFAirComboPhase.SlowFalling)]
  public void RehitBeginsMotionFromContactHeightWithoutTeleportOrSavedGravityLoss(LSDFAirComboPhase phase) {
    var config = new LSDFJuggleSettings(); var air = new LSDF_AirCombo(); var body = Body(FP._1 + FP._0_50); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    air.Phase = phase; y = FP._0_20; body.Velocity.Y = -FP._2;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 120, Delta);
    Assert.AreEqual(FP._0_20, y); Assert.AreEqual(LSDFAirComboPhase.RehitRising, air.Phase);
    Assert.AreEqual(126, air.RiseUntilTick); Assert.AreEqual(132, air.HoldUntilTick);
    Assert.AreEqual(config.ComboHeight + config.RehitLift, air.TargetY);
    Assert.AreEqual(config.ComboHeight, air.ComboY); Assert.IsTrue(body.Velocity.Y > FP._0);
    Assert.AreEqual(FP._1 + FP._0_50, air.SavedGravityScale);
    LSDFJuggle.Cancel(ref air, ref body); Assert.AreEqual(FP._1 + FP._0_50, body.GravityScale);
  }

  [TestCase(false)] [TestCase(true)]
  public void RehitCurvesToBaselineThenAcceleratesDownWithoutATeleport(bool drag) {
    var config = new LSDFJuggleSettings(); var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    body.Drag = drag ? FP._0_50 : FP._0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 10, Delta);
    y = config.ComboHeight;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    for (int tick = 101; tick <= 106; tick++) Advance(ref air, ref body, ref y, tick);
    Near(config.ComboHeight + config.RehitLift, y);
    Assert.IsTrue(y > config.ComboHeight); Assert.AreEqual(LSDFAirComboPhase.Returning, air.Phase);
    Assert.IsTrue(body.Velocity.Y < FP._0);
    FP firstReturnSpeed = -body.Velocity.Y;
    FP previousY = y;
    for (int tick = 107; tick <= 112; tick++) {
      Advance(ref air, ref body, ref y, tick);
      Assert.IsTrue(y <= previousY); Assert.IsTrue(y >= config.ComboHeight - FP._0_01);
      previousY = y;
    }
    Near(config.ComboHeight, y); Assert.AreEqual(LSDFAirComboPhase.SlowFalling, air.Phase);
    Assert.IsTrue(firstReturnSpeed < config.RehitLift / (config.RehitReturnTicks * Delta));
    FP previousSpeed = body.Velocity.Y;
    for (int tick = 113; tick <= 142; tick++) {
      FP before = y; Advance(ref air, ref body, ref y, tick);
      Assert.IsTrue(y < before); Assert.IsTrue(body.Velocity.Y < previousSpeed);
      Assert.AreEqual(FP._0, body.GravityScale); previousSpeed = body.Velocity.Y;
      LSDFJuggle.Step(ref air, ref body, y, tick, Delta, config);
      Assert.AreEqual(previousSpeed, body.Velocity.Y);
    }
    FP duration = 30 * Delta;
    Assert.IsTrue(y < config.ComboHeight - config.SlowFallSpeed * duration);
    Assert.AreEqual(FP._2, body.Velocity.X);
  }

  [TestCase(1, false)] [TestCase(2, false)] [TestCase(3, false)] [TestCase(4, false)]
  [TestCase(1, true)] [TestCase(2, true)] [TestCase(3, true)] [TestCase(4, true)]
  public void StoppingAfterAnyNonterminalHitUsesTheSameAcceleratingFallAndLands(int hits, bool drag) {
    var config = new LSDFJuggleSettings(); var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    body.Drag = drag ? FP._0_50 : FP._0;
    Assert.IsFalse(LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 100, Delta, starter: true));
    for (int hit = 1; hit <= hits; hit++) {
      y = config.ComboHeight;
      Assert.IsFalse(LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 200 + hit * 30, Delta));
    }
    Assert.AreEqual(hits, air.HitCount); Assert.IsFalse(LSDFJuggle.BlocksHit(air));
    int tick = 200 + hits * 30;
    for (int i = 1; i <= 12; i++) Advance(ref air, ref body, ref y, tick + i, config);
    Near(config.ComboHeight, y); Assert.AreEqual(LSDFAirComboPhase.SlowFalling, air.Phase);
    FP speed = body.Velocity.Y;
    bool landed = false;
    for (int i = 13; i <= 120; i++) {
      Advance(ref air, ref body, ref y, tick + i, config);
      Assert.IsTrue(body.Velocity.Y < speed); speed = body.Velocity.Y;
      if (y <= air.BaseY) { landed = true; break; }
    }
    Assert.IsTrue(landed, "A stopped combo must eventually reach the ground.");
    var player = new LSDF_Player { isAir = true, isGround = true };
    Assert.IsTrue(LSDFJuggle.ShouldLand(player, body, air));
    LSDFJuggle.Cancel(ref air, ref body);
    Assert.AreEqual(FP._1, body.GravityScale); Assert.AreEqual(default(LSDF_AirCombo), air);
  }

  [TestCase(0)] [TestCase(-1)]
  public void NonpositiveAccelerationRetainsConstantFallAndRepeatedTickDoesNotAddSpeed(int acceleration) {
    var config = new LSDFJuggleSettings { SlowFallAcceleration = acceleration };
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 10, Delta);
    y = config.ComboHeight; LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    for (int tick = 101; tick <= 112; tick++) Advance(ref air, ref body, ref y, tick, config);
    FP speed = body.Velocity.Y;
    for (int tick = 113; tick <= 125; tick++) {
      FP before = y; Advance(ref air, ref body, ref y, tick, config);
      Assert.AreEqual(speed, body.Velocity.Y); Near(config.SlowFallSpeed * Delta, before - y);
      LSDFJuggle.Step(ref air, ref body, y, tick, Delta, config);
      Assert.AreEqual(speed, body.Velocity.Y);
    }
  }

  [Test]
  public void SmallLiftWithLargeRequestedSpeedDoesNotUndershootBeforeReturnEnds() {
    var config = new LSDFJuggleSettings { RehitLift = FP._0_01, SlowFallSpeed = FP._2 };
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 10, Delta);
    y = config.ComboHeight; LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    for (int tick = 101; tick <= 112; tick++) {
      Advance(ref air, ref body, ref y, tick, config);
      Assert.IsTrue(y >= config.ComboHeight - FP._0_01);
    }
    Near(config.ComboHeight, y);
    Assert.AreEqual(LSDFAirComboPhase.SlowFalling, air.Phase);
  }

  [Test]
  public void DifferentIncomingHeightsAndVelocitiesConvergeAtPeakAndHaveSameSlowFall() {
    var config = new LSDFJuggleSettings(); var a = new LSDF_AirCombo(); var b = new LSDF_AirCombo();
    var first = Body(FP._0_75); var second = Body(FP._0_75); FP firstY = FP._2, secondY = FP._2;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 10, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, config, 10, Delta);
    firstY += FP._0_10; secondY += FP._0_33;
    first.Velocity.Y = FP._2; second.Velocity.Y = -FP._2;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 100, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, config, 200, Delta);
    for (int elapsed = 1; elapsed <= 42; elapsed++) {
      Advance(ref a, ref first, ref firstY, 100 + elapsed);
      Advance(ref b, ref second, ref secondY, 200 + elapsed);
      if (elapsed >= config.RehitRiseTicks) Near(firstY, secondY);
      if (elapsed >= config.RehitRiseTicks + config.RehitReturnTicks)
        Assert.AreEqual(first.Velocity.Y, second.Velocity.Y);
    }
    Assert.AreEqual(FP._0_75, a.SavedGravityScale); Assert.AreEqual(FP._0_75, b.SavedGravityScale);
  }

  [Test]
  public void RepeatedRehitsCapPeakAndDoNotMoveAnotherFighterOrMutateSharedSettings() {
    var config = new LSDFJuggleSettings { ComboHeight = FP._0_33, RehitLift = FP._0_25 };
    var a = new LSDF_AirCombo(); var b = new LSDF_AirCombo(); var first = Body(); var second = Body();
    FP firstY = 0, secondY = FP._1;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 100, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, config, 110, Delta);
    var beforeSecond = b;
    for (int i = 0; i < 20; i++) LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 120 + i, Delta);
    Assert.AreEqual(config.MaximumHeight, a.TargetY); Assert.AreEqual(FP._0, firstY);
    Assert.AreEqual(beforeSecond, b); Assert.AreEqual(FP._0_25, config.RehitLift);
    LSDFJuggle.Cancel(ref a, ref first); Assert.AreEqual(FP._0, second.GravityScale);
  }

  [TestCase(LSDFAirComboPhase.Rising)] [TestCase(LSDFAirComboPhase.Hovering)]
  [TestCase(LSDFAirComboPhase.RehitRising)] [TestCase(LSDFAirComboPhase.Returning)]
  [TestCase(LSDFAirComboPhase.SlowFalling)]
  public void CancelRestoresOriginalGravityAndClearsAllArcState(LSDFAirComboPhase phase) {
    var air = new LSDF_AirCombo(); var body = Body(FP._0_75); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, new LSDFJuggleSettings(), 30, Delta); air.Phase = phase;
    LSDFJuggle.Cancel(ref air, ref body);
    Assert.AreEqual(FP._0_75, body.GravityScale); Assert.AreEqual(default(LSDF_AirCombo), air);
    body.GravityScale = FP._2; LSDFJuggle.Cancel(ref air, ref body); Assert.AreEqual(FP._2, body.GravityScale);
  }

  [TestCase(LSDFAirComboPhase.Rising, true, false)] [TestCase(LSDFAirComboPhase.Hovering, true, false)]
  [TestCase(LSDFAirComboPhase.RehitRising, true, false)] [TestCase(LSDFAirComboPhase.Returning, true, false)]
  [TestCase(LSDFAirComboPhase.SlowFalling, false, false)] [TestCase(LSDFAirComboPhase.SlowFalling, true, true)]
  [TestCase(LSDFAirComboPhase.Falling, true, true)] [TestCase(LSDFAirComboPhase.Inactive, true, true)]
  public void LandingRequiresGroundContactAfterArcMotion(LSDFAirComboPhase phase, bool grounded, bool expected) {
    var player = new LSDF_Player { isAir = true, isGround = grounded }; var body = Body(); body.Velocity.Y = 0;
    Assert.AreEqual(expected, LSDFJuggle.ShouldLand(player, body, new LSDF_AirCombo { Phase = phase }));
  }

  [Test]
  public void InvalidTimingAndSpeedAreBoundedAndNoLiftStillReturnsAndFalls() {
    var config = new LSDFJuggleSettings { LaunchRiseTicks = 0, LaunchHoldTicks = -3,
      RehitRiseTicks = 0, RehitReturnTicks = 0, RehitLift = -FP._1, SlowFallSpeed = -FP._1 };
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 100, Delta);
    Advance(ref air, ref body, ref y, 101);
    Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase); Assert.AreEqual(1, air.ReturnTicks);
    Assert.AreEqual(FP._1, body.GravityScale);
    Advance(ref air, ref body, ref y, 102);
    Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase); Assert.IsTrue(body.Velocity.Y < FP._0);
    LSDFJuggle.Begin(ref air, ref body, ref y, config, 110, Delta);
    Assert.AreEqual(111, air.RiseUntilTick); Assert.AreEqual(112, air.HoldUntilTick);
    Assert.AreEqual(air.ComboY, air.TargetY); Assert.AreEqual(FP._0_01, air.SlowFallSpeed);
  }

  [Test]
  public void GameConfigExposesArcTuningAndPreservesTheOriginalAssetIdentity() {
    var config = AssetDatabase.LoadAssetAtPath<LSDF_GameConfig>("Assets/QuantumUser/Resources/LSDF_GameConfig.asset");
    Assert.IsNotNull(config.AirCombo); Assert.IsTrue(config.Guid.IsValid);
    Assert.Greater(config.AirCombo.RehitRiseTicks, 0); Assert.Greater(config.AirCombo.RehitReturnTicks, 0);
    Assert.IsTrue(config.AirCombo.RehitLift > FP._0); Assert.IsTrue(config.AirCombo.SlowFallSpeed > FP._0);
    Assert.IsTrue(config.AirCombo.SlowFallAcceleration > FP._0);
    Assert.AreEqual(6, new LSDFJuggleSettings().RehitReturnTicks);
  }
}
