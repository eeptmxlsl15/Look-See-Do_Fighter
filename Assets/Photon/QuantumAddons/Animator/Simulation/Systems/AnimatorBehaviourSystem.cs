namespace Quantum.Addons.Animator {
    using Photon.Deterministic;
    using Quantum.Addons.Animator;
  using UnityEngine.Scripting;

  [Preserve]
    public unsafe class AnimatorBehaviourSystem : SystemSignalsOnly, ISignalOnAnimatorStateEnter, ISignalOnAnimatorStateUpdate, ISignalOnAnimatorStateExit {

        void ISignalOnAnimatorStateEnter.OnAnimatorStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state) {
            foreach (var behaviour in state.StateBehaviours)
                behaviour.OnStateEnter(f, entity, animator, graph, state);
        }

        void ISignalOnAnimatorStateExit.OnAnimatorStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state) {
            foreach (var behaviour in state.StateBehaviours)
                behaviour.OnStateExit(f, entity, animator, graph, state);
        }

        void ISignalOnAnimatorStateUpdate.OnAnimatorStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state, FP time, AnimatorStateType stateType) {
            foreach (var behaviour in state.StateBehaviours)
                behaviour.OnStateUpdate(f, entity, animator, graph, state, time, stateType);
        }
    }
}
