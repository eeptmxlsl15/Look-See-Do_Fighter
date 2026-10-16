using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using UnityEngine;

public class LSDFRemainingAttackTests {
  [Serializable] public class Contracts { public Contract[] attacks; }
  [Serializable] public class Contract {
    public string type, asset, sourceSha256;
    public bool zeroEffect;
    public int colliderAdds, StartupFrames, OffsetX, OffsetY, HalfX, HalfY;
    public int AttackType, CounterReaction, GuardReaction, ParryReaction, GuaranteeReaction;
    public int JumpAttack, DodgeHigh, Launcher, WallLauncher, NotSitLauncher;
    public int GuardTicks, HitTicks, CounterTicks, Damage;
    public long ForceBack;
  }
  public static IEnumerable Cases() {
    var path = Path.Combine(Application.dataPath, "../docs/ai/evidence/remaining-attack-contracts.json");
    var contracts = JsonUtility.FromJson<Contracts>(File.ReadAllText(path));
    foreach (var contract in contracts.attacks) yield return new TestCaseData(contract).SetName("LegacyContract_" + contract.type);
  }
  [TestCaseSource(nameof(Cases))]
  public void SerializedAttackPreservesCapturedCombatContract(Contract expected) {
    var asset = AssetDatabase.LoadAssetAtPath<AssetObject>(expected.asset);
    Assert.IsNotNull(asset);
    Assert.AreEqual(expected.type, asset.GetType().Name);
    var hit = (LSDFSingleHitDefinition)asset.GetType().GetField("Hit").GetValue(asset);
    Assert.IsNotNull(hit);
    Assert.AreEqual(expected.StartupFrames, hit.StartupFrames);
    Assert.AreEqual(expected.OffsetX, hit.Offset.X.RawValue);
    Assert.AreEqual(expected.OffsetY, hit.Offset.Y.RawValue);
    Assert.AreEqual(expected.HalfX, hit.HalfExtents.X.RawValue);
    Assert.AreEqual(expected.HalfY, hit.HalfExtents.Y.RawValue);
    Assert.IsFalse(hit.IsSpawnFrame(expected.StartupFrames - 2));
    Assert.IsTrue(hit.IsSpawnFrame(expected.StartupFrames - 1));
    Assert.IsFalse(hit.IsSpawnFrame(expected.StartupFrames));
    var info = hit.CreateInfo(default, expected.zeroEffect ? FPVector2.Zero : hit.PositionAt(FPVector2.Zero, 1));
    Assert.AreEqual(expected.AttackType, (int)info.AttackType);
    Assert.AreEqual(expected.CounterReaction, (int)info.CountType);
    Assert.AreEqual(expected.GuardReaction, (int)info.DelayGuardTpye);
    Assert.AreEqual(expected.ParryReaction, (int)info.HomingReturnType);
    Assert.AreEqual(expected.GuaranteeReaction, (int)info.GuaranteeAttackType);
    Assert.AreEqual(expected.Damage, info.attackDamage);
    Assert.AreEqual(expected.GuardTicks, info.enemyGuardTime);
    Assert.AreEqual(expected.HitTicks, info.enemyHitTime);
    Assert.AreEqual(expected.CounterTicks, info.enemyCountTime);
    Assert.AreEqual(expected.ForceBack, info.forceBack.RawValue);
    Assert.AreEqual(expected.JumpAttack != 0, (bool)info.jumpAttack);
    Assert.AreEqual(expected.DodgeHigh != 0, (bool)info.dodgeHigh);
    Assert.AreEqual(expected.Launcher != 0, (bool)info.launcher);
    Assert.AreEqual(expected.WallLauncher != 0, (bool)info.wallLauncher);
    Assert.AreEqual(expected.NotSitLauncher != 0, (bool)info.notSitLauncher);
    var left = hit.PositionAt(FPVector2.Zero, -1);
    Assert.AreEqual(-expected.OffsetX, left.X.RawValue);
    Assert.AreEqual(expected.OffsetY, left.Y.RawValue);
  }
}
