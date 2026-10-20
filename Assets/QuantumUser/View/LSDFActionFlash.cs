using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Deterministic;
using Quantum;
using Quantum.Addons.Animator;
using Quantum.LSDF;
using UnityEngine;

public enum LSDFActionFlashKind { None, Homing, Parry }

/// <summary>Presentation-only pulse and sprite overlay. Never writes to Frame or source materials.</summary>
public sealed class LSDFActionFlash : IDisposable {
  private readonly Dictionary<int, LSDFActionFlashKind> _states = new Dictionary<int, LSDFActionFlashKind>();
  private AssetGuid _graph;
  private int _lastTick = -1, _state;
  private FP _lastTime;
  private LSDFActionFlashKind _kind;
  private float _elapsed = float.PositiveInfinity;
  private SpriteRenderer _source, _overlay;
  private MaterialPropertyBlock _properties;
  private static readonly int OpacityId = Shader.PropertyToID("_FlashOpacity");

  public unsafe void Observe(Frame frame, EntityRef entity) {
    if (frame == null || !frame.TryGet<LSDF_Player>(entity, out var player) || player.isRoundEnd || player.isHealing ||
        !frame.TryGet<AnimatorComponent>(entity, out var animator) || !animator.AnimatorGraph.Id.IsValid ||
        animator.Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) { Reset(); return; }
    if (_graph != animator.AnimatorGraph.Id) { Reset(); _states.Clear(); _graph = animator.AnimatorGraph.Id; }
    var graph = frame.FindAsset<AnimatorGraph>(animator.AnimatorGraph);
    var layers = frame.ResolveList(animator.Layers);
    if (graph == null || layers.Count == 0) { Reset(); return; }
    var layer = layers.GetPointer(0);
    int id = layer->ToStateId != 0 ? layer->ToStateId : layer->CurrentStateId;
    var state = graph.GetState(id);
    if (state == null) { Reset(); return; }
    if (!_states.TryGetValue(id, out var kind)) { kind = Classify(frame, state); _states[id] = kind; }
    Observe(frame.Number, id, kind, layer->ToStateId != 0 ? layer->ToStateTime : layer->Time, state.Name == "Parring");
  }

  public static unsafe LSDFActionFlashKind Classify(Frame frame, AnimatorState state) {
    if (state == null) return LSDFActionFlashKind.None;
    if (state.Name == "ParringStart" || state.Name == "Parring") return LSDFActionFlashKind.Parry;
    if (state.SerialisedMotions == null) return LSDFActionFlashKind.None;
    foreach (var motion in state.SerialisedMotions) {
      var events = motion.AnimatorData?.Events;
      if (events == null) continue;
      foreach (var window in events) {
        if (window == null || !window.AssetRef.Id.IsValid) continue;
        var asset = frame.FindAsset(window.AssetRef);
        if (asset == null || asset.IsMuted) continue;
        // Inspect immutable authored hit definitions once per graph/state, including multi-hit schedules.
        foreach (var field in asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)) {
          LSDFSingleHitDefinition hit = null;
          if (field.FieldType == typeof(LSDFSingleHitDefinition)) hit = field.GetValue(asset) as LSDFSingleHitDefinition;
          else if (field.FieldType == typeof(LSDFTimedHitDefinition)) hit = (field.GetValue(asset) as LSDFTimedHitDefinition)?.Hit;
          if (hit != null && hit.ParryReaction == HomingType.Homing) return LSDFActionFlashKind.Homing;
        }
      }
    }
    return LSDFActionFlashKind.None;
  }

  public void Observe(int tick, int state, LSDFActionFlashKind kind, FP time, bool parryContinuation = false) {
    if (_lastTick >= 0 && tick < _lastTick) {
      // Resample the returned state rather than restarting a full-strength pulse after prediction rewind.
      _elapsed = kind == LSDFActionFlashKind.None ? float.PositiveInfinity : Mathf.Max(0, time.AsFloat);
    } else if (kind == LSDFActionFlashKind.None) {
      _elapsed = float.PositiveInfinity;
    } else if ((_state != state || _kind != kind || (tick > _lastTick && time < _lastTime && !parryContinuation)) &&
               !(parryContinuation && _kind == LSDFActionFlashKind.Parry)) {
      _elapsed = 0;
    }
    _lastTick = tick; _state = state; _kind = kind; _lastTime = time;
  }

  public float Advance(float delta, float strength, float fadeIn, float fadeOut) {
    float amount = Envelope(_elapsed, strength, fadeIn, fadeOut);
    _elapsed += Mathf.Max(0, delta);
    return amount;
  }

  public static float Envelope(float elapsed, float strength, float fadeIn, float fadeOut) {
    fadeIn = Mathf.Max(0, fadeIn); fadeOut = Mathf.Max(0, fadeOut);
    if (elapsed < 0 || elapsed >= fadeIn + fadeOut) return 0;
    float weight = fadeIn > 0 && elapsed < fadeIn ? Mathf.SmoothStep(0, 1, elapsed / fadeIn)
      : fadeOut > 0 ? 1 - Mathf.SmoothStep(0, 1, (elapsed - fadeIn) / fadeOut) : 0;
    return Mathf.Clamp01(strength) * weight;
  }

  public void Apply(SpriteRenderer source, Material material, float amount) {
    if (source == null || material == null || amount <= 0 || !source.enabled || source.sprite == null) {
      if (_overlay != null) _overlay.enabled = false;
      return;
    }
    if (_source != source) { ReleaseOverlay(); _source = source; }
    if (_overlay == null) {
      var child = new GameObject("Action White Flash");
      child.hideFlags = HideFlags.DontSave;
      child.transform.SetParent(source.transform, false);
      _overlay = child.AddComponent<SpriteRenderer>();
    }
    _overlay.gameObject.layer = source.gameObject.layer;
    _overlay.sharedMaterial = material;
    _overlay.sprite = source.sprite;
    _overlay.drawMode = source.drawMode; _overlay.size = source.size;
    _overlay.sortingLayerID = source.sortingLayerID; _overlay.sortingOrder = source.sortingOrder + 1;
    _overlay.transform.localScale = new Vector3(source.flipX ? -1 : 1, source.flipY ? -1 : 1, 1);
    if (_properties == null) _properties = new MaterialPropertyBlock();
    _properties.SetTexture("_MainTex", source.sprite.texture);
    _properties.SetFloat(OpacityId, Mathf.Clamp01(amount) * source.color.a);
    _overlay.SetPropertyBlock(_properties);
    _overlay.enabled = true;
  }

  public void Reset() {
    _lastTick = -1; _state = 0; _kind = LSDFActionFlashKind.None; _lastTime = FP._0;
    _elapsed = float.PositiveInfinity;
    if (_overlay != null) _overlay.enabled = false;
  }

  private void ReleaseOverlay() {
    if (_overlay != null) {
      _overlay.enabled = false;
      if (Application.isPlaying) UnityEngine.Object.Destroy(_overlay.gameObject);
      else UnityEngine.Object.DestroyImmediate(_overlay.gameObject);
    }
    _overlay = null; _source = null;
  }

  public void Dispose() { Reset(); ReleaseOverlay(); _states.Clear(); _graph = default; }
}
