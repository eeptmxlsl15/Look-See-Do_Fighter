using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;

public class LSDFPlayerHealthTests {
  [TestCase(7, 0, 7)]
  [TestCase(7, 1, 4)]
  [TestCase(7, 2, 3)]
  [TestCase(7, 3, 2)]
  [TestCase(7, 4, 2)]
  [TestCase(7, 5, 2)]
  [TestCase(3, 5, 0)]
  [TestCase(6, 5, 1)]
  [TestCase(12, 6, 3)]
  [TestCase(15, 7, 4)]
  [TestCase(17, 2, 8)]
  [TestCase(17, 4, 5)]
  [TestCase(7, -1, 7)]
  [TestCase(20, 1, 14)]
  [TestCase(20, 2, 10)]
  [TestCase(20, 3, 6)]
  [TestCase(20, 4, 6)]
  [TestCase(20, 5, 6)]
  [TestCase(10, 1, 7)]
  [TestCase(10, 3, 3)]
  [TestCase(int.MaxValue, 1, 1503238552)]
  public void AirDamageUsesActualFollowupOrdinalAndTruncatesIntegerPercentages(int damage, int ordinal, int expected) {
    Assert.AreEqual(expected, LSDFPlayerHealth.NormalHitDamage(damage, ordinal));
  }

  [TestCase(-5, false, 0, false)]
  [TestCase(-5, true, 0, false)]
  [TestCase(0, true, 0, false)]
  [TestCase(167, true, 169, true)]
  [TestCase(168, true, 170, false)]
  [TestCase(169, true, 170, false)]
  [TestCase(170, true, 170, false)]
  [TestCase(175, true, 170, false)]
  [TestCase(175, false, 175, false)]
  public void RecoveryNeverRevivesKoAndKeepsLivingHealingCompletion(int before, bool healing, int after, bool remainsHealing) {
    var player = new LSDF_Player { playerHp = before, isHealing = healing, hitCount = 4, DelayFrame = 123 };
    LSDFPlayerHealth.UpdateRecovery(ref player);
    Assert.AreEqual(after, player.playerHp);
    Assert.AreEqual(remainsHealing, (bool)player.isHealing);
    Assert.AreEqual(4, player.hitCount);
    Assert.AreEqual(123, player.DelayFrame);
  }

  [Test]
  public void LethalHitClampsHpImmediatelyAndCannotBeHealedBack() {
    var player = new LSDF_Player { playerHp = 4, hitCount = 0, isHit = true, DelayFrame = 125 };
    LSDFPlayerHealth.ApplyNormalHit(ref player, 7);
    Assert.AreEqual(0, player.playerHp);
    Assert.IsTrue(player.isHit);
    Assert.AreEqual(125, player.DelayFrame);
    player.isHealing = true;
    LSDFPlayerHealth.UpdateRecovery(ref player);
    Assert.AreEqual(0, player.playerHp);
    Assert.IsFalse(player.isHealing);
  }

  [Test]
  public void GroundCounterKeepsItsMultiplierAndGroundHitIgnoresLegacyEntryCount() {
    var a = new LSDF_Player { playerHp = LSDFPlayerHealth.Maximum, hitCount = 5, isHealing = true };
    var b = new LSDF_Player { playerHp = LSDFPlayerHealth.Maximum, hitCount = 2 };
    LSDFPlayerHealth.ApplyCounterHit(ref a, 7);
    Assert.AreEqual(162, a.playerHp);
    Assert.AreEqual(5, a.hitCount);
    Assert.IsTrue(a.isHealing);
    Assert.AreEqual(170, b.playerHp);
    LSDFPlayerHealth.ApplyNormalHit(ref b, 7);
    Assert.AreEqual(163, b.playerHp);
    Assert.AreEqual(162, a.playerHp);
  }
}
