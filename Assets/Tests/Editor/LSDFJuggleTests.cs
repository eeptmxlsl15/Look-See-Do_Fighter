using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFJuggleTests {
  private static FP Delta => FP._1 / 60;
  private static PhysicsBody2D Body(FP? gravity = null) {
    var body = new PhysicsBody2D();
    body.GravityScale = gravity ?? FP._1; body.Drag = FP._0_50;
    body.Velocity.X = FP._2;
    return body;
  }
  private static void Integrate(ref PhysicsBody2D body, ref FP y) {
    body.Velocity.Y -= 10 * body.GravityScale * Delta;
    body.Velocity.Y *= FP._1 - body.Drag * Delta;
    y += body.Velocity.Y * Delta;
  }

  [Test]
  public void FlightRisesHoldsAtReachableHeightThenFallsWithOriginalGravity() {
    var settings = new LSDFJuggleSettings(); var state = new LSDF_AirCombo();
    var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref state, ref body, ref y, settings, 100, Delta);
    Assert.AreEqual(112, state.RiseUntilTick); Assert.AreEqual(136, state.HoldUntilTick);
    for (int tick = 101; tick <= 112; tick++) {
      Integrate(ref body, ref y); LSDFJuggle.Step(ref state, ref body, y, tick, Delta);
    }
    Assert.AreEqual(LSDFAirComboPhase.Hovering, state.Phase);
    Assert.That(FPMath.Abs(y - settings.LaunchHeight).RawValue, Is.LessThanOrEqualTo(8));
    FP hoverY = y;
    for (int tick = 113; tick < 136; tick++) {
      Integrate(ref body, ref y); LSDFJuggle.Step(ref state, ref body, y, tick, Delta);
      Assert.AreEqual(hoverY, y); Assert.AreEqual(FP._0, body.GravityScale);
    }
    LSDFJuggle.Step(ref state, ref body, y, 136, Delta);
    Assert.AreEqual(LSDFAirComboPhase.Falling, state.Phase); Assert.AreEqual(FP._1, body.GravityScale);
    Integrate(ref body, ref y); Assert.IsTrue(y < hoverY); Assert.AreEqual(FP._2, body.Velocity.X);
  }

  [TestCase(LSDFAirComboPhase.Rising)] [TestCase(LSDFAirComboPhase.Hovering)] [TestCase(LSDFAirComboPhase.Falling)]
  public void RehitSnapsToFixedHeightAndClearsVelocityWithoutReplacingSavedGravity(LSDFAirComboPhase phase) {
    var settings = new LSDFJuggleSettings(); var state = new LSDF_AirCombo();
    var body = Body(FP._1 + FP._0_50); FP y = 0;
    LSDFJuggle.Begin(ref state, ref body, ref y, settings, 100, Delta);
    state.Phase = phase;
    y = FP._0_33; body.Velocity.Y = -FP._2;
    LSDFJuggle.Begin(ref state, ref body, ref y, settings, 120, Delta);
    Assert.AreEqual(LSDFAirComboPhase.Hovering, state.Phase);
    Assert.AreEqual(120, state.RiseUntilTick); Assert.AreEqual(132, state.HoldUntilTick);
    Assert.AreEqual(settings.ComboHeight, state.TargetY); Assert.AreEqual(state.TargetY, y);
    Assert.AreEqual(FP._1 + FP._0_50, state.SavedGravityScale);
    Assert.AreEqual(FP._0, body.GravityScale); Assert.AreEqual(FP._0, body.Velocity.Y);
    LSDFJuggle.Cancel(ref state, ref body);
    Assert.AreEqual(FP._1 + FP._0_50, body.GravityScale);
  }

  [Test]
  public void RepeatedHitsCapHeightAndSharedSettingsDoNotLeakBetweenFighters() {
    var settings = new LSDFJuggleSettings(); var a = new LSDF_AirCombo(); var b = new LSDF_AirCombo();
    var first = Body(); var second = Body();
    FP firstY = 0, secondY = FP._1;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, settings, 100, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, settings, 110, Delta);
    FP secondTarget = b.TargetY; int secondUntil = b.HoldUntilTick;
    for (int i = 0; i < 20; i++) LSDFJuggle.Begin(ref a, ref first, ref firstY, settings, 120 + i, Delta);
    Assert.IsTrue(a.TargetY <= settings.MaximumHeight);
    Assert.AreEqual(settings.ComboHeight, firstY);
    Assert.AreEqual(secondTarget, b.TargetY); Assert.AreEqual(secondUntil, b.HoldUntilTick);
    Assert.AreEqual(FP._0_25 + FP._0_05, settings.LaunchHeight);
    LSDFJuggle.Cancel(ref a, ref first); Assert.AreEqual(FP._0, second.GravityScale);
  }

  [TestCase(LSDFAirComboPhase.Rising)] [TestCase(LSDFAirComboPhase.Hovering)]
  [TestCase(LSDFAirComboPhase.Falling)]
  public void CancelRestoresGravityOnceAndDropsAllPerFlightState(LSDFAirComboPhase phase) {
    var air = new LSDF_AirCombo(); var body = Body(FP._0_75); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, new LSDFJuggleSettings(), 30, Delta);
    air.Phase = phase;
    LSDFJuggle.Cancel(ref air, ref body);
    Assert.AreEqual(FP._0_75, body.GravityScale); Assert.AreEqual(default(LSDF_AirCombo), air);
    body.GravityScale = FP._2; LSDFJuggle.Cancel(ref air, ref body); Assert.AreEqual(FP._2, body.GravityScale);
  }

  [TestCase(LSDFAirComboPhase.Rising, true, false)]
  [TestCase(LSDFAirComboPhase.Hovering, true, false)]
  [TestCase(LSDFAirComboPhase.Falling, false, false)]
  [TestCase(LSDFAirComboPhase.Falling, true, true)]
  [TestCase(LSDFAirComboPhase.Inactive, true, true)]
  public void OnlyGroundContactAfterTheHoldCanTriggerRising(LSDFAirComboPhase phase, bool grounded, bool expected) {
    var player = new LSDF_Player { isAir = true, isGround = grounded };
    var body = Body(); body.Velocity.Y = 0;
    Assert.AreEqual(expected, LSDFJuggle.ShouldLand(player, body, new LSDF_AirCombo { Phase = phase }));
  }

  [Test]
  public void ZeroHoldStillFallsAndInvalidSettingsCannotDivideByZero() {
    var settings = new LSDFJuggleSettings { LaunchRiseTicks = 0, LaunchHoldTicks = -3, MaximumHeight = -FP._1 };
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.Begin(ref air, ref body, ref y, settings, 100, Delta);
    Assert.AreEqual(101, air.RiseUntilTick); Assert.AreEqual(101, air.HoldUntilTick);
    Assert.IsTrue(air.TargetY >= FP._0);
    LSDFJuggle.Step(ref air, ref body, air.TargetY, 101, Delta);
    Assert.AreEqual(LSDFAirComboPhase.Falling, air.Phase); Assert.AreEqual(FP._1, body.GravityScale);
  }

  [Test]
  public void DifferentIncomingHeightsAndVelocitiesProduceIdenticalRehitFallTrajectories() {
    var config = new LSDFJuggleSettings();
    var a = new LSDF_AirCombo(); var b = new LSDF_AirCombo();
    var first = Body(FP._0_75); var second = Body(FP._0_75);
    FP firstY = FP._2, secondY = FP._2;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 10, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, config, 10, Delta);
    firstY += FP._0_10; secondY += FP._0_33;
    first.Velocity.Y = FP._2; second.Velocity.Y = -FP._2;
    a.Phase = LSDFAirComboPhase.Rising; b.Phase = LSDFAirComboPhase.Falling;
    LSDFJuggle.Begin(ref a, ref first, ref firstY, config, 100, Delta);
    LSDFJuggle.Begin(ref b, ref second, ref secondY, config, 200, Delta);
    Assert.AreEqual(FP._2 + config.ComboHeight, firstY); Assert.AreEqual(firstY, secondY);
    for (int elapsed = 1; elapsed <= config.RehitHoldTicks + 30; elapsed++) {
      Integrate(ref first, ref firstY); Integrate(ref second, ref secondY);
      LSDFJuggle.Step(ref a, ref first, firstY, 100 + elapsed, Delta);
      LSDFJuggle.Step(ref b, ref second, secondY, 200 + elapsed, Delta);
      Assert.AreEqual(firstY, secondY, "height at relative tick " + elapsed);
      Assert.AreEqual(first.Velocity.Y, second.Velocity.Y, "velocity at relative tick " + elapsed);
      Assert.AreEqual(first.GravityScale, second.GravityScale);
    }
    Assert.IsTrue(first.Velocity.Y < FP._0); Assert.AreEqual(FP._0_75, first.GravityScale);
  }

  [Test]
  public void GameConfigContainsEditableAirComboValuesWithoutChangingTheOldBalance() {
    var config = AssetDatabase.LoadAssetAtPath<LSDF_GameConfig>("Assets/QuantumUser/Resources/LSDF_GameConfig.asset");
    Assert.IsNotNull(config.AirCombo);
    Assert.GreaterOrEqual(config.AirCombo.LaunchHoldTicks, 0);
    Assert.GreaterOrEqual(config.AirCombo.RehitHoldTicks, 0);
    var defaults = new LSDFJuggleSettings();
    Assert.AreEqual(24, defaults.LaunchHoldTicks); Assert.AreEqual(12, defaults.RehitHoldTicks);
    Assert.IsTrue(config.Guid.IsValid);
  }
}
