using System;
using Quantum;
using UnityEngine;

public partial class LSDFSceneBindings {
  [Tooltip("Select counter-hit zoom moves and tune their camera effect here.")]
  public LSDFCounterHitZoomSettings CounterHitZoom;
  private readonly LSDFCounterHitZoomPlayer _counterZoom = new LSDFCounterHitZoomPlayer();
  private IDisposable _counterZoomSubscription;
  private GameObject _followCamera;
  private Vector3 _followPosition;
  private int _lastZoomTick = -1, _lastVerifiedTick = -1;

  private void OnEnable() {
    StopCounterHitZoom();
    _counterZoomSubscription = QuantumEvent.SubscribeManual<EventOnCounterHitZoom>(OnCounterHitZoom);
  }

  private void OnCounterHitZoom(EventOnCounterHitZoom hit) {
    if (!isActiveAndEnabled || hit.Game != Game || !Accept(hit.Game) ||
        !TryLocal(hit.Game, out var local) || hit.Tick <= _lastZoomTick || CounterHitZoom == null ||
        !CounterHitZoom.TryGet(hit.Attack.Id, out var settings)) return;
    var cameraObject = local == (PlayerRef)0 ? Player1Camera : Player2Camera;
    if (cameraObject == null || !cameraObject.TryGetComponent<Camera>(out var camera)) return;
    _cameraHitShake.RemoveOffset();
    if (_followCamera != cameraObject) {
      _cameraHitShake.Reset();
      _counterZoom.Reset();
      _followCamera = cameraObject;
      _followPosition = cameraObject.transform.position;
    }
    _lastZoomTick = hit.Tick;
    _counterZoom.Begin(camera, settings, new Vector3(hit.Position.X.AsFloat, hit.Position.Y.AsFloat, 0));
  }

  private void UpdateCameraFollow(GameObject cameraObject, Vector3 target, EntityRef first, EntityRef second) {
    _cameraHitShake.RemoveOffset();
    var verified = Game.Frames.Verified;
    if (verified != null && verified.Number < _lastVerifiedTick) ResetCounterHitZoom();
    if (_followCamera != cameraObject) {
      _cameraHitShake.Reset();
      _counterZoom.Reset();
      _followCamera = cameraObject;
      _followPosition = cameraObject.transform.position;
    }
    if (verified != null) {
      _lastVerifiedTick = verified.Number;
      if (verified.TryGet<LSDF_Player>(first, out var a) && verified.TryGet<LSDF_Player>(second, out var b)) {
        if (a.isHealing || b.isHealing || verified.Number < Math.Max(a.tickToEnableMove, b.tickToEnableMove)) {
          _counterZoom.Reset();
          _cameraHitShake.Reset();
        } else _cameraHitShake.Observe(first, a, second, b, verified.Number);
      } else _cameraHitShake.Reset();
    } else _cameraHitShake.Reset();
    if (CounterHitZoom == null || !CounterHitZoom.Enabled) _counterZoom.Reset();
    _followPosition = Vector3.Lerp(_followPosition, target, Time.deltaTime * 5);
    cameraObject.transform.position = _followPosition;
    _counterZoom.Advance(_followPosition, Time.unscaledDeltaTime);
    _cameraHitShake.Advance(cameraObject.transform, Time.unscaledDeltaTime, HitShakeDuration, HitShakeStrength);
  }

  private void ResetCounterHitZoom() {
    _cameraHitShake.Reset();
    _counterZoom.Reset();
    _followCamera = null;
    _lastZoomTick = -1;
    _lastVerifiedTick = -1;
  }

  private void StopCounterHitZoom() {
    _counterZoomSubscription?.Dispose();
    _counterZoomSubscription = null;
    ResetCounterHitZoom();
  }
}
