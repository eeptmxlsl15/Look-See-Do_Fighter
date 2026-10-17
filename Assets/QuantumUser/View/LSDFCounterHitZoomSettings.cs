using System;
using Quantum;
using Quantum.Addons.Animator;
using UnityEngine;

[CreateAssetMenu(menuName = "LSDF/Counter Hit Zoom Settings")]
public sealed class LSDFCounterHitZoomSettings : ScriptableObject {
  [Serializable]
  public sealed class Entry {
    public AnimatorTimeWindowEventAsset Attack;
    public bool Enabled;
    [Range(1, 3), Tooltip("1 = no magnification, 2 = twice as large.")]
    public float Magnification = 1.7f;
    [Min(0)] public float ZoomInSeconds = .035f;
    [Min(0)] public float HoldSeconds = .08f;
    [Min(0)] public float ReturnSeconds = .18f;
    [Range(0, 1), Tooltip("How far the camera centers on the hitbox; 1 centers it fully.")]
    public float FocusStrength = 1;
    [Tooltip("World-space offset from the hitbox center, independent of player side.")]
    public Vector2 FocusOffset;
  }

  public bool Enabled = true;
  public Entry[] Attacks = Array.Empty<Entry>();

  public bool TryGet(AssetGuid attack, out Entry entry) {
    if (Enabled && attack.IsValid && Attacks != null) foreach (var candidate in Attacks) {
      if (candidate != null && candidate.Attack != null && candidate.Attack.Guid == attack) {
        entry = candidate;
        return candidate.Enabled;
      }
    }
    entry = null;
    return false;
  }
}
