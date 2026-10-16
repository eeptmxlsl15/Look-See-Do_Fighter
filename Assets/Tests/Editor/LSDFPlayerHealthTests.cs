using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;

public class LSDFPlayerHealthTests {
  [TestCase(7, 0, 7)]
  [TestCase(7, 1, 0)]
  [TestCase(7, 2, 5)]
  [TestCase(7, 3, 0)]
  [TestCase(7, 4, 3)]
  [TestCase(7, 5, 2)]
  [TestCase(3, 5, 0)] // FP._0_33 is raw 21845: three times it stays below one.
  [TestCase(6, 5, 1)]
  [TestCase(12, 6, 3)]
  [TestCase(15, 7, 4)]
  [TestCase(17, 2, 12)]
  [TestCase(17, 4, 8)]
  [TestCase(7, -1, 0)]
  public void NormalDamageKeepsLegacyComboBoundaries(int damage, int hitCount, int expected) {
    Assert.AreEqual(expected, LSDFPlayerHealth.NormalHitDamage(damage, hitCount));
  }

  [TestCase(-5, false, 0, false)]
  [TestCase(-5, true, 2, true)]
  [TestCase(167, true, 169, true)]
  [TestCase(168, true, 170, false)]
  [TestCase(169, true, 170, false)]
  [TestCase(170, true, 170, false)]
  [TestCase(175, true, 170, false)]
  [TestCase(175, false, 175, false)]
  public void RecoveryKeepsClampOrderAndHealingCompletion(int before, bool healing, int after, bool remainsHealing) {
    var player = new LSDF_Player { playerHp = before, isHealing = healing, hitCount = 4, DelayFrame = 123 };
    LSDFPlayerHealth.UpdateRecovery(ref player);
    Assert.AreEqual(after, player.playerHp);
    Assert.AreEqual(remainsHealing, (bool)player.isHealing);
    Assert.AreEqual(4, player.hitCount);
    Assert.AreEqual(123, player.DelayFrame);
  }

  [Test]
  public void HitKeepsNegativeHpUntilTheExistingUpdatePoint() {
    var player = new LSDF_Player { playerHp = 4, hitCount = 0, isHit = true, DelayFrame = 125 };
    LSDFPlayerHealth.ApplyNormalHit(ref player, 7);
    Assert.AreEqual(-3, player.playerHp);
    Assert.IsTrue(player.isHit);
    Assert.AreEqual(125, player.DelayFrame);
    LSDFPlayerHealth.UpdateRecovery(ref player);
    Assert.AreEqual(0, player.playerHp);
  }

  [Test]
  public void CounterBypassesComboScalingAndDoesNotTouchOtherPlayers() {
    var a = new LSDF_Player { playerHp = LSDFPlayerHealth.Maximum, hitCount = 5, isHealing = true };
    var b = new LSDF_Player { playerHp = LSDFPlayerHealth.Maximum, hitCount = 2 };
    LSDFPlayerHealth.ApplyCounterHit(ref a, 7);
    Assert.AreEqual(162, a.playerHp);
    Assert.AreEqual(5, a.hitCount);
    Assert.IsTrue(a.isHealing);
    Assert.AreEqual(170, b.playerHp);
    LSDFPlayerHealth.ApplyNormalHit(ref b, 7);
    Assert.AreEqual(165, b.playerHp);
    Assert.AreEqual(162, a.playerHp);
  }
}
