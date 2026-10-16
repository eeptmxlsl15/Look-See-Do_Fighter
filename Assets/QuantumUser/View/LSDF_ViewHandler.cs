using Quantum;
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
  private void OnEnable() { if (IsActive && PredictedFrame != null) OnActivate(PredictedFrame); }
  private void OnDisable() { OnDeactivate(); }
}
