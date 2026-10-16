using System.Collections;
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
  private LSDFSceneBindings _bindings;
  private LSDFSceneBindings.Hud _hud;
  private int _lastPresentedRound;
  private bool _introPlaying;
  public override void OnActivate(Frame frame) {
    OnDeactivate();
    _bindings = LSDFSceneBindings.For(EntityView);
  }
  public override void OnUpdateView() {
    if (_bindings == null || !_bindings.Accept(Game) || !_bindings.ClaimHud(this) || !LSDFSceneBindings.TryLocal(Game, out var local)) return;
    var frame = PredictedFrame;
    if (frame == null || !LSDFRoster.TryFind(frame, local, out myPlayerEntity) ||
        !LSDFRoster.TryFind(frame, local == (PlayerRef)0 ? (PlayerRef)1 : (PlayerRef)0, out opponentEntity)) return;
    if (!frame.TryGet<LSDF_Player>(myPlayerEntity, out var mine) || !frame.TryGet<LSDF_Player>(opponentEntity, out var other)) return;
    _hud = _bindings.LocalHud(local);
    if (_hud == null) return;
    TimerText = _hud.Timer; RoundMessageRoot = _hud.RoundRoot; RoundText = _hud.Round; FightText = _hud.Fight;
    LeftRoundWins = _hud.LeftWins; RightRoundWins = _hud.RightWins;
    int maximum = LSDFBalance.MaximumHealth(frame);
    if (_hud.LeftHealth != null) _hud.LeftHealth.fillAmount = Mathf.Clamp01((float)mine.playerHp / maximum);
    if (_hud.RightHealth != null) _hud.RightHealth.fillAmount = Mathf.Clamp01((float)other.playerHp / maximum);
    var confirmed = VerifiedFrame;
    if (confirmed == null || !confirmed.TryGet<LSDF_Player>(myPlayerEntity, out mine) || !confirmed.TryGet<LSDF_Player>(opponentEntity, out other)) return;
    SetWins(LeftRoundWins, other.loseRound); SetWins(RightRoundWins, mine.loseRound);
    int round = mine.loseRound + other.loseRound + 1;
    if ((mine.isHealing || other.isHealing) && round != _lastPresentedRound) { _lastPresentedRound = round; ShowRoundIntro(round); }
  }
  private static void SetWins(Image[] images, int wins) {
    if (images == null) return;
    for (int i = 0; i < images.Length; i++) if (images[i] != null) images[i].gameObject.SetActive(i < wins);
  }
  public void ShowRoundIntro(int roundNumber) {
    if (_introPlaying || RoundMessageRoot == null || RoundText == null || FightText == null) return;
    StartCoroutine(PlayIntro(roundNumber));
  }
  private IEnumerator PlayIntro(int round) {
    _introPlaying = true; RoundMessageRoot.SetActive(true);
    RoundText.text = $"Round {round}"; RoundText.gameObject.SetActive(true); FightText.gameObject.SetActive(false);
    yield return new WaitForSeconds(1.5f);
    RoundText.gameObject.SetActive(false); FightText.gameObject.SetActive(true);
    yield return new WaitForSeconds(1);
    RoundMessageRoot.SetActive(false); _introPlaying = false;
  }
  public override void OnDeactivate() {
    StopAllCoroutines();
    if (_bindings != null) {
      if (_bindings.OwnsHud(this) && RoundMessageRoot != null) RoundMessageRoot.SetActive(false);
      _bindings.Release(this);
    }
    _bindings = null; _hud = null; _lastPresentedRound = 0; _introPlaying = false;
    myPlayerEntity = default; opponentEntity = default;
  }
  public override void OnGameChanged() { OnDeactivate(); if (PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnEnable() { if (IsActive && PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnDisable() { OnDeactivate(); }
}
