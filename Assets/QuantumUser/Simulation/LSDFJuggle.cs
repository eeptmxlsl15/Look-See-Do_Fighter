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
      ApplyHeight(frame, transform, y);
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
      Step(ref *air, ref *body, transform->Position.Y, frame.Number, frame.DeltaTime);
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
      if (!wall || ended) {
        bool liftedFromContact = y != transform->Position.Y && y > air->BaseY;
        ApplyHeight(frame, transform, y);
        if (liftedFromContact) {
          player->isGround = false;
          AnimatorComponent.SetBoolean(frame, animator, "Ground", false);
        }
      }
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

    private static void ApplyHeight(Frame frame, Transform2D* transform, FP y) {
      if (transform->Position.Y != y) transform->Teleport(frame, new FPVector2(transform->Position.X, y));
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
        if (grounded && body.Velocity.Y <= FP._0) {
          air.Phase = LSDFAirComboPhase.Recovering;
          air.RecoveryUntilTick = tick + System.Math.Max(1, System.Math.Min(600, config.EnderRecoveryTicks));
          body.Velocity.X = FP._0;
        }
      } else if (air.Phase == LSDFAirComboPhase.Recovering) {
        body.Velocity.X = FP._0;
        if (!grounded) { air.Phase = LSDFAirComboPhase.BlownAway; body.WakeUp(); }
        else if (tick >= air.RecoveryUntilTick) Cancel(ref air, ref body);
      }
    }

    public static bool ShouldLand(Frame frame, EntityRef entity, in LSDF_Player player, in PhysicsBody2D body) {
      frame.TryGet<LSDF_AirCombo>(entity, out var air);
      return ShouldLand(player, body, air);
    }

    public static bool ShouldLand(in LSDF_Player player, in PhysicsBody2D body, in LSDF_AirCombo air) {
      return player.isAir && player.isGround && body.Velocity.Y <= FP._0 &&
        (air.Phase == LSDFAirComboPhase.Inactive || air.Phase == LSDFAirComboPhase.Falling);
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
      air.TargetY = air.BaseY + FPMath.Clamp(rehit ? config.ComboHeight : config.LaunchHeight, FP._0, maximum);
      int rise = rehit ? 0 : System.Math.Max(1, System.Math.Min(600, config.LaunchRiseTicks));
      int hold = System.Math.Max(0, System.Math.Min(600, rehit ? config.RehitHoldTicks : config.LaunchHoldTicks));
      air.RiseUntilTick = tick + rise;
      air.HoldUntilTick = air.RiseUntilTick + hold;
      air.Phase = rehit ? LSDFAirComboPhase.Hovering : LSDFAirComboPhase.Rising;
      if (rehit) {
        positionY = air.TargetY;
        body.Velocity.Y = FP._0;
      }
      body.WakeUp();
      Step(ref air, ref body, positionY, tick, deltaTime);
    }

    public static void Step(ref LSDF_AirCombo air, ref PhysicsBody2D body, FP positionY, int tick, FP deltaTime) {
      if (BlocksHit(air)) return;
      ApplyHorizontal(ref air, ref body, tick);
      if (air.Phase == LSDFAirComboPhase.Inactive || air.Phase == LSDFAirComboPhase.Falling) return;
      if (tick >= air.HoldUntilTick) {
        air.Phase = LSDFAirComboPhase.Falling;
        body.GravityScale = air.SavedGravityScale;
        body.Velocity.Y = FP._0;
        body.WakeUp();
      } else if (tick >= air.RiseUntilTick) {
        air.Phase = LSDFAirComboPhase.Hovering;
        body.GravityScale = FP._0;
        body.Velocity.Y = FP._0;
      } else {
        body.GravityScale = FP._0;
        // Compensate the installed physics integrator's linear drag without changing X or Drag.
        FP dragFactor = FPMath.Max(FP._0_01, FP._1 - body.Drag * deltaTime);
        body.Velocity.Y = deltaTime > FP._0
          ? FPMath.Max(FP._0, air.TargetY - positionY) / ((air.RiseUntilTick - tick) * deltaTime * dragFactor)
          : FP._0;
      }
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
