namespace Quantum.LSDF {
  public enum LSDFRoundPhase { WaitingForPlayers, Fighting, Recovering }

  public static class LSDFRoundPolicy {
    public static LSDFRoundPhase Phase(bool hasPair, in LSDF_Player first, in LSDF_Player second, int tick) {
      if (!hasPair) return LSDFRoundPhase.WaitingForPlayers;
      if ((first.isRoundEnd || second.isRoundEnd) && (tick < first.tickToEnableMove || tick < second.tickToEnableMove))
        return LSDFRoundPhase.Recovering;
      return LSDFRoundPhase.Fighting;
    }
    public static bool IsKnockedOut(in LSDF_Player player) { return player.playerHp <= 0; }
    public static void BeginRecovery(ref LSDF_Player player, int tick, int lockTicks) {
      if (IsKnockedOut(player)) { player.loseRound++; player.playerHp = 1; }
      player.isRoundEnd = true;
      player.tickToEnableMove = tick + lockTicks;
    }
  }
}
