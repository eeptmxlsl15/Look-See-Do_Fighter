using UnityEngine;

/// <summary>One local camera impulse. Always samples a separate, unmodified follow position.</summary>
public sealed class LSDFCounterHitZoomPlayer {
  private Camera _camera;
  private float _fieldOfView, _size, _elapsed, _in, _hold, _out, _magnification, _focus;
  private Vector3 _position, _followPosition;
  public bool IsPlaying => _camera != null;

  public void Begin(Camera camera, LSDFCounterHitZoomSettings.Entry settings, Vector3 hitPosition) {
    Reset();
    if (camera == null || settings == null || !settings.Enabled) return;
    _in = Mathf.Max(0, settings.ZoomInSeconds);
    _hold = Mathf.Max(0, settings.HoldSeconds);
    _out = Mathf.Max(0, settings.ReturnSeconds);
    if (_in + _hold + _out <= 0) return;
    _camera = camera;
    _fieldOfView = camera.fieldOfView;
    _size = camera.orthographicSize;
    _followPosition = camera.transform.position;
    _position = hitPosition + (Vector3)settings.FocusOffset;
    _magnification = Mathf.Clamp(settings.Magnification, 1, 3);
    _focus = Mathf.Clamp01(settings.FocusStrength);
    _elapsed = 0;
  }

  public void Advance(Vector3 followPosition, float deltaTime) {
    if (_camera == null) return;
    _followPosition = followPosition;
    _elapsed += Mathf.Max(0, deltaTime);
    float weight;
    if (_elapsed >= _in + _hold + _out) { Reset(); return; }
    if (_in > 0 && _elapsed < _in) weight = Mathf.SmoothStep(0, 1, _elapsed / _in);
    else if (_elapsed <= _in + _hold) weight = 1;
    else weight = _out > 0 ? 1 - Mathf.SmoothStep(0, 1, (_elapsed - _in - _hold) / _out) : 0;
    float scale = Mathf.Lerp(1, _magnification, weight);
    _camera.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(_fieldOfView * Mathf.Deg2Rad * .5f) / scale) * Mathf.Rad2Deg;
    _camera.orthographicSize = _size / scale;
    var target = new Vector3(_position.x, _position.y, followPosition.z);
    _camera.transform.position = Vector3.Lerp(followPosition, target, _focus * weight);
  }

  public void Reset() {
    if (_camera != null) {
      _camera.fieldOfView = _fieldOfView;
      _camera.orthographicSize = _size;
      _camera.transform.position = _followPosition;
    }
    _camera = null;
  }
}
