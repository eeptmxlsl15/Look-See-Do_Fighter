using System;
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
