using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Stateless health rules. The caller supplies the current Frame-owned component.</summary>
  public static class LSDFPlayerHealth {
    public const int Maximum = 170;
    public const int HealingPerTick = 2;

    // Called at the original PlayerSystem update point, before movement/recovery locks.
    public static void UpdateRecovery(ref LSDF_Player player, int maximum = Maximum, int healing = HealingPerTick) {
      if (player.playerHp < 0) player.playerHp = 0;
      if (player.playerHp == 0 || player.isRoundEnd) { player.isHealing = false; return; }
      if (!player.isHealing) return;
      player.playerHp += healing;
      if (player.playerHp >= maximum) {
        player.playerHp = maximum;
        player.isHealing = false;
      }
    }

    public static int NormalHitDamage(int baseDamage, int hitCount) {
      if (hitCount == 0) return baseDamage;
      if (hitCount == 2) return (int)(baseDamage * FP._0_75);
      if (hitCount == 4) return (int)(baseDamage * FP._0_50);
      if (hitCount > 4) return (int)(baseDamage * FP._0_33);
      // Preserve legacy gaps (including 1 and 3); changing combo balance is a separate task.
      return 0;
    }

    public static void ApplyNormalHit(ref LSDF_Player player, int baseDamage) {
      if (player.playerHp <= 0 || player.isRoundEnd) return;
      player.playerHp = System.Math.Max(0, player.playerHp - NormalHitDamage(baseDamage, player.hitCount));
    }

    public static void ApplyCounterHit(ref LSDF_Player player, int baseDamage) {
      // Counter damage has never used combo scaling.
      if (player.playerHp <= 0 || player.isRoundEnd) return;
      player.playerHp = System.Math.Max(0, player.playerHp - LSDFCombatMath.CounterDamage(baseDamage));
    }
  }
}
