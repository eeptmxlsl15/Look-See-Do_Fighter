using Photon.Deterministic;
using Quantum.Addons.Animator;
using UnityEngine.Scripting;
namespace Quantum.LSDF {
  [Preserve]
  public unsafe class LSDF_IngameSystem : SystemMainThread {
    public override void Update(Frame f) {
      if (!LSDFRoster.TryPair(f, out var first, out var second)) return;
      if (!f.Unsafe.TryGetPointer<LSDF_Player>(first, out var a) || !f.Unsafe.TryGetPointer<LSDF_Player>(second, out var b)) return;
      if (LSDFRoundPolicy.TryFinish(ref *a, ref *b, f.Number, LSDFBalance.RoundResultTicks(f))) {
        BeginPose(f, first, a->playerHp <= 0 ? "Sit Enter" : "Idle", a->playerHp > 0);
        BeginPose(f, second, b->playerHp <= 0 ? "Sit Enter" : "Idle", b->playerHp > 0);
      }
      if (LSDFRoundPolicy.TryBeginNextRound(ref *a, ref *b, f.Number, LSDFBalance.MaximumHealth(f), LSDFBalance.RoundIntroTicks(f))) {
        ResetRoundEntity(f, first, a);
        ResetRoundEntity(f, second, b);
        f.Global->Time = LSDFBalance.RoundSeconds(f);
      }
      if (LSDFRoundPolicy.TryStartFighting(ref *a, ref *b, f.Number)) {
        AllowTransitions(f, first);
        AllowTransitions(f, second);
      }
      if (!a->isRoundEnd || !b->isRoundEnd) return;
      MaintainLock(f, first, a);
      MaintainLock(f, second, b);
      // Cancel old attack windows, including hitboxes spawned while fading out this tick.
      foreach (var pair in f.GetComponentIterator<LSDF_HitboxInfo>()) {
        var hit = f.Get<LSDF_HitboxInfo>(pair.Entity);
        if (hit.AttackerEntity == first || hit.AttackerEntity == second) f.Destroy(pair.Entity);
      }
    }

    private static bool TryAnimator(Frame f, EntityRef entity, out AnimatorComponent* animator, out AnimatorGraph graph) {
      graph = null;
      if (!f.Unsafe.TryGetPointer<AnimatorComponent>(entity, out animator) || !animator->AnimatorGraph.Id.IsValid ||
          animator->Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return false;
      graph = f.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
      return graph != null && f.ResolveList(animator->Layers).Count > 0;
    }

    private static void BeginPose(Frame f, EntityRef entity, string state, bool ignoreTransitions) {
      LSDFJuggle.Cancel(f, entity);
      if (!TryAnimator(f, entity, out var animator, out var graph)) return;
      ClearVariables(f, animator, graph);
      graph.FadeTo(f, animator, state, ignoreTransitions, f.DeltaTime);
      // Exit callbacks can leave attack triggers. The installed SDK ResetVariables activates
      // every Trigger, so clear triggers explicitly instead of calling it.
      ClearVariables(f, animator, graph);
      AnimatorComponent.SetBoolean(f, animator, "IsSit", state == "Sit Enter");
    }

    private static void ClearVariables(Frame f, AnimatorComponent* animator, AnimatorGraph graph) {
      foreach (var variable in graph.Variables) {
        switch (variable.Type) {
          case AnimatorVariable.VariableType.Trigger: AnimatorComponent.ResetTrigger(f, animator, variable.Index); break;
          case AnimatorVariable.VariableType.Bool: AnimatorComponent.SetBoolean(f, animator, variable.Index, variable.DefaultBool); break;
          case AnimatorVariable.VariableType.Int: AnimatorComponent.SetInteger(f, animator, variable.Index, variable.DefaultInt); break;
          case AnimatorVariable.VariableType.FP: AnimatorComponent.SetFixedPoint(f, animator, variable.Index, variable.DefaultFp); break;
        }
      }
    }

    private static void AllowTransitions(Frame f, EntityRef entity) {
      if (!TryAnimator(f, entity, out var animator, out var graph)) return;
      ClearVariables(f, animator, graph);
      if (f.TryGet<LSDF_Player>(entity, out var player)) AnimatorComponent.SetBoolean(f, animator, "Ground", player.isGround);
      var layers = f.ResolveList(animator->Layers);
      for (int i = 0; i < layers.Count; i++) layers.GetPointer(i)->IgnoreTransitions = false;
    }

    private static void ResetRoundEntity(Frame f, EntityRef entity, LSDF_Player* player) {
      var link = f.Get<PlayerLink>(entity);
      if (f.Unsafe.TryGetPointer<Transform2D>(entity, out var transform))
        transform->Teleport(f, new FPVector2(link.PlayerRef == (PlayerRef)0 ? -LSDFBalance.SpawnOffset(f) : LSDFBalance.SpawnOffset(f), 0), FP._0);
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) body->Velocity = FPVector2.Zero;
      if (f.Unsafe.TryGetPointer<DashInputBuffer>(entity, out var dash))
        *dash = new DashInputBuffer { DashInputWindow = dash->DashInputWindow };
      if (f.Unsafe.TryGetPointer<LSDF_CommandBuffer>(entity, out var buffer)) LSDFInputBuffer.Clear(ref *buffer);
      BeginPose(f, entity, "Idle", true);
      LSDFRoundPolicy.FinishPose(ref *player);
      // UpdateCollider resolves components from Entity; a complete movement filter is not required.
      var filter = new LSDF_PlayerSystem.Filter { Entity = entity, LSDF_Player = player };
      LSDFPlayerMotion.UpdateCollider(f, ref filter);
    }

    private static void MaintainLock(Frame f, EntityRef entity, LSDF_Player* player) {
      LSDFRoundPolicy.FinishPose(ref *player);
      if (player->playerHp <= 0 && TryAnimator(f, entity, out var animator, out var graph)) {
        var layer = f.ResolveList(animator->Layers).GetPointer(0);
        var state = graph.GetState(layer->CurrentStateId);
        // Do not freeze Sit Enter: let the existing graph play its timed transition to Siting.
        if (layer->ToStateId == 0 && state != null && state.Name == "Siting") layer->IgnoreTransitions = true;
      }
      if (f.Unsafe.TryGetPointer<LSDF_CommandBuffer>(entity, out var buffer)) LSDFInputBuffer.Clear(ref *buffer);
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) {
        body->Velocity.X = 0;
        // Do not freeze an airborne KO in mid-air: stop rising and allow gravity to land it.
        if (body->Velocity.Y > 0) body->Velocity.Y = 0;
      }
    }
  }
}
