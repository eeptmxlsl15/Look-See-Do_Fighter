using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.Addons.Animator;
using Quantum.LSDF;
using UnityEditor;

public class LSDFJabDefinitionTests {
  [TestCase("Jap", 10, 7, 21, 28, 28)]
  [TestCase("Jap Jap", 10, 7, 21, 28, 28)]
  [TestCase("Jap Two", 8, 12, 19, 22, 28)]
  [TestCase("Jap Jap Two", 5, 7, 21, 29, 29)]
  public void SerializedJabVariantKeepsCombatContract(string name, int startup, int damage,
      int guard, int hitTicks, int counter) {
    var asset = AssetDatabase.LoadAssetAtPath<AnimatorTimeWindowEventAsset>(
      "Assets/QuantumUser/Resources/LSDF_Animator Assets/" + name + " Window Event.asset");
    Assert.IsNotNull(asset);
    var hit = asset.GetType().GetField("Hit").GetValue(asset) as LSDFSingleHitDefinition;
    Assert.IsNotNull(hit);
    Assert.AreEqual(startup, hit.StartupFrames);
    Assert.AreEqual(damage, hit.Damage);
    Assert.AreEqual(guard, hit.GuardTicks);
    Assert.AreEqual(hitTicks, hit.HitTicks);
    Assert.AreEqual(counter, hit.CounterTicks);
    Assert.IsFalse(hit.IsSpawnFrame(startup - 2));
    Assert.IsTrue(hit.IsSpawnFrame(startup - 1));
    Assert.IsFalse(hit.IsSpawnFrame(startup));
    Assert.AreEqual(101, LSDFHitboxFactory.DestroyTick(100));

    bool finisher = name == "Jap Jap Two";
    var origin = new FPVector2(2, 3);
    var offset = finisher ? new FPVector2(FP._0_25, FP._0) :
      new FPVector2(FP._0_25 + FP._0_05, FP._0_25);
    Assert.AreEqual(offset, hit.Offset);
    Assert.AreEqual(finisher ? new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2) :
      new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2), hit.HalfExtents);
    Assert.AreEqual(origin + offset, hit.PositionAt(origin, 1));
    Assert.AreEqual(origin + new FPVector2(-offset.X, offset.Y), hit.PositionAt(origin, -1));
    var info = hit.CreateInfo(default, origin);
    Assert.AreEqual(origin, info.position);
    Assert.AreEqual(startup, info.startFrame);
    Assert.AreEqual(damage, info.attackDamage);
    Assert.AreEqual(guard, info.enemyGuardTime);
    Assert.AreEqual(hitTicks, info.enemyHitTime);
    Assert.AreEqual(counter, info.enemyCountTime);
    Assert.AreEqual(finisher ? HitboxAttackType.Mid : HitboxAttackType.High, info.AttackType);
    Assert.AreEqual(CountAttackType.Normal, info.CountType);
    Assert.AreEqual(finisher ? DelayGuardType.Combo : DelayGuardType.Normal, info.DelayGuardTpye);
    Assert.AreEqual(finisher ? HomingType.Combo : HomingType.Stun, info.HomingReturnType);
    Assert.AreEqual(finisher ? FP._1 : FP._0, info.forceBack);
    Assert.AreEqual(GuaranteeType.Normal, info.GuaranteeAttackType);
    Assert.IsFalse(info.launcher || info.wallLauncher || info.notSitLauncher || info.jumpAttack || info.dodgeHigh);
  }
}
