using Quantum.LSDF;
using UnityEngine;

[CreateAssetMenu(menuName = "LSDF/Player 2 Test Input")]
public sealed class LSDFPlayer2TestInputSettings : ScriptableObject {
  public bool Enabled;
  public CommandDirection Direction = CommandDirection.Neutral;
  public CommandButton Button = CommandButton.LeftPunch;
  public bool Repeat = true;
  [Min(0)] public int InitialDelayTicks = 30;
  [Min(1)] public int IntervalTicks = 120;
  [Min(1)] public int PressTicks = 1;
}
