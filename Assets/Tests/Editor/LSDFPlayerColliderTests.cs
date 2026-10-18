using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using Assert = NUnit.Framework.Assert;

public class LSDFPlayerColliderTests {
  private static FPVector2 Standing => new FPVector2(FP._0, FP._0_01);
  private static PhysicsCollider2D Collider() => new PhysicsCollider2D {
    Shape = Shape2D.CreateBox(new FPVector2(FP._0_20 - FP._0_02, FP._0_33))
  };
  private static LSDF_Player Player() => new LSDF_Player { playerHp = 170, isAir = true };
  private static LSDFJuggleSettings Settings() => new LSDFJuggleSettings {
    EnderColliderCenter = new FPVector2(FP._0_10, -FP._0_05),
    GetUpColliderCenter = new FPVector2(-FP._0_02, FP._0_10)
  };

  [Test]
  public void FlightChangesOnlyCenterEvenWhenBoxSizeAlreadyMatches() {
    var collider = Collider(); var player = Player();
    var air = new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway };
    var settings = Settings(); var size = collider.Shape.Box.Extents;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(settings.EnderColliderCenter, collider.Shape.LocalTransform.Position);
    Assert.AreEqual(size, collider.Shape.Box.Extents);
    settings.EnderColliderCenter.Y = -FP._0_10;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(settings.EnderColliderCenter, collider.Shape.LocalTransform.Position);
    Assert.AreEqual(size, collider.Shape.Box.Extents);
  }

  [TestCase(LSDFAirComboPhase.Inactive)] [TestCase(LSDFAirComboPhase.Rising)]
  [TestCase(LSDFAirComboPhase.Hovering)] [TestCase(LSDFAirComboPhase.Falling)]
  [TestCase(LSDFAirComboPhase.Recovering)] [TestCase(LSDFAirComboPhase.RehitRising)]
  [TestCase(LSDFAirComboPhase.Returning)] [TestCase(LSDFAirComboPhase.SlowFalling)]
  public void EveryOtherPhaseRestoresTheStandingCenter(LSDFAirComboPhase phase) {
    var collider = Collider(); var player = Player();
    var air = new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway }; var settings = Settings();
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    air.Phase = phase;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
  }

  [TestCase(1, true)] [TestCase(0, false)] [TestCase(-1, false)]
  public void GroundContactOnlyUsesFlightCenterWhileStillAscending(int velocity, bool shifted) {
    var collider = Collider(); var player = Player(); player.isGround = true;
    var body = new PhysicsBody2D(); body.Velocity.Y = velocity;
    var air = new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway, RiseUntilTick = 100 }; var settings = Settings();
    LSDFJuggle.UpdateEnder(ref air, ref body, true, settings, 100);
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(shifted ? settings.EnderColliderCenter : Standing, collider.Shape.LocalTransform.Position);
  }

  [TestCase(0, false)] [TestCase(170, true)]
  public void KoAndRoundEndRestoreCenterWithoutWaitingForPhaseCancellation(int hp, bool ended) {
    var collider = Collider(); var player = Player(); var settings = Settings();
    var air = new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway };
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    player.playerHp = hp; player.isRoundEnd = ended;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
  }

  [Test]
  public void CrouchAndMissingSettingsRetainTheirOriginalCenters() {
    var collider = Collider(); var player = Player(); var settings = Settings();
    var air = new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway };
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, null);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
    air.Phase = LSDFAirComboPhase.Inactive; player.isSit = true;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, air, settings);
    Assert.AreEqual(new FPVector2(0, FP._0_25 - FP._0_33 + FP._0_02), collider.Shape.LocalTransform.Position);
    Assert.AreEqual(FP._0_25, collider.Shape.Box.Extents.Y);
  }

  [Test]
  public void OffsetIsWorldAlignedAndDoesNotLeakToAnotherFighterOrMutateSettings() {
    var first = Collider(); var second = Collider(); var player = Player();
    var settings = Settings(); var saved = settings.EnderColliderCenter;
    LSDFPlayerMotion.UpdateCollider(ref first, player, new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway }, settings);
    LSDFPlayerMotion.UpdateCollider(ref second, player, default, settings);
    Assert.AreEqual(saved, first.Shape.LocalTransform.Position);
    Assert.AreEqual(Standing, second.Shape.LocalTransform.Position);
    Assert.AreEqual(saved, settings.EnderColliderCenter);
  }

  [Test]
  public void ConfigPreservesIdentityAndDefaultGetUpCenterWithoutResettingFlightTuning() {
    var config = AssetDatabase.LoadAssetAtPath<LSDF_GameConfig>("Assets/QuantumUser/Resources/LSDF_GameConfig.asset");
    Assert.IsNotNull(config); Assert.IsTrue(config.Guid.IsValid);
    Assert.AreEqual(Standing, new LSDFJuggleSettings().EnderColliderCenter);
    Assert.AreEqual(Standing, config.AirCombo.GetUpColliderCenter);
    Assert.AreEqual(Standing, new LSDFJuggleSettings().GetUpColliderCenter);
  }

  [Test]
  public void GettingUpHasItsOwnCenterAndAnimationExitRestoresStandingWithoutAResize() {
    var collider = Collider(); var player = Player(); player.isAir = false; var settings = Settings();
    var size = collider.Shape.Box.Extents;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, settings, gettingUp: true);
    Assert.AreEqual(settings.GetUpColliderCenter, collider.Shape.LocalTransform.Position);
    Assert.AreEqual(size, collider.Shape.Box.Extents);
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, settings, gettingUp: false);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
    Assert.AreEqual(size, collider.Shape.Box.Extents);
  }

  [Test]
  public void ANewLaunchOrTerminalFlightTakesOwnershipFromAnOldGettingUpPose() {
    var collider = Collider(); var player = Player(); player.isAir = false; var settings = Settings();
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, settings, gettingUp: true);
    player.isAir = true;
    LSDFPlayerMotion.UpdateCollider(ref collider, player,
      new LSDF_AirCombo { Phase = LSDFAirComboPhase.Rising }, settings, gettingUp: true);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
    LSDFPlayerMotion.UpdateCollider(ref collider, player,
      new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway }, settings, gettingUp: true);
    Assert.AreEqual(settings.EnderColliderCenter, collider.Shape.LocalTransform.Position);
    player.isAir = false;
    LSDFPlayerMotion.UpdateCollider(ref collider, player,
      new LSDF_AirCombo { Phase = LSDFAirComboPhase.Recovering }, settings, gettingUp: true);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
  }

  [TestCase(0, false)] [TestCase(170, true)]
  public void GettingUpCenterIsRemovedOnKoOrRoundEnd(int hp, bool ended) {
    var collider = Collider(); var player = Player(); player.isAir = false; var settings = Settings();
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, settings, gettingUp: true);
    player.playerHp = hp; player.isRoundEnd = ended;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, settings, gettingUp: true);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
  }

  [Test]
  public void GettingUpExitCanRestoreCrouchAndMissingSettingsUseTheNormalCenter() {
    var collider = Collider(); var player = Player(); player.isAir = false;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, Settings(), gettingUp: true);
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, null, gettingUp: true);
    Assert.AreEqual(Standing, collider.Shape.LocalTransform.Position);
    player.isSit = true;
    LSDFPlayerMotion.UpdateCollider(ref collider, player, default, Settings(), gettingUp: false);
    Assert.AreEqual(new FPVector2(0, FP._0_25 - FP._0_33 + FP._0_02), collider.Shape.LocalTransform.Position);
  }

  [Test]
  public void TwoFightersCanGetUpAndFlyWithoutSharingColliderPositions() {
    var first = Collider(); var second = Collider(); var player = Player(); player.isAir = false;
    var settings = Settings(); var savedGetUp = settings.GetUpColliderCenter; var savedFlight = settings.EnderColliderCenter;
    LSDFPlayerMotion.UpdateCollider(ref first, player, default, settings, gettingUp: true);
    player.isAir = true;
    LSDFPlayerMotion.UpdateCollider(ref second, player,
      new LSDF_AirCombo { Phase = LSDFAirComboPhase.BlownAway }, settings);
    Assert.AreEqual(savedGetUp, first.Shape.LocalTransform.Position);
    Assert.AreEqual(savedFlight, second.Shape.LocalTransform.Position);
    Assert.AreEqual(savedGetUp, settings.GetUpColliderCenter); Assert.AreEqual(savedFlight, settings.EnderColliderCenter);
  }
}
