using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEngine;

public class LSDFDeterministicContractTests {
  [TestCase(0)]
  [TestCase(27)]
  [TestCase(29)]
  public void MalformedPayloadUsesDefaultLoadout(int length) {
    var skills = new int[length];
    for (int i = 0; i < length; ++i) skills[i] = 1;
    CollectionAssert.AreEqual(new int[28], LSDFLoadout.CopyValidated(skills));
  }

  [Test]
  public void NullPayloadAndInvalidSlotsAreSafe() {
    CollectionAssert.AreEqual(new int[28], LSDFLoadout.CopyValidated(null));
    Assert.AreEqual(0, LSDFLoadout.ReadSkill(new int[28], -1));
    Assert.AreEqual(0, LSDFLoadout.ReadSkill(new int[28], 28));
  }

  [Test]
  public void InvalidIdsFallbackAndPayloadDoesNotAlias() {
    var input = new int[28];
    input[0] = 1;
    input[4] = 2;
    input[17] = 2;
    input[21] = 1;
    input[11] = 1;
    input[1] = int.MaxValue;
    input[2] = -1;
    var result = LSDFLoadout.CopyValidated(input);
    Assert.AreEqual(1, result[0]);
    Assert.AreEqual(2, result[4]);
    Assert.AreEqual(2, result[17]);
    Assert.AreEqual(1, result[21]);
    Assert.AreEqual(1, result[11]);
    Assert.AreEqual(0, result[1]);
    Assert.AreEqual(0, result[2]);
    input[0] = 0;
    Assert.AreEqual(1, result[0]);
  }

  [Test]
  public void RuntimePlayerJsonRoundtripKeepsIndependentLoadouts() {
    var a = new RuntimePlayer { CommandSkills = new int[28] };
    var b = new RuntimePlayer { CommandSkills = new int[28] };
    a.CommandSkills[4] = 2;
    var receivedA = JsonUtility.FromJson<RuntimePlayer>(JsonUtility.ToJson(a));
    var receivedB = JsonUtility.FromJson<RuntimePlayer>(JsonUtility.ToJson(b));
    Assert.AreEqual(2, LSDFLoadout.ReadSkill(receivedA.CommandSkills, 4));
    Assert.AreEqual(0, LSDFLoadout.ReadSkill(receivedB.CommandSkills, 4));
  }

  [TestCase(0, 0)]
  [TestCase(65535, 59)]
  [TestCase(65536, 60)]
  [TestCase(65537, 60)]
  [TestCase(10922, 9)]
  [TestCase(10923, 10)]
  public void AnimationFrameUsesExactFixedPointBoundary(long raw, int expected) {
    Assert.AreEqual(expected, LSDFCombatMath.AnimationFrame(FP.FromRaw(raw)));
  }

  [TestCase(0, 0)]
  [TestCase(7, 8)]
  [TestCase(10, 12)]
  [TestCase(19, 22)]
  [TestCase(170, 204)]
  public void CounterDamageUsesTruncatedRationalScaling(int damage, int expected) {
    Assert.AreEqual(expected, LSDFCombatMath.CounterDamage(damage));
  }

  [Test]
  public unsafe void ComboReservationBelongsToEachPlayerValue() {
    var a = new LSDF_Player();
    var b = new LSDF_Player();
    a.BufferedNextAttack = true;
    Assert.IsTrue(a.BufferedNextAttack);
    Assert.IsFalse(b.BufferedNextAttack);
    // Value-copy is a unit-level contract, not a substitute for a session rollback test.
    var snapshot = a;
    a.BufferedNextAttack = false;
    a = snapshot;
    Assert.IsTrue(a.BufferedNextAttack);
  }
}
