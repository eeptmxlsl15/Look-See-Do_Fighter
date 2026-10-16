using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFRoundResultTests {
  [TestCase(0, 170, LSDFRoundOutcome.SecondWins)]
  [TestCase(-12, 1, LSDFRoundOutcome.SecondWins)]
  [TestCase(170, 0, LSDFRoundOutcome.FirstWins)]
  [TestCase(1, -12, LSDFRoundOutcome.FirstWins)]
  [TestCase(0, 0, LSDFRoundOutcome.Draw)]
  [TestCase(-1, -12, LSDFRoundOutcome.Draw)]
  public void KoHasOnePersistentOutcomeAndMatchingPose(int firstHp, int secondHp, LSDFRoundOutcome expected) {
    var first = new LSDF_Player { playerHp = firstHp, isHealing = true, isAttack = true };
    var second = new LSDF_Player { playerHp = secondHp, isSit = true, isHit = true };
    Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref first, ref second));
    Assert.AreEqual(expected, LSDFRoundPolicy.Outcome(first, second));
    Assert.AreEqual(firstHp <= 0, (bool)first.isSit);
    Assert.AreEqual(secondHp <= 0, (bool)second.isSit);
    Assert.AreEqual(System.Math.Max(0, firstHp), first.playerHp);
    Assert.AreEqual(System.Math.Max(0, secondHp), second.playerHp);
    Assert.IsTrue(first.isCantMove && second.isCantMove);
    Assert.IsFalse(first.isAttack || first.isHealing || second.isHit);
    int losses = first.loseRound + second.loseRound;
    Assert.AreEqual(expected == LSDFRoundOutcome.Draw ? 0 : 1, losses);
    for (int i = 0; i < 400; i++) {
      Assert.IsFalse(LSDFRoundPolicy.TryFinish(ref first, ref second));
      LSDFPlayerHealth.UpdateRecovery(ref first);
      LSDFPlayerHealth.UpdateRecovery(ref second);
    }
    Assert.AreEqual(losses, first.loseRound + second.loseRound);
    Assert.AreEqual(expected, LSDFRoundPolicy.Outcome(first, second));
    Assert.AreEqual(System.Math.Max(0, firstHp), first.playerHp);
    Assert.AreEqual(System.Math.Max(0, secondHp), second.playerHp);
  }

  [Test]
  public void LivingFightersDoNotFinishAndNoWinnerExistsBeforeFinalization() {
    var a = new LSDF_Player { playerHp = 1 }; var b = new LSDF_Player { playerHp = 170 };
    Assert.IsFalse(LSDFRoundPolicy.TryFinish(ref a, ref b));
    Assert.IsFalse(a.isRoundEnd || b.isRoundEnd);
    a.playerHp = 0;
    Assert.AreEqual(LSDFRoundOutcome.None, LSDFRoundPolicy.Outcome(a, b));
  }

  [TestCase(0)] [TestCase(170)]
  public void EndedFighterIgnoresDamageHealingAndFreshOrBufferedCommands(int health) {
    var a = new LSDF_Player { playerHp = health, isRoundEnd = true, isHealing = true };
    LSDFPlayerHealth.ApplyNormalHit(ref a, 999);
    LSDFPlayerHealth.ApplyCounterHit(ref a, 999);
    LSDFPlayerHealth.UpdateRecovery(ref a);
    Assert.AreEqual(health, a.playerHp); Assert.IsFalse(a.isHealing);
    var buffer = new LSDF_CommandBuffer { HasPending = true, Dispatching = true, InputTick = 999 };
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true }, a, 1000, 10);
    Assert.IsFalse(buffer.HasPending || buffer.Dispatching);
    Assert.IsFalse(LSDFInputBuffer.CanExecute(a, 100000));
    Assert.IsFalse(LSDFInputBuffer.TryRoute(ref buffer, a, 100000, 10, true, out _, out _));
    Assert.AreEqual(LSDFContactReaction.Ignore, LSDFContactPolicy.Evaluate(a, default, "Siting"));
  }

  [Test]
  public void TerminalPoseCancelsComboDashAndCounterWindowsWithoutChangingLoadout() {
    var a = new LSDF_Player { playerHp = 50, isAir = true, isJump = true, isWallHit = true,
      isDashFront = true, isDashBack = true, isDodgeHigh = true, canCounter = true,
      isParring = true, BufferedNextAttack = true, forceBack = 2, hitCount = 4 };
    LSDFRoundPolicy.FinishPose(ref a);
    Assert.IsFalse(a.isAir || a.isJump || a.isWallHit || a.isDashFront || a.isDashBack);
    Assert.IsFalse(a.isDodgeHigh || a.canCounter || a.isParring || a.BufferedNextAttack);
    Assert.AreEqual(0, a.forceBack.RawValue); Assert.AreEqual(4, a.hitCount);
    Assert.AreEqual(50, a.playerHp); Assert.IsFalse(a.isSit);
  }

  [Test]
  public void ResultPosesUseTheExistingQuantumGraphAndFadingIsEnabled() {
    var graph = AssetDatabase.LoadAssetAtPath<AnimatorGraph>("Assets/QuantumUser/Animators/LSDF_PlayerAnimatorGraph.asset");
    Assert.IsNotNull(graph); Assert.IsTrue(graph.AllowFadeToTransitions);
    bool standing = false, sitting = false, entering = false;
    foreach (var state in graph.Layers[0].States) {
      if (state.Name == "Idle") standing = true;
      if (state.Name == "Siting") sitting = true;
      if (state.Name == "Sit Enter") {
        entering = true;
        bool exitToSit = false;
        foreach (var transition in state.Transitions)
          if (transition.DestinationStateName == "Siting" && transition.HasExitTime) exitToSit = true;
        Assert.IsTrue(exitToSit, "KO must play Sit Enter then use the existing timed transition.");
      }
    }
    Assert.IsTrue(standing && sitting && entering);
  }

  [Test]
  public unsafe void NextRoundResetsHealthAndCombatStateButKeepsScoreAndLoadouts() {
    var a = new LSDF_Player { playerHp = 0, hitCount = 4, wallCount = 2, BufferedNextAttack = true };
    var b = new LSDF_Player { playerHp = 20, isSit = true };
    a.CommandSkillMap[4] = 2; b.CommandSkillMap[0] = 1;
    Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref a, ref b, 100, 120));
    Assert.IsFalse(LSDFRoundPolicy.TryBeginNextRound(ref a, ref b, 219, 170, 150));
    Assert.IsTrue(LSDFRoundPolicy.TryBeginNextRound(ref a, ref b, 220, 170, 150));
    Assert.AreEqual(170, a.playerHp); Assert.AreEqual(170, b.playerHp);
    Assert.AreEqual(1, a.loseRound); Assert.AreEqual(0, b.loseRound);
    Assert.AreEqual(2, a.CommandSkillMap[4]); Assert.AreEqual(1, b.CommandSkillMap[0]);
    Assert.AreEqual(0, a.hitCount); Assert.AreEqual(0, a.wallCount); Assert.IsFalse(a.BufferedNextAttack);
    Assert.IsFalse(a.isSit || b.isSit || a.isHealing || b.isHealing);
    Assert.AreEqual(LSDFRoundPhase.Intro, LSDFRoundPolicy.Phase(true, a, b, 220));
    Assert.AreEqual(2, LSDFRoundPolicy.RoundNumber(a, b));
    Assert.IsFalse(LSDFInputBuffer.CanExecute(b, 369));
    Assert.IsFalse(LSDFRoundPolicy.TryStartFighting(ref a, ref b, 369));
    Assert.IsTrue(LSDFRoundPolicy.TryStartFighting(ref a, ref b, 370));
    Assert.IsFalse(LSDFRoundPolicy.TryStartFighting(ref a, ref b, 371));
    Assert.IsTrue(LSDFInputBuffer.CanExecute(b, 370));
    Assert.AreEqual(LSDFRoundPhase.Fighting, LSDFRoundPolicy.Phase(true, a, b, 370));
  }

  [Test]
  public void EveryWinSequenceEndsAtThreeWinsWithinFiveDecisiveRounds() {
    for (int sequence = 0; sequence < 32; sequence++) {
      var a = new LSDF_Player { playerHp = 170 }; var b = a;
      int rounds = 0, tick = 0;
      while (!LSDFRoundPolicy.IsMatchOver(a, b)) {
        Assert.Less(rounds, 5);
        if ((sequence & (1 << rounds)) == 0) a.playerHp = 0; else b.playerHp = 0;
        Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref a, ref b, tick, 30));
        rounds++; tick += 30;
        if (LSDFRoundPolicy.IsMatchOver(a, b)) break;
        Assert.IsTrue(LSDFRoundPolicy.TryBeginNextRound(ref a, ref b, tick, 170, 1));
        Assert.IsTrue(LSDFRoundPolicy.TryStartFighting(ref a, ref b, ++tick));
      }
      Assert.AreEqual(3, System.Math.Max(a.loseRound, b.loseRound));
      Assert.AreEqual(LSDFRoundPhase.MatchEnd, LSDFRoundPolicy.Phase(true, a, b, tick));
      Assert.IsFalse(LSDFRoundPolicy.TryBeginNextRound(ref a, ref b, 99999, 170, 150));
      Assert.IsFalse(LSDFRoundPolicy.TryStartFighting(ref a, ref b, 99999));
      Assert.AreEqual(rounds, a.loseRound + b.loseRound);
    }
  }

  [Test]
  public void DrawReplaysTheSameRoundWithoutAddingEitherScore() {
    var a = new LSDF_Player { playerHp = 0, loseRound = 2 };
    var b = new LSDF_Player { playerHp = 0, loseRound = 2 };
    Assert.IsTrue(LSDFRoundPolicy.TryFinish(ref a, ref b, 100, 30));
    Assert.AreEqual(LSDFRoundOutcome.Draw, LSDFRoundPolicy.Outcome(a, b));
    Assert.IsFalse(LSDFRoundPolicy.IsMatchOver(a, b));
    Assert.IsTrue(LSDFRoundPolicy.TryBeginNextRound(ref a, ref b, 130, 170, 150));
    Assert.AreEqual(2, a.loseRound); Assert.AreEqual(2, b.loseRound);
    Assert.AreEqual(5, LSDFRoundPolicy.RoundNumber(a, b));
  }

  [TestCase(LSDFRoundOutcome.FirstWins, false, "ROUND WIN")]
  [TestCase(LSDFRoundOutcome.SecondWins, false, "ROUND LOSE")]
  [TestCase(LSDFRoundOutcome.FirstWins, true, "YOU WIN")]
  [TestCase(LSDFRoundOutcome.SecondWins, true, "YOU LOSE")]
  [TestCase(LSDFRoundOutcome.Draw, false, "DRAW")]
  public void FinalVictoryMessageIsReservedForMatchEnd(LSDFRoundOutcome outcome, bool matchOver, string expected) {
    Assert.AreEqual(expected, LSDF_IngameUI.ResultMessage(outcome, matchOver));
  }
}
