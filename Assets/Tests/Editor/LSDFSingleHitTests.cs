using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFSingleHitTests {
  [Test]
  public void SerializedLeftUpperPreservesOriginalDefinition() {
    var asset = AssetDatabase.LoadAssetAtPath<LeftUpperWindowEvent>(
      "Assets/QuantumUser/Resources/LSDF_Animator Assets/Left Upper Window Event.asset");
    Assert.IsNotNull(asset);
    var hit = asset.Hit;
    Assert.IsNotNull(hit);
    Assert.AreEqual(13, hit.StartupFrames);
    Assert.AreEqual(FP._0_25, hit.Offset.X);
    Assert.AreEqual(FP._0, hit.Offset.Y);
    Assert.AreEqual(FP._0_10 / 2, hit.HalfExtents.X);
    Assert.AreEqual((FP._0_33 - FP._0_03) / 2, hit.HalfExtents.Y);
    Assert.AreEqual(12, hit.Damage);
    Assert.AreEqual(19, hit.GuardTicks);
    Assert.AreEqual(25, hit.HitTicks);
    Assert.AreEqual(25, hit.CounterTicks);
    var info = hit.CreateInfo(default, FPVector2.Zero);
    Assert.AreEqual(HitboxAttackType.Mid, info.AttackType);
    Assert.AreEqual(CountAttackType.Normal, info.CountType);
    Assert.AreEqual(DelayGuardType.Normal, info.DelayGuardTpye);
    Assert.AreEqual(HomingType.Homing, info.HomingReturnType);
    Assert.AreEqual(GuaranteeType.Normal, info.GuaranteeAttackType);
    Assert.AreEqual(FP._0, info.forceBack);
    Assert.IsFalse(info.launcher || info.wallLauncher || info.notSitLauncher || info.jumpAttack || info.dodgeHigh);
  }

  [TestCase(11, false)]
  [TestCase(12, true)]
  [TestCase(13, false)]
  public void SpawnTickPreservesPhysicsLead(int tick, bool expected) {
    Assert.AreEqual(expected, LSDFSingleHitDefinition.CreateLeftUpper().IsSpawnFrame(tick));
    Assert.AreEqual(tick + 1, LSDFHitboxFactory.DestroyTick(tick));
  }

  [Test]
  public void FacingOnlyMirrorsHorizontalOffsetWithoutMutatingDefinition() {
    var hit = LSDFSingleHitDefinition.CreateLeftUpper();
    var origin = new FPVector2(2, 3);
    var right = hit.PositionAt(origin, 1);
    var left = hit.PositionAt(origin, -1);
    Assert.AreEqual(origin.X + FP._0_25, right.X);
    Assert.AreEqual(origin.X - FP._0_25, left.X);
    Assert.AreEqual(origin.Y, right.Y);
    Assert.AreEqual(origin.Y, left.Y);
    Assert.AreEqual(FP._0_25, hit.Offset.X);
    Assert.AreEqual(hit.CreateInfo(default, right).attackDamage, hit.CreateInfo(default, left).attackDamage);
  }
}
