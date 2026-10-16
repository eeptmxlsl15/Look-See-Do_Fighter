namespace Quantum.LSDF {
  public enum LSDFRoundPhase { WaitingForPlayers, Fighting, RoundEnd, Intro, MatchEnd }
  public enum LSDFRoundOutcome { None, FirstWins, SecondWins, Draw }

  public static class LSDFRoundPolicy {
    public const int WinsRequired = 3;
    public static LSDFRoundPhase Phase(bool hasPair, in LSDF_Player first, in LSDF_Player second, int tick) {
      if (!hasPair) return LSDFRoundPhase.WaitingForPlayers;
      if (first.isRoundEnd || second.isRoundEnd) {
        if (Outcome(first, second) == LSDFRoundOutcome.None) return LSDFRoundPhase.Intro;
        return IsMatchOver(first, second) ? LSDFRoundPhase.MatchEnd : LSDFRoundPhase.RoundEnd;
      }
      return LSDFRoundPhase.Fighting;
    }
    public static bool IsMatchOver(in LSDF_Player first, in LSDF_Player second) {
      return first.loseRound >= WinsRequired || second.loseRound >= WinsRequired;
    }
    public static int RoundNumber(in LSDF_Player first, in LSDF_Player second) {
      return System.Math.Min(5, first.loseRound + second.loseRound + 1);
    }
    public static bool IsKnockedOut(in LSDF_Player player) { return player.playerHp <= 0; }
    public static LSDFRoundOutcome Outcome(in LSDF_Player first, in LSDF_Player second) {
      if (!first.isRoundEnd || !second.isRoundEnd) return LSDFRoundOutcome.None;
      if (IsKnockedOut(first) && IsKnockedOut(second)) return LSDFRoundOutcome.Draw;
      if (IsKnockedOut(second)) return LSDFRoundOutcome.FirstWins;
      if (IsKnockedOut(first)) return LSDFRoundOutcome.SecondWins;
      return LSDFRoundOutcome.None;
    }

    // Exactly once, at the existing final IngameSystem update after this tick's contacts.
    public static bool TryFinish(ref LSDF_Player first, ref LSDF_Player second, int tick = 0, int resultTicks = 120) {
      if (first.isRoundEnd || second.isRoundEnd || (!IsKnockedOut(first) && !IsKnockedOut(second))) return false;
      bool firstKo = IsKnockedOut(first), secondKo = IsKnockedOut(second);
      // A simultaneous KO is a draw, not one win for each fighter.
      if (firstKo && !secondKo) first.loseRound++;
      if (secondKo && !firstKo) second.loseRound++;
      FinishPose(ref first);
      FinishPose(ref second);
      first.tickToEnableMove = second.tickToEnableMove = tick + System.Math.Max(30, resultTicks);
      return true;
    }

    public static bool TryBeginNextRound(ref LSDF_Player first, ref LSDF_Player second, int tick,
        int maximumHealth, int introTicks) {
      if (Phase(true, first, second, tick) != LSDFRoundPhase.RoundEnd ||
          tick < first.tickToEnableMove || tick < second.tickToEnableMove) return false;
      ResetForIntro(ref first, maximumHealth, tick + System.Math.Max(1, introTicks));
      ResetForIntro(ref second, maximumHealth, tick + System.Math.Max(1, introTicks));
      return true;
    }

    private static unsafe void ResetForIntro(ref LSDF_Player player, int maximumHealth, int unlockTick) {
      var previous = player;
      player = default;
      player.loseRound = previous.loseRound;
      for (int slot = 0; slot < LSDFLoadout.SlotCount; slot++) player.CommandSkillMap[slot] = previous.CommandSkillMap[slot];
      player.playerHp = System.Math.Max(1, maximumHealth);
      player.tickToEnableMove = unlockTick;
      // HP > 0 with isRoundEnd is the between-round Intro, never a new KO.
      FinishPose(ref player);
    }

    public static bool TryStartFighting(ref LSDF_Player first, ref LSDF_Player second, int tick) {
      if (Phase(true, first, second, tick) != LSDFRoundPhase.Intro ||
          tick < first.tickToEnableMove || tick < second.tickToEnableMove) return false;
      first.isRoundEnd = second.isRoundEnd = false;
      first.isCantMove = second.isCantMove = false;
      return true;
    }

    public static void FinishPose(ref LSDF_Player player) {
      if (player.playerHp < 0) player.playerHp = 0;
      player.isRoundEnd = true;
      player.isSit = IsKnockedOut(player);
      player.isCantMove = true;
      player.isHealing = false;
      player.isAttack = player.isHit = player.isGuard = player.isStun = false;
      player.isCombo = player.isAir = player.isJump = player.isWallHit = false;
      player.isParring = player.canCounter = player.isDodgeHigh = false;
      player.isDashBack = player.isDashFront = player.isStandUpGuard = false;
      player.hitWallLauncher = player.BufferedNextAttack = false;
      player.forceBack = 0;
    }
  }
}
