using Quantum.Addons.Animator;

namespace Quantum.LSDF {
  /// <summary>Runs at the existing CommandSystem position, after Animator and trigger reset.</summary>
  public static unsafe class LSDFCommandProcessor {
    public static void Update(Frame frame, ref LSDF_CommandSystem.Filter filter) {
      if (!frame.Unsafe.TryGetPointer<PlayerLink>(filter.Entity, out var link)) return;
      if (!frame.Unsafe.TryGetPointer<LSDF_CommandBuffer>(filter.Entity, out var buffer)) {
        frame.Add(filter.Entity, new LSDF_CommandBuffer());
        buffer = frame.Unsafe.GetPointer<LSDF_CommandBuffer>(filter.Entity);
      }
      var input = frame.GetPlayerInput(link->PlayerRef);
      if (input == null) {
        LSDFInputBuffer.Clear(ref *buffer);
        buffer->PreviousAttackButtons = 0;
        return;
      }
      int window = LSDFBalance.InputBufferFrames(frame);
      LSDFInputBuffer.Capture(ref *buffer, *input, *filter.LSDF_Player, frame.Number, window);
      bool accepts = buffer->HasPending && CanStartCommand(frame, filter.Entity);
      if (!LSDFInputBuffer.TryRoute(ref *buffer, *filter.LSDF_Player, frame.Number, window, accepts,
          out var direction, out var button)) return;
      int slot = LSDFLoadout.Slot(direction, button);
      if (slot < 0) { LSDFInputBuffer.Clear(ref *buffer); return; }
      AnimatorComponent.SetInteger(frame, filter.Animator, "CommandNum", (int)direction);
      AnimatorComponent.SetInteger(frame, filter.Animator, "FinalNum", filter.LSDF_Player->CommandSkillMap[slot]);
      AnimatorComponent.SetTrigger(frame, filter.Animator, "InputCommand");
      switch (button) {
        case CommandButton.LeftPunch: AnimatorComponent.SetTrigger(frame, filter.Animator, "Lp"); break;
        case CommandButton.RightPunch: AnimatorComponent.SetTrigger(frame, filter.Animator, "Rp"); break;
        case CommandButton.LeftKick: AnimatorComponent.SetTrigger(frame, filter.Animator, "Lk"); break;
        case CommandButton.RightKick: AnimatorComponent.SetTrigger(frame, filter.Animator, "Rk"); break;
      }
    }

    public static bool CanStartCommand(Frame frame, EntityRef entity) {
      if (!frame.TryGet<AnimatorComponent>(entity, out var animator) || !animator.AnimatorGraph.Id.IsValid ||
          animator.Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return false;
      var graph = frame.FindAsset<AnimatorGraph>(animator.AnimatorGraph);
      var layers = frame.ResolveList(animator.Layers);
      if (graph == null || layers.Count == 0) return false;
      var layer = layers.GetPointer(0);
      // A fade is still part of the current action; don't spend a short-lived command inside it.
      return layer->ToStateId == 0 && HasCommandTransition(graph.GetState(layer->CurrentStateId));
    }

    public static bool HasCommandTransition(AnimatorState state) {
      if (state?.Transitions == null) return false;
      foreach (var transition in state.Transitions) {
        if (transition.Conditions == null) continue;
        foreach (var condition in transition.Conditions)
          if (condition.VariableName == "InputCommand") return true;
      }
      return false;
    }
  }
}
