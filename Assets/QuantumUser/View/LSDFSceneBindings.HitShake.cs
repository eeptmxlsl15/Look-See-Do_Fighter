using System;
using Quantum;
using UnityEngine;

public partial class LSDFSceneBindings {
  [Min(0), Tooltip("Whole-camera hit shake duration in seconds. Zero disables it.")]
  public float HitShakeDuration = .18f;
  [Min(0), Tooltip("Maximum hit shake offset in world units, along the camera's screen axes.")]
  public float HitShakeStrength = .015f;
  private readonly LSDFCameraHitShake _cameraHitShake = new LSDFCameraHitShake();
}

/// <summary>Local presentation only: verified hits from either fighter, composed after camera follow and zoom.</summary>
public sealed class LSDFCameraHitShake {
  private struct Sample {
    public EntityRef Entity;
    public int Health, HitEnd;
    public bool Hit;

    public Sample(EntityRef entity, in LSDF_Player player) {
      Entity = entity; Health = player.playerHp; HitEnd = player.DelayFrame;
      Hit = player.isHit && !player.isGuard;
    }

    public bool NewHit(in LSDF_Player player) => player.playerHp < Health ||
      (player.isHit && !player.isGuard && (!Hit || player.DelayFrame != HitEnd));
  }

  private Sample _first, _second;
  private bool _hasSample, _shaking, _hasOffset;
  private int _tick;
  private float _elapsed;
  private Transform _camera;
  private Vector3 _basePosition, _appliedPosition;
  public bool IsShaking => _shaking;

  public void Observe(EntityRef firstEntity, in LSDF_Player first, EntityRef secondEntity,
      in LSDF_Player second, int verifiedTick) {
    if (_hasSample && (firstEntity != _first.Entity || secondEntity != _second.Entity || verifiedTick < _tick)) Reset();
    if (_hasSample && verifiedTick <= _tick) return;
    if (first.isHealing || second.isHealing || verifiedTick < Math.Max(first.tickToEnableMove, second.tickToEnableMove)) {
      Reset(); return;
    }
    if (_hasSample) {
      if (first.playerHp > _first.Health || second.playerHp > _second.Health) Stop();
      else if (_first.NewHit(first) || _second.NewHit(second)) {
        _elapsed = 0;
        _shaking = true;
      }
    }
    _first = new Sample(firstEntity, first); _second = new Sample(secondEntity, second);
    _tick = verifiedTick; _hasSample = true;
  }

  // Remove only our last offset before another camera effect reads or writes the position.
  public void RemoveOffset() {
    if (_hasOffset && _camera != null && _camera.position == _appliedPosition)
      _camera.position = _basePosition;
    _hasOffset = false;
  }

  public void Advance(Transform camera, float deltaTime, float duration, float strength) {
    if (_camera != camera) {
      if (_camera != null) Reset();
      _camera = camera;
    }
    RemoveOffset();
    if (!_shaking) return;
    if (_camera == null || duration <= 0 || strength <= 0) { Stop(); return; }
    _elapsed += Mathf.Max(0, deltaTime);
    if (_elapsed >= duration) { Stop(); return; }
    float fade = 1 - _elapsed / duration;
    float phase = _elapsed * 28 * Mathf.PI * 2;
    var offset = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * .8f) * .55f) * (strength * fade * fade);
    _basePosition = _camera.position;
    _appliedPosition = _basePosition + _camera.right * offset.x + _camera.up * offset.y;
    _camera.position = _appliedPosition;
    _hasOffset = true;
  }

  private void Stop() {
    RemoveOffset();
    _shaking = false; _elapsed = 0;
  }

  public void Reset() {
    Stop(); _camera = null; _hasSample = false;
  }
}
