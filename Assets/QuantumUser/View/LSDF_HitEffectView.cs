using System;
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
    private void OnEnable() { if (IsActive && PredictedFrame != null) OnActivate(PredictedFrame); }
    private void OnDisable() { OnDeactivate(); }
  }
}
