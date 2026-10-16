using System;
using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Immutable authored schedule; no cursor or mutable execution state lives on the asset.</summary>
  [Serializable]
  public sealed class LSDFTimedHitDefinition {
    public int SpawnAnimationFrame;
    public LSDFSingleHitDefinition Hit;

    public bool IsSpawnFrame(int animationFrame) {
      return animationFrame == SpawnAnimationFrame;
    }

    public static LSDFTimedHitDefinition CreateNarakFirst() {
      return new LSDFTimedHitDefinition {
        SpawnAnimationFrame = 17,
        Hit = new LSDFSingleHitDefinition {
          StartupFrames = 18,
          Offset = new FPVector2(FP._0_25, -(FP._0_25 + FP._0_03)),
          HalfExtents = new FPVector2(FP._0_10 / 2, (FP._0_10 - FP._0_02) / 2),
          AttackType = HitboxAttackType.Low,
          CounterReaction = CountAttackType.Normal,
          GuardReaction = DelayGuardType.Combo,
          ParryReaction = HomingType.Combo,
          Damage = 15,
          GuardTicks = 20,
          HitTicks = 20,
          CounterTicks = 20,
          DodgeHigh = true
        }
      };
    }

    public static LSDFTimedHitDefinition CreateNarakSecond() {
      return new LSDFTimedHitDefinition {
        SpawnAnimationFrame = 35,
        Hit = new LSDFSingleHitDefinition {
          // Collision policy consumes the original attack startup, not this hit's schedule.
          StartupFrames = 18,
          Offset = new FPVector2(FP._0_25, FP._0),
          HalfExtents = new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2),
          AttackType = HitboxAttackType.Mid,
          CounterReaction = CountAttackType.Normal,
          GuardReaction = DelayGuardType.Combo,
          ParryReaction = HomingType.Combo,
          Damage = 27,
          GuardTicks = 20,
          HitTicks = 29,
          CounterTicks = 29
        }
      };
    }

    public static LSDFTimedHitDefinition CreateLarsFirst() {
      return CreateLars(15, 12);
    }

    public static LSDFTimedHitDefinition CreateLarsSecond() {
      return CreateLars(25, 14);
    }

    private static LSDFTimedHitDefinition CreateLars(int spawnFrame, int damage) {
      return new LSDFTimedHitDefinition {
        SpawnAnimationFrame = spawnFrame,
        Hit = new LSDFSingleHitDefinition {
          StartupFrames = 16,
          Offset = new FPVector2(FP._0_25, FP._0),
          HalfExtents = new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2),
          AttackType = HitboxAttackType.Mid,
          CounterReaction = CountAttackType.Air,
          GuardReaction = DelayGuardType.Stun,
          ParryReaction = HomingType.Stun,
          Damage = damage,
          GuardTicks = 21,
          HitTicks = 28,
          CounterTicks = 28,
          JumpAttack = true,
          DodgeHigh = true,
          Launcher = true
        }
      };
    }
  }
}
