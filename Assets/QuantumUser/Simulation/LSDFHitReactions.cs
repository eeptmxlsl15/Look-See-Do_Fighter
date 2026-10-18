using Photon.Deterministic;
using Quantum.Addons.Animator;

namespace Quantum.LSDF {
  /// <summary>Atomic, Frame-owned launch reaction; independent of ordinary graph transition priority.</summary>
  public static unsafe class LSDFHitReactions {
    public static bool EnterAir(Frame frame, EntityRef entity, AnimatorComponent* animator) {
      if (!frame.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return false;
      ClearAttack(frame, entity, player);
      ExpireHitboxes(frame, entity);
      if (animator == null || !animator->AnimatorGraph.Id.IsValid ||
          animator->Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return false;
      var graph = frame.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
      var layers = frame.ResolveList(animator->Layers);
      if (graph == null || graph.Layers == null || graph.Layers.Length == 0 || layers.Count == 0) return false;
      AnimatorState air = null;
      foreach (var state in graph.Layers[0].States) if (state.Name == "Air") { air = state; break; }
      if (air == null) {
        AnimatorComponent.SetTrigger(frame, animator, "Air");
        return false;
      }
      ClearControls(frame, animator);
      var layer = layers.GetPointer(0);
      // A rehit refreshes the physical trajectory without replaying Air's entry window.
      if (layer->CurrentStateId == air.Id && layer->ToStateId == 0) return true;
      var previous = graph.GetState(layer->ToStateId != 0 ? layer->ToStateId : layer->CurrentStateId);
      if (previous != null) frame.Signals.OnAnimatorStateExit(entity, animator, graph, previous);
      // Exit callbacks must not leave an old chain or movement request behind.
      ClearAttack(frame, entity, player);
      ClearControls(frame, animator);
      CommitAir(ref *layer, air.Id, air.GetLength(frame, layer));
      frame.Signals.OnAnimatorStateEnter(entity, animator, graph, air);
      // Do not FadeTo here: advancing a ToState before processing its time-zero
      // window can skip AirWindowEvent.OnEnter. A fresh current state evaluates it once.
      return true;
    }

    private static void ClearAttack(Frame frame, EntityRef entity, LSDF_Player* player) {
      player->isAttack = player->canCounter = player->BufferedNextAttack = false;
      player->isDashFront = player->isDashBack = player->isDodgeHigh = player->isJump = false;
      if (frame.Unsafe.TryGetPointer<LSDF_CommandBuffer>(entity, out var commands))
        LSDFInputBuffer.Clear(ref *commands);
    }

    private static void ClearControls(Frame frame, AnimatorComponent* animator) {
      AnimatorComponent.ResetTrigger(frame, animator, "InputCommand");
      AnimatorComponent.ResetTrigger(frame, animator, "Lp");
      AnimatorComponent.ResetTrigger(frame, animator, "Rp");
      AnimatorComponent.ResetTrigger(frame, animator, "Lk");
      AnimatorComponent.ResetTrigger(frame, animator, "Rk");
      AnimatorComponent.ResetTrigger(frame, animator, "Air");
      AnimatorComponent.ResetTrigger(frame, animator, "HighHit");
      AnimatorComponent.ResetTrigger(frame, animator, "MiddleHit");
      AnimatorComponent.ResetTrigger(frame, animator, "LowHit");
      AnimatorComponent.ResetTrigger(frame, animator, "Stun");
      AnimatorComponent.ResetTrigger(frame, animator, "Combo");
      AnimatorComponent.ResetTrigger(frame, animator, "DelayFrame");
      AnimatorComponent.SetBoolean(frame, animator, "NextAttack", false);
      AnimatorComponent.SetBoolean(frame, animator, "YZero", false);
      AnimatorComponent.SetBoolean(frame, animator, "DashFront", false);
      AnimatorComponent.SetBoolean(frame, animator, "DashBack", false);
      AnimatorComponent.SetBoolean(frame, animator, "MoveFront", false);
      AnimatorComponent.SetBoolean(frame, animator, "MoveBack", false);
    }

    private static void CommitAir(ref LayerData layer, int stateId, FP length) {
      layer.CurrentStateId = stateId;
      layer.Time = layer.LastTime = layer.NormalizedTime = FP._0;
      layer.Length = length;
      layer.FromStateId = layer.ToStateId = 0;
      layer.FromStateTime = layer.FromStateLastTime = layer.FromStateNormalizedTime = layer.FromLength = FP._0;
      layer.ToStateTime = layer.ToStateLastTime = layer.ToStateNormalizedTime = layer.ToLength = FP._0;
      layer.TransitionIndex = 0;
      layer.TransitionTime = layer.TransitionDuration = FP._0;
      layer.IgnoreTransitions = false;
    }

    private static void ExpireHitboxes(Frame frame, EntityRef attacker) {
      foreach (var pair in frame.GetComponentIterator<LSDF_HitboxInfo>()) {
        if (frame.Get<LSDF_HitboxInfo>(pair.Entity).AttackerEntity != attacker) continue;
        if (frame.Unsafe.TryGetPointer<TickToDestroy>(pair.Entity, out var expires)) {
          if (expires->TickToDestroyAt > frame.Number) expires->TickToDestroyAt = frame.Number;
        } else frame.Add(pair.Entity, new TickToDestroy { TickToDestroyAt = frame.Number });
      }
    }
  }
}
