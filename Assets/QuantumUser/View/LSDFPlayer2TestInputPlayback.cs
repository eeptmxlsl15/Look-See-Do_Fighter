using System;
using Quantum;
using Quantum.LSDF;

/// <summary>Local input producer only. Its results enter the normal Quantum input stream.</summary>
public sealed class LSDFPlayer2TestInputPlayback {
  private object _game;
  private LSDFPlayer2TestInputSettings _settings;
  private CommandDirection _direction;
  private CommandButton _button;
  private bool _repeat, _started;
  private int _delay, _interval, _press, _startTick;

  public void Reset() {
    _game = null; _settings = null; _started = false;
  }

  /// <returns>True when the local 2P input is overridden, including neutral ticks.</returns>
  public bool TrySample(object game, PlayerRef player, LSDFPlayer2TestInputSettings settings,
      bool ready, int tick, out Quantum.Input input) {
    input = default;
    // A callback for another local player must never reset 2P's schedule.
    if (player != (PlayerRef)1) return false;
    if (game == null || settings == null || !settings.Enabled) { Reset(); return false; }
    if (!ReferenceEquals(_game, game) || _settings != settings || _direction != settings.Direction ||
        _button != settings.Button || _repeat != settings.Repeat || _delay != settings.InitialDelayTicks ||
        _interval != settings.IntervalTicks || _press != settings.PressTicks) {
      _game = game; _settings = settings; _direction = settings.Direction; _button = settings.Button;
      _repeat = settings.Repeat; _delay = settings.InitialDelayTicks;
      _interval = settings.IntervalTicks; _press = settings.PressTicks; _started = false;
    }
    if (!ready || LSDFLoadout.Slot(_direction, _button) < 0) { _started = false; return true; }
    if (!_started) { _startTick = tick; _started = true; }
    long elapsed = (long)tick - _startTick - Math.Max(0, _delay);
    if (elapsed < 0) return true;
    long press = Math.Max(1, _press);
    // A release tick is required for the existing press-edge command buffer.
    long period = Math.Max((long)_interval, press + 1);
    if ((!_repeat && elapsed >= press) || (_repeat && elapsed % period >= press)) return true;
    input = CommandInput(_direction, _button);
    return true;
  }

  public static Quantum.Input CommandInput(CommandDirection direction, CommandButton button) {
    if (LSDFLoadout.Slot(direction, button) < 0) return default;
    return new Quantum.Input {
      Left = direction == CommandDirection.Left || direction == CommandDirection.DownLeft,
      Right = direction == CommandDirection.Right || direction == CommandDirection.DownRight,
      Down = direction == CommandDirection.Down || direction == CommandDirection.DownLeft || direction == CommandDirection.DownRight,
      Up = direction == CommandDirection.Up,
      LeftPunch = button == CommandButton.LeftPunch,
      RightPunch = button == CommandButton.RightPunch,
      LeftKick = button == CommandButton.LeftKick,
      RightKick = button == CommandButton.RightKick
    };
  }
}
