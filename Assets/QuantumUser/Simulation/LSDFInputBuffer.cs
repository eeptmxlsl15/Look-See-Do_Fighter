namespace Quantum.LSDF {
  /// <summary>One latest command, timestamped on the press edge, not on every held tick.</summary>
  public static class LSDFInputBuffer {
    public const int DefaultWindowFrames = 10;
    // A safety bound for the existing four-stage Animator command routing, not the input lifetime.
    public const int MaximumRoutingFrames = 16;

    public static CommandDirection ReadDirection(in Input input) {
      if (input.Down) {
        if (input.Left) return CommandDirection.DownLeft;
        if (input.Right) return CommandDirection.DownRight;
        return CommandDirection.Down;
      }
      if (input.Up) return CommandDirection.Up;
      if (input.Left) return CommandDirection.Left;
      if (input.Right) return CommandDirection.Right;
      return CommandDirection.Neutral;
    }

    public static int AttackButtons(in Input input) {
      return (input.LeftPunch ? 1 : 0) | (input.RightPunch ? 2 : 0) |
        (input.LeftKick ? 4 : 0) | (input.RightKick ? 8 : 0);
    }

    public static bool Suppressed(in LSDF_Player player, int tick) {
      return player.playerHp <= 0 || player.isRoundEnd;
    }

    public static bool CanExecute(in LSDF_Player player, int tick) {
      return !Suppressed(player, tick) && tick >= player.tickToEnableMove &&
        !player.isAttack && !player.isCantMove && !player.isHit && !player.isGuard &&
        !player.isStun && !player.isCombo && !player.isAir && !player.isWallHit;
    }

    public static void Clear(ref LSDF_CommandBuffer buffer) {
      buffer.HasPending = false;
      buffer.Dispatching = false;
    }

    public static void Capture(ref LSDF_CommandBuffer buffer, in Input input, in LSDF_Player player,
        int tick, int windowFrames) {
      int held = AttackButtons(input);
      int pressed = held & ~buffer.PreviousAttackButtons;
      buffer.PreviousAttackButtons = held;
      if (Suppressed(player, tick)) { Clear(ref buffer); return; }
      if (pressed != 0) {
        buffer.HasPending = true;
        buffer.InputTick = tick;
        buffer.PendingDirection = (int)ReadDirection(input);
        // Keep the existing LP > RP > LK > RK priority for simultaneous new presses.
        buffer.PendingButton = (pressed & 1) != 0 ? 0 : (pressed & 2) != 0 ? 1 : (pressed & 4) != 0 ? 2 : 3;
      }
      Expire(ref buffer, tick, windowFrames);
    }

    private static void Expire(ref LSDF_CommandBuffer buffer, int tick, int windowFrames) {
      if (windowFrames < 0) windowFrames = 0;
      if (buffer.HasPending && (buffer.InputTick > tick ||
          (long)tick - buffer.InputTick > windowFrames ||
          LSDFLoadout.Slot((CommandDirection)buffer.PendingDirection, (CommandButton)buffer.PendingButton) < 0))
        buffer.HasPending = false;
    }

    /// <summary>Admit a command once, then latch it only until the Animator enters its attack.</summary>
    public static bool TryRoute(ref LSDF_CommandBuffer buffer, in LSDF_Player player, int tick,
        int windowFrames, bool animatorAcceptsCommand, out CommandDirection direction, out CommandButton button) {
      direction = default;
      button = default;
      Expire(ref buffer, tick, windowFrames);
      if (Suppressed(player, tick)) { Clear(ref buffer); return false; }
      if (buffer.Dispatching) {
        // Do not repeat a completed route or resume a route interrupted by damage/round locks.
        if (!CanExecute(player, tick) || buffer.DispatchTick > tick ||
            (long)tick - buffer.DispatchTick > MaximumRoutingFrames) {
          buffer.Dispatching = false;
          return false;
        }
      } else {
        if (!buffer.HasPending || !CanExecute(player, tick) || !animatorAcceptsCommand) return false;
        buffer.Dispatching = true;
        buffer.DispatchTick = tick;
        buffer.ActiveInputTick = buffer.InputTick;
        buffer.ActiveDirection = buffer.PendingDirection;
        buffer.ActiveButton = buffer.PendingButton;
        buffer.HasPending = false;
      }
      direction = (CommandDirection)buffer.ActiveDirection;
      button = (CommandButton)buffer.ActiveButton;
      return true;
    }
  }
}
