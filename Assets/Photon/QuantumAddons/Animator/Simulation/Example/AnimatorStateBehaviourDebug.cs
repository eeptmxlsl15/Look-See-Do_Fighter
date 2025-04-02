namespace Quantum
{
  using Photon.Deterministic;
  using Quantum.Addons.Animator;

  public unsafe class AnimatorStateBehaviourDebug : AnimatorStateBehaviour
  {
    public string DebugMessage;

    public override unsafe void OnStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator,
      AnimatorGraph graph, AnimatorState state)
    {
      UnityEngine.Debug.Log("Entered State:  " + state.Name + " || " + DebugMessage);
    }

    public override unsafe void OnStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph,
      AnimatorState state)
    {
      UnityEngine.Debug.Log("Exited State:  " + state.Name + " || " + DebugMessage);
    }

    public override unsafe void OnStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator,
      AnimatorGraph graph, AnimatorState state, FP time, AnimatorStateType stateType)
    {
      UnityEngine.Debug.Log("Updating State:  " + state.Name + " || state is " + stateType + " || " + DebugMessage);
    }
  }
}