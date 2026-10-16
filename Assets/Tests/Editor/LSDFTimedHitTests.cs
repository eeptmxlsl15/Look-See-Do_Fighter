using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFTimedHitTests {
  [TestCase(false, false, 17, 18, 15, 20, 20)]
  [TestCase(false, true, 35, 18, 27, 20, 29)]
  [TestCase(true, false, 15, 16, 12, 21, 28)]
  [TestCase(true, true, 25, 16, 14, 21, 28)]
  public void SerializedTimedHitPreservesScheduleAndPayload(bool lars, bool second,
      int spawn, int startup, int damage, int guard, int hitTicks) {
    LSDFTimedHitDefinition step;
    if (lars) {
      var asset = AssetDatabase.LoadAssetAtPath<LarsCutKickWindowEvent>(
        "Assets/QuantumUser/Resources/LSDF_Animator Assets/Lars Cut Kick Window Event.asset");
      Assert.IsNotNull(asset);
      step = second ? asset.SecondHit : asset.FirstHit;
    } else {
      var asset = AssetDatabase.LoadAssetAtPath<NarakTwoWindowEvent>(
        "Assets/QuantumUser/Resources/LSDF_Animator Assets/Narak Two Window Event.asset");
      Assert.IsNotNull(asset);
      step = second ? asset.SecondHit : asset.FirstHit;
    }
    Assert.IsNotNull(step);
    Assert.IsNotNull(step.Hit);
    Assert.AreEqual(spawn, step.SpawnAnimationFrame);
    Assert.IsTrue(step.IsSpawnFrame(spawn));
    Assert.IsFalse(step.IsSpawnFrame(spawn - 1));
    Assert.IsFalse(step.IsSpawnFrame(spawn + 1));
    // A skipped/interrupted animation does not enqueue an overdue follow-up hit.
    Assert.IsFalse(step.IsSpawnFrame(spawn + 20));
    Assert.AreEqual(spawn + 1, LSDFHitboxFactory.DestroyTick(spawn));
    var hit = step.Hit;
    var origin = new FPVector2(2, 3);
    bool low = !lars && !second;
    var offset = new FPVector2(FP._0_25, low ? -(FP._0_25 + FP._0_03) : FP._0);
    Assert.AreEqual(offset, hit.Offset);
    Assert.AreEqual(new FPVector2(FP._0_10 / 2, low ? (FP._0_10 - FP._0_02) / 2 :
      (FP._0_33 - FP._0_03) / 2), hit.HalfExtents);
    Assert.AreEqual(origin + offset, hit.PositionAt(origin, 1));
    Assert.AreEqual(origin + new FPVector2(-offset.X, offset.Y), hit.PositionAt(origin, -1));
    var info = hit.CreateInfo(default, origin);
    Assert.AreEqual(startup, info.startFrame); // Second hit retains the original first-hit startup metadata.
    Assert.AreEqual(damage, info.attackDamage);
    Assert.AreEqual(guard, info.enemyGuardTime);
    Assert.AreEqual(hitTicks, info.enemyHitTime);
    Assert.AreEqual(hitTicks, info.enemyCountTime);
    Assert.AreEqual(low ? HitboxAttackType.Low : HitboxAttackType.Mid, info.AttackType);
    Assert.AreEqual(lars ? CountAttackType.Air : CountAttackType.Normal, info.CountType);
    Assert.AreEqual(lars ? DelayGuardType.Stun : DelayGuardType.Combo, info.DelayGuardTpye);
    Assert.AreEqual(lars ? HomingType.Stun : HomingType.Combo, info.HomingReturnType);
    Assert.AreEqual(lars, (bool)info.jumpAttack);
    Assert.AreEqual(lars || low, (bool)info.dodgeHigh);
    Assert.AreEqual(lars, (bool)info.launcher);
    Assert.IsFalse(info.wallLauncher || info.notSitLauncher);
    Assert.AreEqual(FP._0, info.forceBack);
  }

  [Test]
  public void RequiredQuantumLookupTablesAreConfigured() {
    var tables = AssetDatabase.LoadAssetAtPath<QuantumLookupTables>(
      "Assets/QuantumUser/Resources/QuantumLookupTables.asset");
    Assert.IsNotNull(tables);
    Assert.IsNotNull(tables.TableSinCos);
    Assert.IsNotNull(tables.TableTan);
    Assert.IsNotNull(tables.TableAsin);
    Assert.IsNotNull(tables.TableAcos);
    Assert.IsNotNull(tables.TableAtan);
    Assert.IsNotNull(tables.TableSqrt);
  }
}
