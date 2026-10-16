using System;
using System.Collections.Generic;
using Quantum;

/// <summary>Per-presenter predicted effects. Confirmation releases bookkeeping; cancellation returns the live effect.</summary>
public sealed class LSDFPresentationLedger<T> {
  private readonly Dictionary<EventKey, T> _pending = new Dictionary<EventKey, T>();
  public int Count => _pending.Count;
  public static EventKey Key(EventBase value) => new EventKey(value.Tick, value.Id, value.GetHashCode());
  public bool Contains(EventKey key) => _pending.ContainsKey(key);
  public void Track(EventKey key, T value) { _pending[key] = value; }
  public void Confirm(EventKey key) { _pending.Remove(key); }
  public bool Cancel(EventKey key, out T value) {
    if (!_pending.TryGetValue(key, out value)) return false;
    _pending.Remove(key);
    return true;
  }
  public void Forget(T value) {
    var keys = new List<EventKey>();
    foreach (var pair in _pending) if (EqualityComparer<T>.Default.Equals(pair.Value, value)) keys.Add(pair.Key);
    foreach (var key in keys) _pending.Remove(key);
  }
  public void Clear(Action<T> release) {
    foreach (var value in _pending.Values) release(value);
    _pending.Clear();
  }
}
