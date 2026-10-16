using System;
using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Serialized inside a Quantum event asset. Treat as immutable during simulation.</summary>
  [Serializable]
  public sealed class LSDFSingleHitDefinition {
    public int StartupFrames;
    public FPVector2 Offset;
    public FPVector2 HalfExtents;
    public HitboxAttackType AttackType;
    public CountAttackType CounterReaction;
    public DelayGuardType GuardReaction;
    public HomingType ParryReaction;
    public int Damage;
    public int GuardTicks;
    public int HitTicks;
    public int CounterTicks;
    public FP ForceBack;
    public bool JumpAttack;
    public bool DodgeHigh;
    public bool Launcher;

    public static LSDFSingleHitDefinition CreateJap() {
      return CreateHighPunch(10, 7, 21, 28, 28);
    }

    public static LSDFSingleHitDefinition CreateJapJap() {
      return CreateHighPunch(10, 7, 21, 28, 28);
    }

    public static LSDFSingleHitDefinition CreateJapTwo() {
      return CreateHighPunch(8, 12, 19, 22, 28);
    }

    private static LSDFSingleHitDefinition CreateHighPunch(int startup, int damage, int guard, int hit, int counter) {
      return new LSDFSingleHitDefinition {
        StartupFrames = startup,
        Offset = new FPVector2(FP._0_25 + FP._0_05, FP._0_25),
        HalfExtents = new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2),
        AttackType = HitboxAttackType.High,
        CounterReaction = CountAttackType.Normal,
        GuardReaction = DelayGuardType.Normal,
        ParryReaction = HomingType.Stun,
        Damage = damage,
        GuardTicks = guard,
        HitTicks = hit,
        CounterTicks = counter
      };
    }

    public static LSDFSingleHitDefinition CreateJapJapTwo() {
      return new LSDFSingleHitDefinition {
        StartupFrames = 5,
        Offset = new FPVector2(FP._0_25, FP._0),
        HalfExtents = new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2),
        AttackType = HitboxAttackType.Mid,
        CounterReaction = CountAttackType.Normal,
        GuardReaction = DelayGuardType.Combo,
        ParryReaction = HomingType.Combo,
        Damage = 7,
        GuardTicks = 21,
        HitTicks = 29,
        CounterTicks = 29,
        ForceBack = FP._1
      };
    }

    public static LSDFSingleHitDefinition CreateLeftUpper() {
      return new LSDFSingleHitDefinition {
        StartupFrames = 13,
        Offset = new FPVector2(FP._0_25, FP._0),
        HalfExtents = new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2),
        AttackType = HitboxAttackType.Mid,
        CounterReaction = CountAttackType.Normal,
        GuardReaction = DelayGuardType.Normal,
        ParryReaction = HomingType.Homing,
        Damage = 12,
        GuardTicks = 19,
        HitTicks = 25,
        CounterTicks = 25
      };
    }

    public bool IsSpawnFrame(int animationFrame) {
      // Physics runs before the animator: preserve the existing one-tick lead.
      return animationFrame == StartupFrames - 1;
    }

    public FPVector2 PositionAt(FPVector2 origin, int facing) {
      return origin + new FPVector2(Offset.X * facing, Offset.Y);
    }

    public LSDF_HitboxInfo CreateInfo(EntityRef attacker, FPVector2 position) {
      return new LSDF_HitboxInfo {
        position = position,
        AttackerEntity = attacker,
        startFrame = StartupFrames,
        AttackType = AttackType,
        CountType = CounterReaction,
        DelayGuardTpye = GuardReaction,
        HomingReturnType = ParryReaction,
        enemyGuardTime = GuardTicks,
        enemyHitTime = HitTicks,
        enemyCountTime = CounterTicks,
        attackDamage = Damage,
        forceBack = ForceBack,
        jumpAttack = JumpAttack,
        dodgeHigh = DodgeHigh,
        launcher = Launcher
        // Wall/not-sit flags and GuaranteeType.Normal retain their zero defaults.
      };
    }
  }
}
