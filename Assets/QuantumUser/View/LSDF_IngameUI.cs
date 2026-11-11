using Quantum;
using Quantum.LSDF;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LSDF_IngameUI : QuantumEntityViewComponent {
  public TMP_Text TimerText;
  public EntityRef myPlayerEntity, opponentEntity;
  public Image[] LeftRoundWins = new Image[3], RightRoundWins = new Image[3];
  public GameObject RoundMessageRoot;
  public TMP_Text RoundText, FightText;
  [Min(0), Tooltip("Hit shake duration in seconds; zero disables the effect.")]
  public float HealthShakeDuration = .22f;
  [Min(0), Tooltip("Maximum health bar shake offset in Canvas units.")]
  public float HealthShakeStrength = 6f;
  private LSDFSceneBindings _bindings;
  private LSDFSceneBindings.Hud _hud;
  private readonly LSDFHealthBarShake _leftShake = new LSDFHealthBarShake();
  private readonly LSDFHealthBarShake _rightShake = new LSDFHealthBarShake();
  public override void OnActivate(Frame frame) {
    OnDeactivate();
    _bindings = LSDFSceneBindings.For(EntityView);
  }
  public override void OnUpdateView() {
    if (_bindings == null || !_bindings.Accept(Game) || !_bindings.ClaimHud(this) || !LSDFSceneBindings.TryLocal(Game, out var local)) {
      ResetPresentation(); return;
    }
    var frame = PredictedFrame;
    if (frame == null || !LSDFRoster.TryFind(frame, local, out myPlayerEntity) ||
        !LSDFRoster.TryFind(frame, local == (PlayerRef)0 ? (PlayerRef)1 : (PlayerRef)0, out opponentEntity)) {
      ResetPresentation(); return;
    }
    if (!frame.TryGet<LSDF_Player>(myPlayerEntity, out var mine) || !frame.TryGet<LSDF_Player>(opponentEntity, out var other)) {
      ResetPresentation(); return;
    }
    var nextHud = _bindings.LocalHud(local);
    if (_hud != nextHud) ResetPresentation();
    _hud = nextHud;
    if (_hud == null) { ResetPresentation(); return; }
    LSDFGameplayUIRenderer.ApplyImages(_hud, _bindings.GameplayUIImages);
    TimerText = _hud.Timer; RoundMessageRoot = _hud.RoundRoot; RoundText = _hud.Round; FightText = _hud.Fight;
    LeftRoundWins = _hud.LeftWins; RightRoundWins = _hud.RightWins;
    int maximum = LSDFBalance.MaximumHealth(frame);
    if (_hud.LeftHealth != null) _hud.LeftHealth.fillAmount = Mathf.Clamp01((float)mine.playerHp / maximum);
    if (_hud.RightHealth != null) _hud.RightHealth.fillAmount = Mathf.Clamp01((float)other.playerHp / maximum);
    var confirmed = VerifiedFrame;
    if (confirmed == null || !confirmed.TryGet<LSDF_Player>(myPlayerEntity, out mine) || !confirmed.TryGet<LSDF_Player>(opponentEntity, out other)) return;
    var leftRoot = _hud.LeftHealthShakeRoot != null ? _hud.LeftHealthShakeRoot : _hud.LeftHealth != null ? _hud.LeftHealth.rectTransform : null;
    var rightRoot = _hud.RightHealthShakeRoot != null ? _hud.RightHealthShakeRoot : _hud.RightHealth != null ? _hud.RightHealth.rectTransform : null;
    _leftShake.Observe(leftRoot, myPlayerEntity, mine, confirmed.Number);
    _rightShake.Observe(rightRoot, opponentEntity, other, confirmed.Number);
    _leftShake.Advance(Time.unscaledDeltaTime, HealthShakeDuration, HealthShakeStrength);
    _rightShake.Advance(Time.unscaledDeltaTime, HealthShakeDuration, HealthShakeStrength);
    LSDFGameplayUIRenderer.SetWins(_hud, other.loseRound, mine.loseRound);
    LSDFGameplayUIRenderer.RenderRound(_hud, _bindings.GameplayUIImages, mine, other, confirmed.Number);
  }
  public static string ResultMessage(LSDFRoundOutcome outcome, bool matchOver) {
    if (outcome == LSDFRoundOutcome.Draw) return "DRAW";
    if (outcome == LSDFRoundOutcome.None) return string.Empty;
    return outcome == LSDFRoundOutcome.FirstWins ? (matchOver ? "YOU WIN" : "ROUND WIN") :
      (matchOver ? "YOU LOSE" : "ROUND LOSE");
  }
  public void ShowRoundIntro(int roundNumber) {
    var hud = _hud ?? new LSDFSceneBindings.Hud { RoundRoot = RoundMessageRoot, Round = RoundText, Fight = FightText };
    LSDFGameplayUIRenderer.ShowRound(hud, _bindings != null ? _bindings.GameplayUIImages : null, roundNumber);
  }
  public override void OnDeactivate() {
    ResetPresentation();
    if (_bindings != null) {
      if (_bindings.OwnsHud(this) && RoundMessageRoot != null) RoundMessageRoot.SetActive(false);
      _bindings.Release(this);
    }
    _bindings = null; _hud = null;
    myPlayerEntity = default; opponentEntity = default;
  }
  public override void OnGameChanged() { OnDeactivate(); if (PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnEnable() { if (IsActive && PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnDisable() { OnDeactivate(); }
  private void ResetHealthShake() { _leftShake.Reset(); _rightShake.Reset(); }
  private void ResetPresentation() {
    ResetHealthShake();
    if (_bindings != null && _bindings.OwnsHud(this)) LSDFGameplayUIRenderer.Hide(_hud);
  }
}

/// <summary>Per-bar presentation state, driven only by new verified samples. Never writes to Quantum.</summary>
public sealed class LSDFHealthBarShake {
  private RectTransform _target;
  private EntityRef _entity;
  private Vector2 _origin;
  private int _tick, _health, _hitEnd;
  private bool _hasSample, _wasHit, _shaking;
  private float _elapsed;

  public bool IsShaking => _shaking;

  public void Observe(RectTransform target, EntityRef entity, in LSDF_Player player, int verifiedTick) {
    if (_target != target || _entity != entity || (_hasSample && verifiedTick < _tick)) {
      Reset(); _target = target; _entity = entity;
    }
    if (_target == null) return;
    bool hit = player.isHit && !player.isGuard;
    if (_hasSample && verifiedTick > _tick) {
      if (player.playerHp > _health) Stop(); // Recovery and the next round are not hits.
      else if (player.playerHp < _health || (hit && (!_wasHit || player.DelayFrame != _hitEnd))) {
        if (!_shaking) _origin = _target.anchoredPosition;
        _elapsed = 0; _shaking = true;
      }
    }
    if (!_hasSample || verifiedTick > _tick) {
      _hasSample = true; _tick = verifiedTick; _health = player.playerHp;
      _wasHit = hit; _hitEnd = player.DelayFrame;
    }
  }

  public void Advance(float deltaTime, float duration, float strength) {
    if (!_shaking) return;
    if (_target == null || duration <= 0 || strength <= 0) { Stop(); return; }
    _elapsed += Mathf.Max(0, deltaTime);
    if (_elapsed >= duration) { Stop(); return; }
    float fade = 1 - _elapsed / duration;
    float phase = _elapsed * 28 * Mathf.PI * 2;
    var offset = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * .8f) * .4f);
    _target.anchoredPosition = _origin + offset * (strength * fade * fade);
  }

  private void Stop() {
    if (_shaking && _target != null) _target.anchoredPosition = _origin;
    _shaking = false; _elapsed = 0;
  }

  public void Reset() {
    Stop(); _target = null; _entity = default; _hasSample = false; _wasHit = false;
  }
}
