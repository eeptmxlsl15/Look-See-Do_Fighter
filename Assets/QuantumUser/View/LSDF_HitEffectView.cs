using System;
using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
  public class LSDF_HitEffectView : QuantumSceneViewComponent {
    public GameObject HitParticle, GuardParticle, ParringParticle, CounterParticle;
    [Range(0, 1)] public float HitOpacity = 1;
    [Range(0, 1)] public float GuardOpacity = 1;
    [Range(0, 1)] public float ParringOpacity = 1;
    [Range(0, 1)] public float CounterOpacity = 1;
    [Min(.01f)] public float FallbackLifetime = .15f;
    [Min(.01f)] public float MaximumLifetime = 2f;
    private sealed class Playback {
      public GameObject Root;
      public Animator Animator;
      public bool Billboard;
      public float Elapsed;
      public int Kind;
      public SpriteRenderer[] Sprites;
      public float[] SpriteAlphas;
    }
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
    private readonly LSDFPresentationLedger<GameObject> _effects = new LSDFPresentationLedger<GameObject>();
    private readonly List<Playback> _playing = new List<Playback>();
    private LSDFSceneBindings _bindings;
    public override void OnActivate(Frame frame) {
      OnDeactivate();
      _bindings = Updater != null ? Updater.GetComponent<LSDFSceneBindings>() : null;
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnHitEffect>(e => Spawn(e, HitParticle, e.position, 0)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnGuardEffect>(e => Spawn(e, GuardParticle, e.position, 1)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnCounterEffect>(e => Spawn(e, CounterParticle, e.position, 3)));
      _subscriptions.Add(QuantumEvent.SubscribeManual<EventOnParringEffect>(e => Spawn(e, ParringParticle, e.position, 2)));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventCanceled>(e => {
        if (e.Game == Game && _effects.Cancel(e.EventKey, out var effect) && effect != null) Destroy(effect);
      }));
      _subscriptions.Add(QuantumCallback.SubscribeManual<CallbackEventConfirmed>(e => { if (e.Game == Game) _effects.Confirm(e.EventKey); }));
    }
    private float Opacity(int kind) => Mathf.Clamp01(kind == 0 ? HitOpacity : kind == 1 ? GuardOpacity : kind == 2 ? ParringOpacity : CounterOpacity);
    private void ApplyOpacity(Playback effect) {
      float opacity = Opacity(effect.Kind);
      for (int i = 0; i < effect.Sprites.Length; ++i) {
        var sprite = effect.Sprites[i];
        if (sprite == null) continue;
        var color = sprite.color; color.a = effect.SpriteAlphas[i] * opacity; sprite.color = color;
      }
    }
    private void Spawn(EventBase e, GameObject prefab, FPVector2 position, int kind) {
      if (!IsActiveAndEnabled || e.Game != Game || _bindings == null || !_bindings.Accept(Game) || !_bindings.ClaimEffects(this) || prefab == null) return;
      var key = LSDFPresentationLedger<GameObject>.Key(e);
      if (_effects.Contains(key) || Opacity(kind) <= 0) return;
      bool billboard = prefab.GetComponentInChildren<SpriteRenderer>(true) != null;
      var camera = PresentationCamera();
      var rotation = billboard && camera != null ? camera.transform.rotation : prefab.transform.rotation;
      var effect = Instantiate(prefab, new Vector3(position.X.AsFloat, position.Y.AsFloat, 0), rotation, transform);
      var animator = effect.GetComponentInChildren<Animator>();
      if (animator != null && animator.runtimeAnimatorController != null) animator.Update(0);
      if (effect.TryGetComponent<ParticleSystem>(out var particle)) particle.Play();
      var sprites = effect.GetComponentsInChildren<SpriteRenderer>(true);
      var playback = new Playback { Root = effect, Animator = animator, Billboard = billboard, Kind = kind,
        Sprites = sprites, SpriteAlphas = new float[sprites.Length] };
      for (int i = 0; i < sprites.Length; ++i) playback.SpriteAlphas[i] = sprites[i].color.a;
      ApplyOpacity(playback);
      _playing.Add(playback);
      _effects.Track(key, effect);
    }
    private Camera PresentationCamera() {
      if (_bindings == null) return null;
      var first = _bindings.Player1Camera;
      if (first != null && first.activeInHierarchy) return first.GetComponent<Camera>();
      var second = _bindings.Player2Camera;
      return second != null && second.activeInHierarchy ? second.GetComponent<Camera>() : null;
    }
    private void LateUpdate() {
      var camera = PresentationCamera();
      for (int i = _playing.Count - 1; i >= 0; --i) {
        var effect = _playing[i];
        if (effect.Root == null) { _playing.RemoveAt(i); continue; }
        ApplyOpacity(effect);
        if (effect.Billboard && camera != null) effect.Root.transform.rotation = camera.transform.rotation;
        effect.Elapsed += Time.deltaTime;
        bool animated = effect.Animator != null && effect.Animator.runtimeAnimatorController != null;
        bool finished = animated && effect.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        float lifetime = animated ? MaximumLifetime : FallbackLifetime;
        if (!finished && effect.Elapsed < Mathf.Max(.01f, lifetime)) continue;
        Destroy(effect.Root); _playing.RemoveAt(i);
      }
    }
    public override void OnDeactivate() {
      foreach (var subscription in _subscriptions) subscription.Dispose(); _subscriptions.Clear();
      _effects.Clear(_ => { });
      foreach (var effect in _playing) if (effect.Root != null) Destroy(effect.Root);
      _playing.Clear();
      if (_bindings != null) _bindings.Release(this); _bindings = null;
    }
    public override void OnGameChanged() { OnDeactivate(); if (Game != null) OnActivate(PredictedFrame); }
    public override void OnDisable() { base.OnDisable(); OnDeactivate(); }
  }
}
