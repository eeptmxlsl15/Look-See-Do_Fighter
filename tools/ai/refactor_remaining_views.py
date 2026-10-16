"""Project View adapters with explicit per-updater ownership. Existing GUIDs are retained."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
VIEW = ROOT/'Assets/QuantumUser/View'

(VIEW/'LSDF_ViewHandler.cs').write_text('''using Quantum;
using UnityEngine;
public class LSDF_ViewHandler : QuantumEntityViewComponent {
  public Animator animator;
  public SpriteRenderer spriteRenderer;
  public RuntimeAnimatorController playerController, enemyController;
  private LSDFSceneBindings _bindings;
  public override void OnActivate(Frame frame) {
    OnDeactivate();
    _bindings = LSDFSceneBindings.For(EntityView);
    if (_bindings == null || !_bindings.Accept(Game) || !frame.TryGet<PlayerLink>(EntityRef, out var link)) return;
    _bindings.Register(link.PlayerRef, this);
    ApplyPerspective(frame);
  }
  private void ApplyPerspective(Frame frame) {
    if (!LSDFSceneBindings.TryLocal(Game, out var local) || !frame.TryGet<PlayerLink>(EntityRef, out var link)) return;
    if (animator != null) animator.runtimeAnimatorController = local == link.PlayerRef ? playerController : enemyController;
    if (spriteRenderer != null) spriteRenderer.flipX = local == (PlayerRef)1;
  }
  public override void OnUpdateView() { if (PredictedFrame != null) ApplyPerspective(PredictedFrame); }
  public override void OnDeactivate() { if (_bindings != null) _bindings.Unregister(this); _bindings = null; }
  public override void OnGameChanged() { OnDeactivate(); if (PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnDisable() { OnDeactivate(); }
}
''',encoding='utf-8')

(VIEW/'LSDF_IngameUI.cs').write_text('''using System.Collections;
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
  private void OnDisable() { OnDeactivate(); }
}
''',encoding='utf-8')

(VIEW/'LSDF_HitEffectView.cs').write_text('''using System;
using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
  public class LSDF_HitEffectView : QuantumEntityViewComponent {
    public GameObject HitParticle, GuardParticle, ParringParticle, CounterParticle;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
    private readonly LSDFPresentationLedger<GameObject> _effects = new LSDFPresentationLedger<GameObject>();
    private LSDFSceneBindings _bindings;
    public override void OnActivate(Frame frame) {
      OnDeactivate(); _bindings = LSDFSceneBindings.For(EntityView);
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnHitEffect>(e => Spawn(e, HitParticle, e.position, .2f)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnGuardEffect>(e => Spawn(e, GuardParticle, e.position, .3f)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnCounterEffect>(e => Spawn(e, CounterParticle, e.position, .3f)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnParringEffect>(e => Spawn(e, ParringParticle, e.position, .5f)));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventCanceled>(e => {
        if (e.Game == Game && _effects.Cancel(e.EventKey, out var effect) && effect != null) Destroy(effect);
      }));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventConfirmed>(e => { if (e.Game == Game) _effects.Confirm(e.EventKey); }));
    }
    private void Spawn(EventBase e, GameObject prefab, FPVector2 position, float scale) {
      if (e.Game != Game || _bindings == null || !_bindings.Accept(Game) || !_bindings.ClaimEffects(this) || prefab == null) return;
      var key = LSDFPresentationLedger<GameObject>.Key(e);
      if (_effects.Contains(key)) return;
      var effect = Instantiate(prefab, new Vector3(position.X.AsFloat, position.Y.AsFloat, 0), Quaternion.Euler(90, 0, 0));
      effect.transform.localScale = Vector3.one * scale;
      if (effect.TryGetComponent<ParticleSystem>(out var particle)) particle.Play();
      _effects.Track(key, effect); Destroy(effect, .1f);
    }
    public override void OnDeactivate() {
      foreach (var subscription in _subscriptions) subscription.Dispose(); _subscriptions.Clear();
      _effects.Clear(effect => { if (effect != null) Destroy(effect); });
      if (_bindings != null) _bindings.Release(this); _bindings = null;
    }
    public override void OnGameChanged() { OnDeactivate(); if (PredictedFrame != null) OnActivate(PredictedFrame); }
    private void OnDisable() { OnDeactivate(); }
  }
}
''',encoding='utf-8')

(VIEW/'LSDF_PlayerInput.cs').write_text('''using System;
using Photon.Deterministic;
using UnityEngine;
namespace Quantum.Asteroids {
  public class LSDF_PlayerInput : MonoBehaviour {
    public QuantumEntityViewUpdater ViewUpdater;
    private IDisposable _subscription;
    private void OnEnable() {
      _subscription = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
        if (ViewUpdater != null && callback.Game == ViewUpdater.ObservedGame) PollInput(callback);
      });
    }
    private void OnDisable() { _subscription?.Dispose(); _subscription = null; }
    public static Quantum.Input ReadInput(Func<KeyCode, bool> pressed) {
      return new Quantum.Input {
        Left = pressed(KeyCode.A) || pressed(KeyCode.LeftArrow),
        Right = pressed(KeyCode.D) || pressed(KeyCode.RightArrow),
        Up = pressed(KeyCode.W) || pressed(KeyCode.UpArrow),
        Down = pressed(KeyCode.S) || pressed(KeyCode.DownArrow),
        LeftPunch = pressed(KeyCode.U), RightPunch = pressed(KeyCode.I),
        LeftKick = pressed(KeyCode.J), RightKick = pressed(KeyCode.K)
      };
    }
    public void PollInput(CallbackPollInput callback) { callback.SetInput(ReadInput(UnityEngine.Input.GetKey), DeterministicInputFlags.Repeatable); }
  }
}
''',encoding='utf-8')

for filename in ['SfxManager.cs','LSDF_AudioSource.cs']:
    old = ROOT/'Assets/Scripts'/filename
    for suffix in ['', '.meta']:
        original = Path(str(old)+suffix); target = VIEW/(filename+suffix)
        assert original.resolve().is_relative_to((ROOT/'Assets/Scripts').resolve()) and target.resolve().is_relative_to(VIEW.resolve())
        assert not target.exists(), target
        original.rename(target)

(VIEW/'SfxManager.cs').write_text('''using System;
using System.Collections.Generic;
using UnityEngine;
namespace Quantum.Asteroids {
  public class SfxManager : MonoBehaviour {
    public AudioSource AudioSourcePrefab;
    public int MaxAudioSources = 16;
    public Transform AudioSourceDefaultParent;
    public LSDF_AudioConfiguration OnHitAudio, OnGuardAudio, OnCounterAudio, OnAttackAudio, OnParringAudio;
    public QuantumEntityViewUpdater ViewUpdater;
    private readonly Stack<AudioSource> _free = new Stack<AudioSource>();
    private readonly List<AudioSource> _active = new List<AudioSource>(), _owned = new List<AudioSource>();
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
    private readonly LSDFPresentationLedger<AudioSource> _sounds = new LSDFPresentationLedger<AudioSource>();
    private void OnEnable() {
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnHitSound>(e => Play(e, OnHitAudio)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnGuardSound>(e => Play(e, OnGuardAudio)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnCounterSound>(e => Play(e, OnCounterAudio)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnAttackSound>(e => Play(e, OnAttackAudio)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnParringSound>(e => Play(e, OnParringAudio)));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventCanceled>(e => {
        if (ViewUpdater != null && e.Game == ViewUpdater.ObservedGame && _sounds.Cancel(e.EventKey, out var source) && source != null) source.Stop();
      }));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventConfirmed>(e => {
        if (ViewUpdater != null && e.Game == ViewUpdater.ObservedGame) _sounds.Confirm(e.EventKey);
      }));
    }
    private AudioSource Available() {
      if (_owned.Count == 0 && AudioSourcePrefab != null) {
        for (int i = 0; i < Mathf.Max(1, MaxAudioSources); i++) {
          var source = Instantiate(AudioSourcePrefab, AudioSourceDefaultParent);
          source.transform.position = Vector3.zero; _owned.Add(source); _free.Push(source);
        }
      }
      AudioSource result;
      if (_free.Count > 0) result = _free.Pop();
      else if (_active.Count > 0) { result = _active[0]; _active.RemoveAt(0); result.Stop(); _sounds.Forget(result); }
      else return null;
      _active.Add(result); return result;
    }
    private void Play(EventBase e, LSDF_AudioConfiguration config) {
      if (ViewUpdater == null || e.Game != ViewUpdater.ObservedGame || !config.IsValid()) return;
      var bindings = ViewUpdater.GetComponent<LSDFSceneBindings>();
      if (bindings == null || !bindings.Accept(ViewUpdater.ObservedGame) || !bindings.ClaimAudio(this)) return;
      var key = LSDFPresentationLedger<AudioSource>.Key(e);
      if (_sounds.Contains(key)) return;
      var source = Available(); if (source == null) return;
      config.AssignToAudioSource(source); source.transform.position = Vector3.zero; source.Play(); _sounds.Track(key, source);
    }
    private void Update() {
      for (int i = _active.Count - 1; i >= 0; i--) if (_active[i] == null || !_active[i].isPlaying) {
        var source = _active[i]; _active.RemoveAt(i); _sounds.Forget(source);
        if (source != null) { source.transform.SetParent(AudioSourceDefaultParent); _free.Push(source); }
      }
    }
    private void OnDisable() {
      foreach (var subscription in _subscriptions) subscription.Dispose(); _subscriptions.Clear();
      _sounds.Clear(source => { if (source != null) source.Stop(); });
      foreach (var source in _active) if (source != null) { source.Stop(); _free.Push(source); }
      _active.Clear();
      if (ViewUpdater != null) ViewUpdater.GetComponent<LSDFSceneBindings>()?.Release(this);
    }
    private void OnDestroy() { foreach (var source in _owned) if (source != null) Destroy(source.gameObject); _owned.Clear(); }
  }
}
''',encoding='utf-8')
print('Replaced View adapters and moved audio with GUIDs; presentation is now scoped to explicit updater bindings.')
