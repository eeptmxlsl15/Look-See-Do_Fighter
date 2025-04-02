namespace Quantum.Addons.Animator
{
  using System.Collections.Generic;
  using Photon.Deterministic;
  using System;
  using UnityEngine;

  [Serializable]
  public unsafe class AnimatorState
  {
    public int Id;
    public string Name;
    public bool IsAny;
    public bool IsDefault;
    public FP CycleOffset = FP._0;
    public FP Speed = FP._1;
    [NonSerialized] public AnimatorMotion Motion;
    [HideInInspector] public AnimatorTransition[] Transitions;
    [HideInInspector] public List<SerializableMotion> SerialisedMotions;

    // AssetRef for custom baking. Insert any asset you want here from the Unity baking
    public AssetRef StateAsset;
    
    public List<AnimatorStateBehaviour> StateBehaviours;

    public void FadeTo(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, AnimatorState toState, FP deltaTime,
      bool resetVariables = false)
    {
      FadeTo(f, animatorComponent, layerData, toState, deltaTime, FP._0_10, FP._0, resetVariables);
    }

    public void FadeTo(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, AnimatorState toState, FP deltaTime,
      FP duration,
      bool resetVariables = false)
    {
      FadeTo(f, animatorComponent, layerData, toState, deltaTime, duration, FP._0, resetVariables);
    }

    public void FadeTo(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, AnimatorState toState, FP deltaTime,
      FP duration,
      FP offset,
      bool resetVariables = false)
    {
      // Gets the graph first since the call updates are used for this
      var graph = f.FindAsset<AnimatorGraph>(animatorComponent->AnimatorGraph.Id);

      layerData->TransitionTime = FP._0;
      layerData->TransitionDuration = duration;
      layerData->TransitionIndex = 0;

      // Calls the on state exit signal
      f.Signals.OnAnimatorStateExit(animatorComponent->Self, animatorComponent, graph, graph.GetState(layerData->CurrentStateId));

      layerData->FromStateId = layerData->CurrentStateId;
      layerData->FromStateTime = layerData->Time;
      layerData->FromStateLastTime = layerData->LastTime;
      layerData->FromStateNormalizedTime = layerData->NormalizedTime;
      layerData->FromLength = layerData->Length;

      layerData->ToStateId = toState.Id;
      layerData->ToStateTime = offset;
      layerData->ToStateLastTime = FPMath.Max(offset - deltaTime, FP._0);

      // If AnimatorState.Update run the code for s, the weights are not initialized and we get a divide by zero exception.
      //var s = graph.GetState(a->to_state_id);
      if (toState.GetLength(f, layerData) == 0)
      {
        toState.Motion.CalculateWeights(f, animatorComponent, layerData, layerData->ToStateId);
      }

      layerData->ToLength = toState.GetLength(f, layerData);
      layerData->ToStateNormalizedTime = graph.ClampTime
        ? FPMath.Clamp(layerData->ToStateTime / layerData->ToLength, FP._0, FP._1)
        : layerData->ToStateTime / layerData->ToLength;

      if (resetVariables)
      {
        animatorComponent->ResetVariables(f, animatorComponent, graph);
      }

      // Calls the on state enter signal
      f.Signals.OnAnimatorStateEnter(animatorComponent->Self, animatorComponent, graph, graph.GetState(layerData->ToStateId));
    }

    /// <summary>
    /// Progress the state machine state by a frame
    /// </summary>
    public void Update(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, AnimatorGraph graph, FP deltaTime)
    {
      if (!IsAny)
      {
        if ((Motion == null || Motion.IsEmpty) && !IsDefault)
        {
          layerData->CurrentStateId = 0;
          layerData->FromStateId = 0;
          layerData->ToStateId = 0;
          return;
        }

        if (Motion != null && !Motion.IsEmpty)
        {
          Motion.CalculateWeights(f, animatorComponent, layerData, Id);
          if (Motion.CalculateSpeed(f, animatorComponent, layerData, out var motionSpeed) == false)
          {
            motionSpeed = Speed;
          }

          FP deltaTimeSpeed = deltaTime * motionSpeed;

          //advance time - current state
          if (Id == layerData->CurrentStateId && layerData->ToStateId == 0)
          {
            var length = Motion.CalculateLength(f, layerData, FP._1, this);
            if (length == FP._0)
            {
              return;
            }

            FP sampleTime = layerData->NormalizedTime * length;
            FP currentTime = sampleTime + deltaTimeSpeed;
            FP lastTime = layerData->Time;


            // The time is clamped but only if the graph is specified to do so.
            if (graph.ClampTime)
            {
              if (!Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = length; //clamp
                if (length < lastTime) lastTime = currentTime - deltaTimeSpeed; //clamp
              }

              if (Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = currentTime % length;
                lastTime = currentTime - deltaTimeSpeed;
              }
            }

            FP normalizedTime = currentTime / length;

            if (graph.ClampTime)
            {
              if (Motion.LoopTime)
              {
                if (normalizedTime > 1)
                {
                  currentTime = currentTime  % length;
                }
                normalizedTime = normalizedTime % FP._1;
              }
              else
              {
                normalizedTime = FPMath.Clamp(normalizedTime, FP._0, FP._1);
              }
            }

            layerData->Time = currentTime;
            layerData->LastTime = lastTime;
            layerData->NormalizedTime = normalizedTime;
            layerData->Length = length;

            Motion.ProcessEvents(f, animatorComponent, layerData, Id);

            // Calls the update state signal
            f.Signals.OnAnimatorStateUpdate(animatorComponent->Self, animatorComponent, graph,
              graph.GetState(layerData->CurrentStateId), layerData->Time, AnimatorStateType.CurrentState);
          }

          //advance time - transition state
          if (layerData->FromStateId == Id)
          {
            var length = Motion.CalculateLength(f, layerData, FP._1, this);
            if (length == FP._0) //lengthless motion - ignore
              return;

            FP sampleTime = layerData->FromStateNormalizedTime * length;
            FP lastTime = sampleTime;
            FP currentTime = sampleTime + deltaTimeSpeed;

            if (graph.ClampTime)
            {
              if (!Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = length; //clamp
                if (length < lastTime) lastTime = currentTime - deltaTimeSpeed; //clamp
              }

              if (Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = currentTime % length;
                lastTime = currentTime - deltaTimeSpeed;
              }
            }

            FP normalisedTime = currentTime / length;

            if (graph.ClampTime)
            {
              if (Motion.LoopTime)
              {
                normalisedTime = normalisedTime % FP._1;
              }
              else
              {
                normalisedTime = FPMath.Clamp(normalisedTime, FP._0, FP._1);
              }
            }

            layerData->FromStateTime = currentTime;
            layerData->FromStateLastTime = lastTime;
            layerData->FromStateNormalizedTime = normalisedTime;
            layerData->FromLength = length;

            // Calls the update state signal for the from state.
            f.Signals.OnAnimatorStateUpdate(animatorComponent->Self, animatorComponent, graph,
              graph.GetState(layerData->FromStateId), layerData->FromStateTime, AnimatorStateType.FromState);
          }

          if (layerData->ToStateId == Id)
          {
            var length = Motion.CalculateLength(f, layerData, FP._1, this);
            if (length == FP._0) //lengthless motion - ignore
              return;

            FP sampleTime = layerData->ToStateNormalizedTime * length;
            FP lastTime = sampleTime;
            FP currentTime = sampleTime + deltaTimeSpeed;

            if (graph.ClampTime)
            {
              if (!Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = length; //clamp
                if (length < lastTime) lastTime = currentTime - deltaTimeSpeed; //clamp
              }

              if (Motion.LoopTime && length + deltaTimeSpeed < currentTime)
              {
                currentTime = currentTime % length;
                lastTime = currentTime - deltaTimeSpeed;
              }
            }

            FP normalisedTime = currentTime / length;

            if (graph.ClampTime)
            {
              if (Motion.LoopTime)
              {
                normalisedTime = normalisedTime % FP._1;
              }
              else
              {
                normalisedTime = FPMath.Clamp(normalisedTime, FP._0, FP._1);
              }
            }

            layerData->ToStateTime = currentTime;
            layerData->ToStateLastTime = lastTime;
            layerData->ToStateNormalizedTime = normalisedTime;
            layerData->ToLength = length;

            // Updates the animator state for the To State
            f.Signals.OnAnimatorStateUpdate(animatorComponent->Self, animatorComponent, graph,
              graph.GetState(layerData->ToStateId), layerData->ToStateTime, AnimatorStateType.ToState);
          }
        }
      }

      if (layerData->IgnoreTransitions == false)
      {
        // If we allow transition interruption, we have to make sure that we are only checking transitions for one state.
        // This has to be either the current state if there is no transition OR the to state if there is an active transition.
        if (graph.AllowTransitionInterruption && layerData->ToStateId != 0 &&
            (layerData->CurrentStateId == Id || layerData->FromStateId == Id))
          return;

        for (Int32 i = 0; i < Transitions.Length; i++)
        {
          Transitions[i].Update(f, animatorComponent, layerData, this, deltaTime);
        }
      }
    }

    public FP GetLength(Frame f, LayerData* layerData)
    {
      if (Motion != null && !Motion.IsEmpty)
        return Motion.CalculateLength(f, layerData, FP._1, this);
      return FP._0;
    }

    /// <summary>
    /// Generate the blend list
    /// This is a list of all the animations used in the current state machine frame
    /// The output will be a list of animations and weights that can be used to pose an animation
    /// </summary>
    /// <param name="f">The Quantum Game Frame.</param>
    /// <param name="animatorComponent">The AnimatorComponent.</param>
    /// <param name="layerData">The animation layer to be used during the generation.</param>
    /// <param name="list">The list to build into</param>
    /// <param name="graph"></param>
    public void GenerateBlendList(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData,
      AnimatorLayer layer,
      List<AnimatorRuntimeBlendData> list)
    {
      if (!IsAny)
      {
        if (Motion == null || Motion.IsEmpty && !IsDefault)
        {
          return;
        }

        Motion.CalculateWeights(f, animatorComponent, layerData, Id);

        var length = Motion.CalculateLength(f, layerData, FP._1, this);
        if (length == FP._0)
        {
          return;
        }

        Motion.GenerateBlendList(f, layerData, layer, this, FP._1, list);
      }
    }

    /// <summary>
    /// Get a motion from within a blend tree by the index
    /// </summary>
    /// <returns></returns>
    public AnimatorMotion GetMotion(int treeIndex, List<AnimatorMotion> processList)
    {
      if (Motion != null)
      {
        processList.Add(Motion);
      }

      while (processList.Count > 0)
      {
        AnimatorMotion current = processList[0];
        processList.RemoveAt(0);

        if (current.TreeIndex == treeIndex)
        {
          return current;
        }

        if (current.IsTree)
        {
          if (current is AnimatorBlendTree tree)
          {
            processList.AddRange(tree.Motions);
          }
        }
      }

      return null;
    }
  }
}