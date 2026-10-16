using UnityEngine.Scripting;
namespace Quantum.LSDF {
  [Preserve]
  public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D, ISignalOnCollisionEnter2D, ISignalOnCollisionExit2D {
    private static bool TryFighter(Frame f, EntityRef entity, out LSDF_Player* player, out AnimatorComponent* animator) {
      animator = null;
      return f.Unsafe.TryGetPointer<LSDF_Player>(entity, out player) && f.Unsafe.TryGetPointer<AnimatorComponent>(entity, out animator);
    }
    public void OnCollisionEnter2D(Frame f, CollisionInfo2D info) {
      if (!TryFighter(f, info.Other, out var player, out var animator)) return;
      if (f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall)) {
        f.Signals.OnCollisionEnterWall(info, player, animator, wall);
        if (player->isAir || player->hitWallLauncher) f.Signals.OnCollisionWallHitEnter(info, player, animator, wall);
      }
      if (f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground)) f.Signals.OnCollisionGroundEnter(info, player, animator, ground);
    }
    public void OnCollisionExit2D(Frame f, ExitInfo2D info) {
      if (!TryFighter(f, info.Other, out var player, out var animator)) return;
      if (f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground)) f.Signals.OnCollisionGroundExit(info, player, animator, ground);
      if (f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall)) f.Signals.OnCollisionExitWall(info, player, animator, wall);
    }
    public void OnTriggerEnter2D(Frame f, TriggerInfo2D info) {
      if (!TryFighter(f, info.Entity, out var defender, out var animator)) return;
      if (!f.Unsafe.TryGetPointer<LSDF_HitboxInfo>(info.Other, out var hit)) return;
      if (hit->AttackerEntity == info.Entity || !animator->AnimatorGraph.Id.IsValid) return;
      if (animator->Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return;
      var graph = f.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
      var layers = f.ResolveList(animator->Layers);
      if (graph == null || layers.Count == 0) return;
      var state = graph.GetState(layers.GetPointer(0)->CurrentStateId);
      if (state == null) return;
      var reaction = LSDFContactPolicy.Evaluate(*defender, *hit, state.Name);
      switch (reaction) {
        case LSDFContactReaction.Normal: f.Signals.OnTriggerNormalHit(info, defender, animator, hit); break;
        case LSDFContactReaction.Counter: f.Signals.OnTriggerCounterHit(info, defender, animator, hit); break;
        case LSDFContactReaction.StunHit:
          f.Signals.OnTriggerNormalHit(info, defender, animator, hit); defender->isStun = false; break;
        case LSDFContactReaction.StunGuard:
          f.Signals.OnTriggerGuard(info, defender, animator, hit); defender->isStun = false; break;
        case LSDFContactReaction.StunIgnore: defender->isStun = false; break;
        case LSDFContactReaction.ComboHit:
          f.Signals.OnTriggerNormalHit(info, defender, animator, hit); defender->isCombo = false; break;
        case LSDFContactReaction.Guard:
          f.Signals.OnTriggerGuard(info, defender, animator, hit);
          if (hit->DelayGuardTpye != DelayGuardType.Normal && TryFighter(f, hit->AttackerEntity, out var guardedAttacker, out var guardedAnimator))
            f.Signals.OnTriggerEnemyGuard(info, guardedAttacker, guardedAnimator, hit);
          break;
        case LSDFContactReaction.ParryAttacker:
          if (TryFighter(f, hit->AttackerEntity, out var parriedAttacker, out var parriedAnimator))
            f.Signals.OnTriggerEnemyParring(info, parriedAttacker, parriedAnimator, hit);
          break;
      }
    }
  }
}
