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

    // Zero is the ground starter; positive counts are registered airborne follow-ups.
    public static int NormalHitDamage(int baseDamage, int airComboHitCount) {
      int percent = airComboHitCount <= 0 ? 100 : airComboHitCount == 1 ? 70 : airComboHitCount == 2 ? 50 : 30;
      // Integer percentages avoid fixed-point approximation at exact boundaries (e.g. 10 * 70%).
      return (int)((long)baseDamage * percent / 100);
    }

    // Called after Juggle.OnHit has registered this contact, including the fifth-hit ender.
    public static void ApplyHit(Frame frame, EntityRef entity, ref LSDF_Player player, int baseDamage, bool counter) {
      int count = frame.TryGet<LSDF_AirCombo>(entity, out var air) && air.ComboActive ? air.HitCount : 0;
      if (counter && count <= 0) ApplyCounterHit(ref player, baseDamage);
      else ApplyNormalHit(ref player, baseDamage, count);
    }

    public static void ApplyNormalHit(ref LSDF_Player player, int baseDamage, int airComboHitCount = 0) {
      if (player.playerHp <= 0 || player.isRoundEnd) return;
      player.playerHp = System.Math.Max(0, player.playerHp - NormalHitDamage(baseDamage, airComboHitCount));
    }

    public static void ApplyCounterHit(ref LSDF_Player player, int baseDamage) {
      // Ground counters retain their existing multiplier; airborne contacts use ApplyHit's scaling.
      if (player.playerHp <= 0 || player.isRoundEnd) return;
      player.playerHp = System.Math.Max(0, player.playerHp - LSDFCombatMath.CounterDamage(baseDamage));
    }
  }
}
