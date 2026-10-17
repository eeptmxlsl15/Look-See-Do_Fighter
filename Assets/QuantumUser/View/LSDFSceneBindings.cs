using System;
using System.Collections.Generic;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit scene references and presentation ownership for exactly one EntityViewUpdater.</summary>
public partial class LSDFSceneBindings : MonoBehaviour {
  [Serializable] public class Hud {
    public Image LeftHealth, RightHealth;
    public RectTransform LeftHealthShakeRoot, RightHealthShakeRoot;
    public TMP_Text Timer, Round, Fight;
    public GameObject RoundRoot;
    public Image[] LeftWins = new Image[3];
    public Image[] RightWins = new Image[3];
  }
  public QuantumEntityViewUpdater Updater;
  public GameObject Player1Camera, Player2Camera;
  public Hud Player1Hud = new Hud(), Player2Hud = new Hud();
  private readonly Dictionary<PlayerRef, LSDF_ViewHandler> _fighters = new Dictionary<PlayerRef, LSDF_ViewHandler>();
  private QuantumGame _game;
  private UnityEngine.Object _hudOwner, _effectOwner, _audioOwner;
  public QuantumGame Game => Updater != null ? Updater.ObservedGame : null;
  public static LSDFSceneBindings For(QuantumEntityView view) => view != null && view.EntityViewUpdater != null
    ? view.EntityViewUpdater.GetComponent<LSDFSceneBindings>() : null;
  public bool Accept(QuantumGame game) {
    if (game == null || game != Game) return false;
    if (_game != game) {
      ResetCounterHitZoom();
      _fighters.Clear(); _hudOwner = null; _effectOwner = null; _audioOwner = null; _game = game;
    }
    return true;
  }
  public static bool TryLocal(QuantumGame game, out PlayerRef local) {
    if (game != null) foreach (var player in game.GetLocalPlayers()) {
      if (Quantum.LSDF.LSDFRoster.IsFighter(player)) { local = player; return true; }
    }
    local = default; return false;
  }
  public void Register(PlayerRef slot, LSDF_ViewHandler view) { if (Accept(view.Game)) _fighters[slot] = view; }
  public void Unregister(LSDF_ViewHandler view) {
    var keys = new List<PlayerRef>();
    foreach (var pair in _fighters) if (pair.Value == view) keys.Add(pair.Key);
    foreach (var key in keys) _fighters.Remove(key);
  }
  private static bool Claim(ref UnityEngine.Object owner, UnityEngine.Object candidate) {
    if (owner == null) owner = candidate;
    return owner == candidate;
  }
  public bool ClaimHud(UnityEngine.Object candidate) => Claim(ref _hudOwner, candidate);
  public bool OwnsHud(UnityEngine.Object candidate) => _hudOwner == candidate;
  public bool ClaimEffects(UnityEngine.Object candidate) => Claim(ref _effectOwner, candidate);
  public bool ClaimAudio(UnityEngine.Object candidate) => Claim(ref _audioOwner, candidate);
  public void Release(UnityEngine.Object candidate) {
    if (_hudOwner == candidate) _hudOwner = null;
    if (_effectOwner == candidate) _effectOwner = null;
    if (_audioOwner == candidate) _audioOwner = null;
  }
  public Hud LocalHud(PlayerRef player) => player == (PlayerRef)0 ? Player1Hud : Player2Hud;
  private void LateUpdate() {
    if (!Accept(Game) || !TryLocal(Game, out var local)) { ResetCounterHitZoom(); return; }
    bool first = local == (PlayerRef)0;
    if (Player1Camera != null) Player1Camera.SetActive(first);
    if (Player2Camera != null) Player2Camera.SetActive(!first);
    if (!_fighters.TryGetValue((PlayerRef)0, out var a) || a == null || !_fighters.TryGetValue((PlayerRef)1, out var b) || b == null) { ResetCounterHitZoom(); return; }
    var cameraObject = first ? Player1Camera : Player2Camera;
    if (cameraObject == null) { ResetCounterHitZoom(); return; }
    int flip = first ? 1 : -1;
    var center = (a.transform.position + b.transform.position) * .5f;
    float distance = Vector3.Distance(a.transform.position, b.transform.position);
    float z = -.8f * flip - Mathf.Max(0, distance - 1) * flip;
    UpdateCameraFollow(cameraObject, new Vector3(center.x, 0, z), a.EntityRef, b.EntityRef);
  }
  private void OnDisable() {
    StopCounterHitZoom();
    _fighters.Clear(); _hudOwner = null; _effectOwner = null; _audioOwner = null; _game = null;
  }
}
