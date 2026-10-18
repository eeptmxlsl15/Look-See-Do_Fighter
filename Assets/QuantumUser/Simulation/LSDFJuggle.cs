using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Fixed-point vertical motion at the existing PlayerSystem update position.</summary>
  public static unsafe class LSDFJuggle {
    // Follow-up contacts only. The ground launcher starts at zero.
    public const int MaximumHits = 5;
    private static readonly LSDFJuggleSettings Defaults = new LSDFJuggleSettings();

    public static void Begin(Frame frame, EntityRef entity, bool rehit = false) {
      if (!frame.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body) ||
          !frame.Unsafe.TryGetPointer<Transform2D>(entity, out var transform)) return;
      if (!frame.Unsafe.TryGetPointer<LSDF_AirCombo>(entity, out var air)) {
        frame.Add(entity, new LSDF_AirCombo());
        air = frame.Unsafe.GetPointer<LSDF_AirCombo>(entity);
      }
      // Air animation re-entry must neither reset a trajectory nor count another hit.
      if (!rehit && air->ComboActive) return;
      FP y = transform->Position.Y;
      Begin(ref *air, ref *body, ref y, LSDFBalance.Config(frame)?.AirCombo,
        frame.Number, frame.DeltaTime);
    }

    public static void Update(Frame frame, EntityRef entity, LSDF_Player* player,
        PhysicsBody2D* body, Transform2D* transform) {
      if (!frame.Unsafe.TryGetPointer<LSDF_AirCombo>(entity, out var air)) return;
      if (player->isRoundEnd || player->playerHp <= 0) {
        Cancel(ref *air, ref *body); return;
      }
      if (BlocksHit(*air)) {
        player->isAir = true;
        player->isWallHit = player->canCounter = false;
        UpdateEnder(ref *air, ref *body, player->isGround, LSDFBalance.Config(frame)?.AirCombo, frame.Number);
        if (!BlocksHit(*air)) {
          player->isAir = player->isHit = player->isCantMove = false;
          if (frame.Unsafe.TryGetPointer<AnimatorComponent>(entity, out var animator))
            AnimatorComponent.SetBoolean(frame, animator, "YZero", true);
        }
        return;
      }
      if (!player->isAir || player->isOnWall || player->isWallHit) {
        Cancel(ref *air, ref *body, preserveCombo: player->isOnWall || player->isWallHit); return;
      }
      Step(ref *air, ref *body, transform->Position.Y, frame.Number, frame.DeltaTime,
        LSDFBalance.Config(frame)?.AirCombo);
    }

    public static void Cancel(Frame frame, EntityRef entity, bool preserveCombo = false) {
      if (!frame.Unsafe.TryGetPointer<LSDF_AirCombo>(entity, out var air)) return;
      if (frame.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) Cancel(ref *air, ref *body, preserveCombo);
      else *air = default;
    }

    public static bool BlocksHit(Frame frame, EntityRef entity) {
      return frame.TryGet<LSDF_AirCombo>(entity, out var air) && BlocksHit(air);
    }

    public static bool BlocksHit(in LSDF_AirCombo air) {
      return air.Phase == LSDFAirComboPhase.BlownAway || air.Phase == LSDFAirComboPhase.Recovering;
    }

    public static bool OnHit(Frame frame, EntityRef entity, LSDF_Player* player, AnimatorComponent* animator,
        LSDF_HitboxInfo* hit, bool counter) {
      frame.TryGet<LSDF_AirCombo>(entity, out var previous);
      bool wall = player->isOnWall || player->isWallHit;
      bool launches = counter ? hit->CountType == CountAttackType.Air :
        hit->launcher && !(hit->notSitLauncher && player->isSit);
      if (!hit->ComboFinisher && !player->isAir && !previous.ComboActive && !(launches && !wall)) return false;
      if (!frame.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body) ||
          !frame.Unsafe.TryGetPointer<Transform2D>(entity, out var transform)) return false;
      if (!frame.Unsafe.TryGetPointer<LSDF_AirCombo>(entity, out var air)) {
        frame.Add(entity, new LSDF_AirCombo());
        air = frame.Unsafe.GetPointer<LSDF_AirCombo>(entity);
      }
      int direction = frame.TryGet<PlayerLink>(entity, out var link) && link.PlayerRef == (PlayerRef)0 ? -1 : 1;
      if (frame.TryGet<Transform2D>(hit->AttackerEntity, out var attacker))
        direction = AwayDirection(transform->Position.X, attacker.Position.X, direction);
      var settings = LSDFBalance.Config(frame)?.AirCombo ?? Defaults;
      FP y = transform->Position.Y;
      bool starter = launches && !player->isAir && !previous.ComboActive;
      bool ended = RegisterHit(ref *air, ref *body, ref y, direction, hit->ComboFinisher,
        settings, frame.Number, frame.DeltaTime, starter);
      player->forceBack = ended ? FPMath.Max(FP._0, settings.EnderSpeed) : FPMath.Max(FP._0, settings.HitPushSpeed);
      if (wall && !ended) { Cancel(ref *air, ref *body, preserveCombo: true); return false; }
      player->isAir = true;
      player->isSit = player->isAttack = player->isStun = player->isCombo = player->isCantMove = false;
      player->canCounter = player->isDodgeHigh = player->isJump = false;
      if (ended) {
        player->isWallHit = player->hitWallLauncher = false;
        AnimatorComponent.ResetTrigger(frame, animator, "FirstWallHit");
        AnimatorComponent.ResetTrigger(frame, animator, "SecondWallHit");
        AnimatorComponent.ResetTrigger(frame, animator, "ThirdWallHit");
      }
      AnimatorComponent.ResetTrigger(frame, animator, "YZero");
      return true;
    }

    public static int AwayDirection(FP defenderX, FP attackerX, int fallback) {
      return defenderX > attackerX ? 1 : defenderX < attackerX ? -1 : fallback < 0 ? -1 : 1;
    }

    public static bool RegisterHit(ref LSDF_AirCombo air, ref PhysicsBody2D body, ref FP positionY, int direction,
        bool finisher, LSDFJuggleSettings settings, int tick, FP deltaTime, bool starter = false) {
      if (BlocksHit(air)) return true;
      var config = settings ?? Defaults;
      bool active = air.ComboActive;
      int hits = (active ? air.HitCount : 0) + (starter && !active ? 0 : 1);
      int sign = direction < 0 ? -1 : 1;
      if (finisher || hits >= MaximumHits) {
        // Enders fly from the actual contact point, without the ordinary rehit reset.
        if (!active) Begin(ref air, ref body, ref positionY, config, tick, deltaTime);
        air.HitCount = System.Math.Min(MaximumHits, hits);
        air.ControlsHorizontal = true;
        air.Phase = LSDFAirComboPhase.BlownAway;
        // Keep the first ascending tick safe from a stale launch contact. Later
        // ground contact is authoritative even if the solver has added a rebound.
        air.RiseUntilTick = tick + 1;
        air.HorizontalVelocity = sign * FPMath.Max(FP._0, config.EnderSpeed);
        body.GravityScale = air.SavedGravityScale;
        body.Velocity = new FPVector2(air.HorizontalVelocity, FPMath.Max(FP._0, config.EnderUpSpeed));
        body.WakeUp();
        return true;
      }
      Begin(ref air, ref body, ref positionY, config, tick, deltaTime);
      air.HitCount = hits;
      air.ControlsHorizontal = true;
      air.HorizontalVelocity = sign * FPMath.Max(FP._0, config.HitPushSpeed);
      air.PushUntilTick = tick + System.Math.Max(0, System.Math.Min(600, config.HitPushTicks));
      ApplyHorizontal(ref air, ref body, tick);
      return false;
    }

    private static void ApplyHorizontal(ref LSDF_AirCombo air, ref PhysicsBody2D body, int tick) {
      if (air.ControlsHorizontal) body.Velocity.X = tick < air.PushUntilTick ? air.HorizontalVelocity : FP._0;
    }

    public static void UpdateEnder(ref LSDF_AirCombo air, ref PhysicsBody2D body, bool grounded,
        LSDFJuggleSettings settings, int tick) {
      var config = settings ?? Defaults;
      if (air.Phase == LSDFAirComboPhase.BlownAway) {
        body.GravityScale = air.SavedGravityScale;
        body.Velocity.X = air.HorizontalVelocity;
        if (grounded && (body.Velocity.Y <= FP._0 || tick > air.RiseUntilTick)) {
          air.Phase = LSDFAirComboPhase.Recovering;
          air.RecoveryUntilTick = tick + System.Math.Max(1, System.Math.Min(600, config.EnderRecoveryTicks));
          body.Velocity = FPVector2.Zero;
        }
      } else if (air.Phase == LSDFAirComboPhase.Recovering) {
        body.Velocity.X = FP._0;
        // Restoring the flight-only collider center can briefly open a gap above the
        // floor. Keep recovery ownership while gravity settles the normal collider;
        // re-entering flight would reapply the offset and repeat the contact cycle.
        if (!grounded) body.WakeUp();
        else if (tick >= air.RecoveryUntilTick) Cancel(ref air, ref body);
      }
    }

    public static bool ShouldLand(Frame frame, EntityRef entity, in LSDF_Player player, in PhysicsBody2D body) {
      frame.TryGet<LSDF_AirCombo>(entity, out var air);
      return ShouldLand(player, body, air);
    }

    public static bool ShouldLand(in LSDF_Player player, in PhysicsBody2D body, in LSDF_AirCombo air) {
      return player.isAir && player.isGround && body.Velocity.Y <= FP._0 &&
        (air.Phase == LSDFAirComboPhase.Inactive || air.Phase == LSDFAirComboPhase.Falling ||
         air.Phase == LSDFAirComboPhase.SlowFalling);
    }

    public static void Begin(ref LSDF_AirCombo air, ref PhysicsBody2D body, ref FP positionY,
        LSDFJuggleSettings settings, int tick, FP deltaTime) {
      var config = settings ?? Defaults;
      bool rehit = air.ComboActive;
      if (!rehit) {
        air.BaseY = positionY;
        air.SavedGravityScale = body.GravityScale;
      }
      air.ComboActive = true;
      FP maximum = FPMath.Max(FP._0_01, config.MaximumHeight);
      FP comboHeight = FPMath.Clamp(config.ComboHeight, FP._0, maximum);
      air.ComboY = air.BaseY + comboHeight;
      air.TargetY = air.BaseY + FPMath.Clamp(rehit ? comboHeight + FPMath.Max(FP._0, config.RehitLift) : config.LaunchHeight, FP._0, maximum);
      air.ReturnTicks = System.Math.Max(1, System.Math.Min(600, config.RehitReturnTicks));
      air.SlowFallSpeed = FPMath.Max(FP._0_01, config.SlowFallSpeed);
      int rise = System.Math.Max(1, System.Math.Min(600, rehit ? config.RehitRiseTicks : config.LaunchRiseTicks));
      int hold = rehit ? air.ReturnTicks : System.Math.Max(0, System.Math.Min(600, config.LaunchHoldTicks));
      air.RiseUntilTick = tick + rise;
      air.HoldUntilTick = air.RiseUntilTick + hold;
      air.Phase = rehit ? LSDFAirComboPhase.RehitRising : LSDFAirComboPhase.Rising;
      body.WakeUp();
      Step(ref air, ref body, positionY, tick, deltaTime, config);
    }

    public static void Step(ref LSDF_AirCombo air, ref PhysicsBody2D body, FP positionY, int tick, FP deltaTime,
        LSDFJuggleSettings settings = null) {
      if (BlocksHit(air)) return;
      ApplyHorizontal(ref air, ref body, tick);
      if (air.Phase == LSDFAirComboPhase.Inactive || air.Phase == LSDFAirComboPhase.Falling) return;
      if (air.Phase == LSDFAirComboPhase.SlowFalling) {
        SetSlowFall(ref air, ref body, tick, deltaTime, settings ?? Defaults);
        return;
      }
      if (air.Phase == LSDFAirComboPhase.RehitRising) {
        if (tick < air.RiseUntilTick) {
          SetTargetVelocity(ref body, air.TargetY - positionY, air.RiseUntilTick - tick, deltaTime);
          return;
        }
        air.Phase = LSDFAirComboPhase.Returning;
        body.WakeUp();
      }
      if (air.Phase == LSDFAirComboPhase.Returning) {
        if (tick < air.HoldUntilTick) {
          SetReturnVelocity(ref air, ref body, positionY, tick, deltaTime);
        } else {
          air.Phase = LSDFAirComboPhase.SlowFalling;
          body.WakeUp();
          SetSlowFall(ref air, ref body, tick, deltaTime, settings ?? Defaults);
        }
        return;
      }
      if (tick >= air.HoldUntilTick) {
        // An untouched launcher drops naturally after its hold. Only a rehit uses the arc/slow fall.
        air.Phase = LSDFAirComboPhase.Falling;
        body.GravityScale = air.SavedGravityScale;
        body.Velocity.Y = FP._0;
        body.WakeUp();
      } else if (tick >= air.RiseUntilTick) {
        air.Phase = LSDFAirComboPhase.Hovering;
        body.GravityScale = FP._0;
        body.Velocity.Y = FP._0;
      } else {
        SetTargetVelocity(ref body, air.TargetY - positionY, air.RiseUntilTick - tick, deltaTime);
      }
    }

    private static FP DragFactor(in PhysicsBody2D body, FP deltaTime) {
      return FPMath.Max(FP._0_01, FP._1 - body.Drag * deltaTime);
    }

    private static void SetTargetVelocity(ref PhysicsBody2D body, FP distance, int ticks, FP deltaTime) {
      body.GravityScale = FP._0;
      body.Velocity.Y = deltaTime > FP._0 && ticks > 0
        ? distance / (ticks * deltaTime * DragFactor(body, deltaTime)) : FP._0;
    }

    private static FP ReturnExitSpeed(in LSDF_AirCombo air, FP deltaTime) {
      FP duration = air.ReturnTicks * deltaTime;
      // Bound the endpoint tangent so even a tiny/zero lift cannot undershoot ComboY.
      return duration > FP._0 ? FPMath.Min(air.SlowFallSpeed,
        3 * FPMath.Max(FP._0, air.TargetY - air.ComboY) / duration) : FP._0;
    }

    private static void SetReturnVelocity(ref LSDF_AirCombo air, ref PhysicsBody2D body,
        FP positionY, int tick, FP deltaTime) {
      // Sample the next physics position on a cubic Hermite curve. The peak has zero
      // slope and the baseline tangent matches the following accelerating fall.
      FP t = FPMath.Clamp((FP)(tick - air.RiseUntilTick + 1) / air.ReturnTicks, FP._0, FP._1);
      FP squared = t * t, cubed = squared * t;
      FP nextY = air.TargetY + (air.ComboY - air.TargetY) * (3 * squared - 2 * cubed)
        - ReturnExitSpeed(air, deltaTime) * air.ReturnTicks * deltaTime * (cubed - squared);
      SetTargetVelocity(ref body, nextY - positionY, 1, deltaTime);
    }

    private static void SetSlowFall(ref LSDF_AirCombo air, ref PhysicsBody2D body, int tick,
        FP deltaTime, LSDFJuggleSettings settings) {
      body.GravityScale = FP._0;
      FP elapsed = System.Math.Max(0, tick - air.HoldUntilTick) * deltaTime;
      FP speed = ReturnExitSpeed(air, deltaTime) + FPMath.Max(FP._0, settings.SlowFallAcceleration) * elapsed;
      body.Velocity.Y = -speed / DragFactor(body, deltaTime);
    }

    public static void Cancel(ref LSDF_AirCombo air, ref PhysicsBody2D body, bool preserveCombo = false) {
      var previous = air;
      if (air.ComboActive || air.Phase != LSDFAirComboPhase.Inactive) {
        body.GravityScale = air.SavedGravityScale;
        body.WakeUp();
      }
      if (air.ControlsHorizontal) body.Velocity.X = FP._0;
      air = default;
      if (preserveCombo && previous.ComboActive) {
        air.ComboActive = true; air.HitCount = previous.HitCount;
        air.BaseY = previous.BaseY; air.SavedGravityScale = previous.SavedGravityScale;
      }
    }
  }
}
