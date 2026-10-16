namespace Quantum.LSDF {
  public enum LSDFContactReaction { Ignore, Normal, Counter, StunHit, StunGuard, StunIgnore, ComboHit, Guard, ParryAttacker }

  /// <summary>Legacy collision priority, independent of pointers and presentation.</summary>
  public static class LSDFContactPolicy {
    public static LSDFContactReaction Evaluate(in LSDF_Player defender, in LSDF_HitboxInfo hit, string state) {
      if ((defender.isDodgeHigh && hit.AttackType == HitboxAttackType.High) ||
          (defender.isJump && hit.AttackType == HitboxAttackType.Low)) return LSDFContactReaction.Ignore;
      if ((defender.hitWallLauncher || defender.isAir) && defender.isOnWall) return LSDFContactReaction.Normal;
      if (defender.canCounter) return LSDFContactReaction.Counter;
      if (state == "Stun") {
        if (hit.startFrame <= 10) return LSDFContactReaction.StunHit;
        return hit.AttackType == HitboxAttackType.High || hit.AttackType == HitboxAttackType.Mid || hit.AttackType == HitboxAttackType.Low
          ? LSDFContactReaction.StunGuard : LSDFContactReaction.StunIgnore;
      }
      if (state == "Combo") return LSDFContactReaction.ComboHit;
      if (ShouldGuard(hit.AttackType, state, defender.isSit)) return LSDFContactReaction.Guard;
      if (state == "Parring") {
        if (hit.HomingReturnType == HomingType.Homing) return LSDFContactReaction.Normal;
        if (hit.HomingReturnType == HomingType.Stun || hit.HomingReturnType == HomingType.Combo) return LSDFContactReaction.ParryAttacker;
        return LSDFContactReaction.Ignore;
      }
      return LSDFContactReaction.Normal;
    }
    public static bool ShouldGuard(HitboxAttackType type, string state, bool sitting) {
      if (type == HitboxAttackType.High || type == HitboxAttackType.Mid)
        return state == "Idle" || state == "Move Back" || state == "Dash Back" || state == "Rising" || state == "Stand Guard" || state == "Low Guard";
      return type == HitboxAttackType.Low && (state == "Sit Enter" || state == "Siting" || sitting);
    }
  }
}
