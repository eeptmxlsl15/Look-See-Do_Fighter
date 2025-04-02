namespace Quantum {
    using Photon.Deterministic;
    using Quantum.Addons.Animator;

    public unsafe abstract class AnimatorStateBehaviour : AssetObject {

        public abstract void OnStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state);

        public abstract void OnStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state);

        public abstract void OnStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, AnimatorState state, FP time, AnimatorStateType stateType);
    }
}
