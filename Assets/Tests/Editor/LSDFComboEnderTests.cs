using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;

public class LSDFComboEnderTests {
  private static FP Delta => FP._1 / 60;
  private static PhysicsBody2D Body() => new PhysicsBody2D { GravityScale = FP._0_75 };

  [TestCase(-1)] [TestCase(1)]
  public void LauncherThenFiveFollowupsEndsComboAndRestoresGravity(int direction) {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings(); FP y = 0;
    Assert.IsFalse(LSDFJuggle.RegisterHit(ref air, ref body, ref y, direction, false, config, 0, Delta, starter: true));
    Assert.AreEqual(0, air.HitCount); Assert.IsTrue((bool)air.ComboActive);
    for (int hit = 1; hit <= 5; hit++) {
      bool ended = LSDFJuggle.RegisterHit(ref air, ref body, ref y, direction, false, config, hit * 30, Delta);
      Assert.AreEqual(hit, air.HitCount);
      Assert.AreEqual(hit == 5, ended); Assert.AreEqual(hit == 5, LSDFJuggle.BlocksHit(air));
    }
    Assert.AreEqual(LSDFAirComboPhase.BlownAway, air.Phase);
    Assert.AreEqual(direction * config.EnderSpeed, body.Velocity.X);
    Assert.AreEqual(config.EnderUpSpeed, body.Velocity.Y); Assert.AreEqual(FP._0_75, body.GravityScale);
  }

  [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
  public void ExplicitFinisherEndsAtEveryOrdinalIncludingFirstGroundContact(int ordinal) {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings(); FP y = 0;
    for (int hit = 1; hit <= ordinal; hit++)
      LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, hit == ordinal, config, hit * 30, Delta, starter: hit == 1);
    Assert.AreEqual(ordinal - 1, air.HitCount); Assert.IsTrue(LSDFJuggle.BlocksHit(air));
    Assert.AreEqual(config.EnderSpeed, body.Velocity.X);
  }

  [Test]
  public void FurtherContactsCannotChangeCountDirectionOrTerminalTimers() {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings(); FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, true, config, 100, Delta);
    var before = air; var velocity = body.Velocity;
    for (int i = 0; i < 10; i++)
      LSDFJuggle.RegisterHit(ref air, ref body, ref y, -1, false, config, 100 + i, Delta);
    Assert.AreEqual(before, air); Assert.AreEqual(velocity, body.Velocity);
  }

  [Test]
  public void OrdinaryAirPushUsesItsOwnSpeedAndStopsAfterConfiguredTicks() {
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    var config = new LSDFJuggleSettings { HitPushSpeed = FP._0_10, HitPushTicks = 3 };
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, -1, false, config, 100, Delta);
    Assert.AreEqual(-FP._0_10, body.Velocity.X);
    LSDFJuggle.Step(ref air, ref body, 0, 102, Delta); Assert.AreEqual(-FP._0_10, body.Velocity.X);
    LSDFJuggle.Step(ref air, ref body, 0, 103, Delta); Assert.AreEqual(FP._0, body.Velocity.X);
    Assert.IsFalse(LSDFJuggle.BlocksHit(air));
  }

  [TestCase(0)] [TestCase(-5)]
  public void ZeroOrNegativePushTimeDisablesOrdinaryPush(int ticks) {
    var air = new LSDF_AirCombo(); var body = Body(); FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false,
      new LSDFJuggleSettings { HitPushTicks = ticks }, 100, Delta);
    Assert.AreEqual(FP._0, body.Velocity.X);
  }

  [Test]
  public void FlightAndGroundRecoveryBlockHitsUntilFullRecoveryThenResetNextCombo() {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings { EnderRecoveryTicks = 24 }; FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, true, config, 100, Delta);
    LSDFJuggle.UpdateEnder(ref air, ref body, true, config, 101); // Still ascending: stale ground contact must not land.
    Assert.AreEqual(LSDFAirComboPhase.BlownAway, air.Phase);
    body.Velocity.Y = 0;
    LSDFJuggle.UpdateEnder(ref air, ref body, true, config, 120);
    Assert.AreEqual(LSDFAirComboPhase.Recovering, air.Phase); Assert.IsTrue(LSDFJuggle.BlocksHit(air));
    Assert.AreEqual(FP._0, body.Velocity.X);
    LSDFJuggle.UpdateEnder(ref air, ref body, true, config, 143); Assert.IsTrue(LSDFJuggle.BlocksHit(air));
    LSDFJuggle.UpdateEnder(ref air, ref body, true, config, 144);
    Assert.AreEqual(default(LSDF_AirCombo), air); Assert.AreEqual(FP._0_75, body.GravityScale);
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, -1, false, config, 150, Delta);
    Assert.AreEqual(1, air.HitCount); Assert.IsFalse(LSDFJuggle.BlocksHit(air));
  }

  [Test]
  public void WallMotionReleaseKeepsCountAndFifthWallContactStillEndsCombo() {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings(); FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 90, Delta, starter: true);
    for (int i = 0; i < 4; i++) {
      LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 100 + i, Delta);
      LSDFJuggle.Cancel(ref air, ref body, preserveCombo: true);
      Assert.AreEqual(i + 1, air.HitCount); Assert.AreEqual(FP._0_75, body.GravityScale);
    }
    Assert.IsTrue(LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 110, Delta));
    Assert.AreEqual(5, air.HitCount);
    LSDFJuggle.Cancel(ref air, ref body); Assert.AreEqual(default(LSDF_AirCombo), air);
  }

  [Test]
  public void AnotherLauncherDuringActiveComboCountsAsFollowupAndEnderKeepsContactHeight() {
    var air = new LSDF_AirCombo(); var body = Body(); var config = new LSDFJuggleSettings(); FP y = 0;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 100, Delta, starter: true);
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, false, config, 120, Delta, starter: true);
    Assert.AreEqual(1, air.HitCount);
    y = FP._0_10;
    LSDFJuggle.RegisterHit(ref air, ref body, ref y, 1, true, config, 140, Delta);
    Assert.AreEqual(FP._0_10, y); Assert.IsTrue(LSDFJuggle.BlocksHit(air));
  }

  [TestCase(-2, 0, 1, -1)] [TestCase(2, 0, -1, 1)]
  [TestCase(0, 0, -1, -1)] [TestCase(0, 0, 1, 1)]
  public void KnockawayPointsAwayFromActualAttacker(int defenderX, int attackerX, int fallback, int expected) {
    Assert.AreEqual(expected, LSDFJuggle.AwayDirection(defenderX, attackerX, fallback));
  }

  [Test]
  public void FinisherIsCopiedToPayloadAndDefaultsToOff() {
    var hit = LSDFSingleHitDefinition.CreateJap();
    Assert.IsFalse(hit.ComboFinisher); Assert.IsFalse((bool)hit.CreateInfo(default, FPVector2.Zero).ComboFinisher);
    hit.ComboFinisher = true;
    Assert.IsTrue((bool)hit.CreateInfo(default, FPVector2.Zero).ComboFinisher);
  }
}
