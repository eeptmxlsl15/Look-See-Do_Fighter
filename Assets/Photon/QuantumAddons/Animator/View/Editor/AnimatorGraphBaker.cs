﻿namespace Quantum.Addons.Animator
{
  using System;
  using System.Collections.Generic;
  using Photon.Deterministic;
  using UnityEditor;
  using UnityEditor.Animations;
  using UnityEngine;
  using UA = UnityEditor.Animations;
  using Quantum;
  using System.Linq;

  public class AnimatorGraphBaker : MonoBehaviour
  {
    public static void OnBake(AnimatorGraph asset)
    {
      try
      {
        BakeAnimatorGraphAsset(asset, asset.Controller);
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }

      EditorUtility.SetDirty(asset);
      AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Quantum Animator/ Bake all Graph Assets")]
    internal static void BakeAll()
    {
      string[] guids = AssetDatabase.FindAssets($"t:{nameof(AnimatorGraph)}");
      var assets = guids
        .Select(guid => AssetDatabase.LoadAssetAtPath<AnimatorGraph>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

      foreach (var asset in assets)
      {
        OnBake(asset);
      }
    }
    
    
    public static void BakeAnimatorGraphAsset(AnimatorGraph graphAsset, RuntimeAnimatorController runtimeController)
    {
      if (runtimeController == null)
      {
        graphAsset.IsValid = false;
        throw new Exception(
          string.Format($"[Quantum Animator] AnimatorGraph Controller is not valid, fix {graphAsset.name} before importing animations."));
      }

      var controller = (UnityEditor.Animations.AnimatorController)runtimeController;

      if (!graphAsset)
      {
        return;
      }

      int weightTableResolution = (int)graphAsset.WeightTableResolution;
      int variableCount = controller.parameters.Length;


      graphAsset.Variables = new AnimatorVariable[variableCount];
      
      #region Mecanim Parameters/Variables
      // Mecanim Parameters/Variables
      // Make a dictionary of paramets by name for use when extracting conditions for transitions
      Dictionary<string, AnimatorControllerParameter> parameterDic =
        new Dictionary<string, AnimatorControllerParameter>();
      for (int i = 0; i < variableCount; i++)
      {
        AnimatorControllerParameter parameter = controller.parameters[i];
        parameterDic.Add(parameter.name, parameter);
        AnimatorVariable newVariable = new AnimatorVariable();

        newVariable.Name = parameter.name;
        newVariable.Index = i;
        switch (parameter.type)
        {
          case AnimatorControllerParameterType.Bool:
            newVariable.Type = AnimatorVariable.VariableType.Bool;
            newVariable.DefaultBool = parameter.defaultBool;
            break;
          case AnimatorControllerParameterType.Float:
            newVariable.Type = AnimatorVariable.VariableType.FP;
            newVariable.DefaultFp = FP.FromFloat_UNSAFE(parameter.defaultFloat);
            break;
          case AnimatorControllerParameterType.Int:
            newVariable.Type = AnimatorVariable.VariableType.Int;
            newVariable.DefaultInt = parameter.defaultInt;
            break;
          case AnimatorControllerParameterType.Trigger:
            newVariable.Type = AnimatorVariable.VariableType.Trigger;
            break;
        }

        graphAsset.Variables[i] = newVariable;
      }
      #endregion
      

      // Mecanim State Graph
      var clips = new List<AnimationClip>();
      int layerCount = controller.layers.Length;
      graphAsset.Layers = new AnimatorLayer[layerCount];
      for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
      {
        AnimatorLayer newLayer = new AnimatorLayer();
        newLayer.Name = controller.layers[layerIndex].name;
        newLayer.Id = layerIndex;

        // Gets all states in the layer regardless if in a sub state machine or not.
        List<UnityEditor.Animations.AnimatorState> stateList = new List<UnityEditor.Animations.AnimatorState>();
        PopulateStateList(stateList, controller.layers[layerIndex].stateMachine);
        int stateCount = controller.layers[layerIndex].stateMachine.states.Length;
        newLayer.States = new AnimatorState[stateCount + 1]; // additional element for the any state
        Dictionary<UA.AnimatorState, AnimatorState> stateDictionary =
          new Dictionary<UA.AnimatorState, AnimatorState>();

        for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
        {
          UnityEditor.Animations.AnimatorState state = controller.layers[layerIndex].stateMachine.states[stateIndex]
            .state;
          AnimatorState newState = new AnimatorState();
          newState.Name = state.name;
          newState.Id = state.nameHash;
          newState.IsDefault = controller.layers[layerIndex].stateMachine.defaultState == state;
          newState.Speed = FP.FromFloat_UNSAFE(state.speed);
          newState.CycleOffset = FP.FromFloat_UNSAFE(state.cycleOffset);

          if (state.motion != null)
          {
            AnimationClip clip = state.motion as AnimationClip;
            if (clip != null)
            {
              clips.Add(clip);
              AnimatorClip newClip = new AnimatorClip();
              newClip.Name = state.motion.name;
              newClip.Id = clip.name.GetHashCode();
              newClip.Data = Extract(clip);
              newState.Motion = newClip;
            }
            else
            {
              BlendTree tree = state.motion as BlendTree;
              if (tree != null)
              {
                foreach (var child in tree.children)
                {
                  if (child.motion == null)
                  {
                    graphAsset.IsValid = false;
                    throw new Exception(string.Format(
                      "There is a missing motion on State {0}. This is no allowed, fix before importing animations.",
                      state.name));
                  }
                }

                int childCount = tree.children.Length;

                AnimatorBlendTree newBlendTree = new AnimatorBlendTree();
                newBlendTree.Name = state.motion.name;
                newBlendTree.MotionCount = childCount;
                newBlendTree.Motions = new AnimatorMotion[childCount];
                newBlendTree.Positions = new FPVector2[childCount];
                newBlendTree.TimesScale = new FP[childCount];

                string parameterXname = tree.blendParameter;
                string parameterYname = tree.blendParameterY;

                for (int v = 0; v < variableCount; v++)
                {
                  if (controller.parameters[v].name == parameterXname)
                    newBlendTree.BlendParameterIndex = v;
                  if (controller.parameters[v].name == parameterYname)
                    newBlendTree.BlendParameterIndexY = v;
                }

                if (tree.blendType == BlendTreeType.Simple1D)
                {
                  newBlendTree.BlendParameterIndexY = newBlendTree.BlendParameterIndex;
                }

                if (newBlendTree.BlendParameterIndex == -1)
                {
                  Debug.LogError(
                    $"[Quantum Animator] Blend Tree parameter named {parameterXname} was not found on the Animator Controller during the baking process");
                }

                if (tree.blendType == BlendTreeType.Simple1D && newBlendTree.BlendParameterIndexY == -1)
                {
                  Debug.LogError(
                    $"[Quantum Animator] Blend Tree parameter named {parameterYname} was not found on the Animator Controller during the baking process");
                }

                for (int c = 0; c < childCount; c++)
                {
                  ChildMotion cMotion = tree.children[c];
                  AnimationClip cClip = cMotion.motion as AnimationClip;
                  if (tree.blendType == BlendTreeType.Simple1D)
                  {
                    newBlendTree.Positions[c] = new FPVector2(FP.FromFloat_UNSAFE(cMotion.threshold), 0);
                    newBlendTree.TimesScale[c] = FP.FromFloat_UNSAFE(cMotion.timeScale);
                  }
                  else
                  {
                    newBlendTree.Positions[c] = new FPVector2(FP.FromFloat_UNSAFE(cMotion.position.x),
                      FP.FromFloat_UNSAFE(cMotion.position.y));
                    //TODO timesScale
                  }

                  if (cClip != null)
                  {
                    clips.Add(cClip);
                    AnimatorClip newClip = new AnimatorClip();
                    newClip.Data = Extract(cClip);
                    newClip.Name = newClip.ClipName;
                    newClip.Id = cClip.name.GetHashCode();
                    newBlendTree.Motions[c] = newClip;
                  }
                }

                newBlendTree.CalculateWeightTable(weightTableResolution);

                //Debug WeightTable
                System.Text.StringBuilder debugString = new System.Text.StringBuilder();
                debugString.Append("weightTable content:\n");

                for (int i = 0; i < newBlendTree.WeightTable.GetLength(0); i++)
                {
                  for (int j = 0; j < newBlendTree.WeightTable.GetLength(1); j++)
                  {
                    FP[] arrayElement = newBlendTree.WeightTable[i, j];

                    debugString.Append($"weightTable[{i},{j}] = [");
                    for (int k = 0; k < arrayElement.Length; k++)
                    {
                      debugString.Append(arrayElement[k].ToString());
                      if (k < arrayElement.Length - 1)
                      {
                        debugString.Append(", ");
                      }
                    }

                    debugString.Append("]\n");
                  }
                }
                //Debug.Log(debugString);

                newBlendTree.CalculateTimeScaleTable(weightTableResolution);

                //Debug SpeedTable
                debugString = new System.Text.StringBuilder();
                debugString.Append("speedTable content:\n");

                for (int i = 0; i < newBlendTree.TimeScaleTable.GetLength(0); i++)
                {
                  debugString.Append($"speedTable[{i}] = [");
                  debugString.Append($"{newBlendTree.TimeScaleTable[i]}");
                  debugString.Append("]\n");
                }
                //Debug.Log(debugString);

                newState.Motion = newBlendTree;
              }
            }
          }

          newLayer.States[stateIndex] = newState;

          stateDictionary.Add(state, newState);
        }

        #region State Transitions

        // State Transitions
        // once the states have all been created
        // we'll hook up the transitions
        for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
        {
          UnityEditor.Animations.AnimatorState state = controller.layers[layerIndex].stateMachine.states[stateIndex].state;
          AnimatorState newState = newLayer.States[stateIndex];
          int transitionCount = state.transitions.Length;
          newState.Transitions = new AnimatorTransition[transitionCount];
          for (int transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
          {
            AnimatorStateTransition unityAnimatorStateTransition = state.transitions[transitionIndex];

            // Done to prevent transitions from occuring with the AnimatorMecanim system.  Might be better to be an optional graph parameter
            if (graphAsset.MuteGraphTransitionsOnExport)
              unityAnimatorStateTransition.mute = true;

            var destinationState = unityAnimatorStateTransition.isExit
              ? controller.layers[layerIndex].stateMachine.defaultState
              : unityAnimatorStateTransition.destinationState;
            if (!stateDictionary.ContainsKey(destinationState)) continue;

            //if (state.motion == null)
            //{
            //  throw new Exception(string.Format("Baking '{0}' failed: Animation state '{1}' in controller '{2}' (layer '{3}') requires a motion.", dataAsset.name, state.name, controller.name, controller.layers[l].name));
            //}
            //if (destinationState.motion == null)
            //{
            //  throw new Exception(string.Format("Baking '{0}' failed: Animation state '{1}' in controller '{2}' (layer '{3}') requires a motion.", dataAsset.name, destinationState.name, controller.name, controller.layers[l].name));
            //}

            AnimatorTransition newTransition = new AnimatorTransition();
            newTransition.Index = transitionIndex;
            newTransition.Name = string.Format("{0} to {1}", state.name, destinationState.name);

            FP transitionDuration = unityAnimatorStateTransition.duration.ToFP();
            FP transitionOffset = unityAnimatorStateTransition.offset.ToFP();
            if (unityAnimatorStateTransition.hasFixedDuration == false && state.motion != null && destinationState.motion != null)
            {
              transitionDuration *= state.motion.averageDuration.ToFP();
              transitionOffset *= destinationState.motion.averageDuration.ToFP();
            }

            newTransition.Duration = transitionDuration;
            newTransition.Offset = transitionOffset;
            newTransition.HasExitTime = unityAnimatorStateTransition.hasExitTime;

            var exitTime = state.motion != null
              ? unityAnimatorStateTransition.exitTime * state.motion.averageDuration
              : unityAnimatorStateTransition.exitTime;

            newTransition.ExitTime = FP.FromFloat_UNSAFE(exitTime);
            newTransition.DestinationStateId = stateDictionary[destinationState].Id;
            newTransition.DestinationStateName = stateDictionary[destinationState].Name;
            newTransition.CanTransitionToSelf =
              false; // Only any state transitions should be able to transition to themselves (might need to adjust)


            int conditionCount = unityAnimatorStateTransition.conditions.Length;
            newTransition.Conditions = new AnimatorCondition[conditionCount];
            for (int conditionIndex = 0; conditionIndex < conditionCount; conditionIndex++)
            {
              UnityEditor.Animations.AnimatorCondition condition = state.transitions[transitionIndex].conditions[conditionIndex];

              if (!parameterDic.ContainsKey(condition.parameter)) continue;
              AnimatorControllerParameter parameter = parameterDic[condition.parameter];
              
              AnimatorCondition newCondition = new AnimatorCondition();

              newCondition.VariableName = condition.parameter;
              newCondition.Mode = (AnimatorCondition.Modes)condition.mode;

              SetupConditionParameter(newTransition, condition, ref newCondition, parameter.type);
              newTransition.Conditions[conditionIndex] = newCondition;
            }
            newState.Transitions[transitionIndex] = newTransition;
          }
        }

        #endregion

        

        // This populates the animator behaviours for the state...
        for (int s = 0; s < stateCount; s++)
        {
          UnityEditor.Animations.AnimatorState state = stateList[s];
          AnimatorState newState = newLayer.States[s];

          newState.StateBehaviours = new List<AnimatorStateBehaviour>();
          foreach (var behaviour in state.behaviours)
          {
            if (behaviour is AnimatorStateBehaviourHolder holder && holder.AnimatorStateBehaviourAssets != null)
            {
              newState.StateBehaviours.AddRange(
                holder.AnimatorStateBehaviourAssets
                  .Select(QuantumUnityDB.GetGlobalAsset)
                  .Where(currentBehaviour => currentBehaviour != null)
              );
            }
          }

          // Mostly done to prevent users from adding null state behaviours.
          newState.StateBehaviours.RemoveAll(x => x == null);
        }

        #region AnyState

        //Create Any State
        AnimatorState anyState = new AnimatorState();
        anyState.Name = "Any State";
        anyState.Id = anyState.Name.GetHashCode();
        anyState.IsAny = true; //important for this one
        AnimatorStateTransition[] anyStateTransitions = controller.layers[layerIndex].stateMachine.anyStateTransitions;
        int anyStateTransitionCount = anyStateTransitions.Length;
        anyState.Transitions = new AnimatorTransition[anyStateTransitionCount];
        for (int t = 0; t < anyStateTransitionCount; t++)
        {
          // Done to prevent transitions from occuring with the AnimatorMecanim system.  Might be better to be an optional graph parameter
          if (graphAsset.MuteGraphTransitionsOnExport)
            anyStateTransitions[t].mute = true;

          AnimatorStateTransition transition = anyStateTransitions[t];
          if (!stateDictionary.ContainsKey(transition.destinationState)) continue;
          AnimatorTransition newTransition = new AnimatorTransition();
          newTransition.Index = t;
          newTransition.Name = string.Format("Any State to {0}", transition.destinationState.name);
          newTransition.Duration = FP.FromFloat_UNSAFE(transition.duration);
          newTransition.HasExitTime = transition.hasExitTime;
          newTransition.ExitTime = FP._1;
          newTransition.Offset =
            FP.FromFloat_UNSAFE(transition.offset * transition.destinationState.motion.averageDuration);
          newTransition.DestinationStateId = stateDictionary[transition.destinationState].Id;
          newTransition.DestinationStateName = stateDictionary[transition.destinationState].Name;
          newTransition.CanTransitionToSelf = transition.canTransitionToSelf;

          int conditionCount = transition.conditions.Length;
          newTransition.Conditions = new AnimatorCondition[conditionCount];
          for (int c = 0; c < conditionCount; c++)
          {
            UnityEditor.Animations.AnimatorCondition condition = anyStateTransitions[t].conditions[c];

            if (!parameterDic.ContainsKey(condition.parameter)) continue;
            AnimatorControllerParameter parameter = parameterDic[condition.parameter];
            AnimatorCondition newCondition = new AnimatorCondition();

            newCondition.VariableName = condition.parameter;
            newCondition.Mode = (AnimatorCondition.Modes)condition.mode;

            switch (parameter.type)
            {
              case AnimatorControllerParameterType.Float:
                newCondition.ThresholdFp = FP.FromFloat_UNSAFE(condition.threshold);
                break;

              case AnimatorControllerParameterType.Int:
                newCondition.ThresholdInt = Mathf.RoundToInt(condition.threshold);
                break;
            }

            newTransition.Conditions[c] = newCondition;
          }

          anyState.Transitions[t] = newTransition;
        }

        #endregion
        
        newLayer.States[stateCount] = anyState;
        graphAsset.Layers[layerIndex] = newLayer;
      }

      AnimatorGraph.Serialize(graphAsset);

      // Actually write the quantum asset onto the scriptable object.
      graphAsset.Clips = clips;
      //dataAsset = quantumGraph;
      graphAsset.Controller = controller;

      graphAsset.IsValid = true;

      EditorUtility.SetDirty(graphAsset);

      Debug.Log($"[Quantum Animator] Imported {graphAsset.name} data.");
    }

    private static void SetupConditionParameter(AnimatorTransition transition, UnityEditor.Animations.AnimatorCondition unityCondition,
      ref AnimatorCondition newCondition, AnimatorControllerParameterType parameterType)
    {
      bool isValidCondition = true;
      switch (parameterType)
      {
        case AnimatorControllerParameterType.Bool:
          if (newCondition.Mode != AnimatorCondition.Modes.If && newCondition.Mode != AnimatorCondition.Modes.IfNot)
            isValidCondition = false;
          break;
        case AnimatorControllerParameterType.Float:
          newCondition.ThresholdFp = FP.FromFloat_UNSAFE(unityCondition.threshold);
          if (newCondition.Mode != AnimatorCondition.Modes.Greater && newCondition.Mode != AnimatorCondition.Modes.Less)
            isValidCondition = false;
          break;
        case AnimatorControllerParameterType.Int:
          newCondition.ThresholdInt = Mathf.RoundToInt(unityCondition.threshold);
          if (newCondition.Mode != AnimatorCondition.Modes.Greater 
              && newCondition.Mode != AnimatorCondition.Modes.Less
              && newCondition.Mode != AnimatorCondition.Modes.Equals
              && newCondition.Mode != AnimatorCondition.Modes.NotEqual)
            isValidCondition = false;
          
          break;
        case AnimatorControllerParameterType.Trigger:
          if (newCondition.Mode != AnimatorCondition.Modes.If)
            isValidCondition = false;
          break;
      }

      if (isValidCondition == false)
      {
        Debug.LogWarning($"The transition from {transition.Name} has an invalid condition. " +
                         $"Please recreate the condition that uses the parameter {newCondition.VariableName} ");

      }
      
    }

    /// <summary>
    /// Populates the list with all of the state machine's states and then traverses into the sub state machines of the state machine
    /// </summary>
    /// <param name="stateList">The list of Unity AnimatorStates to populate.</param>
    /// <param name="stateMachine">The state machine using to populate the list.</param>
    private static void PopulateStateList(List<UnityEditor.Animations.AnimatorState> stateList,
      AnimatorStateMachine stateMachine)
    {
      for (int i = 0; i < stateMachine.states.Length; i++)
      {
        stateList.Add(stateMachine.states[i].state);
      }

      for (int i = 0; i < stateMachine.stateMachines.Length; i++)
      {
        PopulateStateList(stateList, stateMachine.stateMachines[i].stateMachine);
      }
    }

    public static AnimatorData Extract(AnimationClip clip)
    {
      AnimatorData animationData = new AnimatorData();
      animationData.ClipName = clip.name;
      animationData.MotionId = clip.name.GetHashCode();

      EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
      AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

      float usedTime = settings.stopTime - settings.startTime;

      animationData.FrameRate = Mathf.RoundToInt(clip.frameRate);
      animationData.Length = FP.FromFloat_UNSAFE(usedTime);
      animationData.FrameCount = Mathf.RoundToInt(clip.frameRate * usedTime);
      animationData.Frames = new AnimatorFrame[animationData.FrameCount];
      animationData.LoopTime = clip.isLooping && settings.loopTime;
      animationData.Mirror = settings.mirror;
      animationData.Events = ProcessEvents(clip);

      //Read the curves of animation
      int frameCount = animationData.FrameCount;
      int curveBindingsLength = curveBindings.Length;
      if (curveBindingsLength == 0) return animationData;

      AnimationCurve curveTx = null,
        curveTy = null,
        curveTz = null,
        curveRx = null,
        curveRy = null,
        curveRz = null,
        curveRw = null;

      for (int c = 0; c < curveBindingsLength; c++)
      {
        string propertyName = curveBindings[c].propertyName;

        //Check if the current property is a source for root motion
        if (curveBindings[c].path.Split("/").Length != 1)
          continue;

        if (propertyName == "m_LocalPosition.x" || propertyName == "RootT.x")
          curveTx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalPosition.y" || propertyName == "RootT.y")
          curveTy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalPosition.z" || propertyName == "RootT.z")
          curveTz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);

        if (propertyName == "m_LocalRotation.x" || propertyName == "RootQ.x")
          curveRx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.y" || propertyName == "RootQ.y")
          curveRy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.z" || propertyName == "RootQ.z")
          curveRz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.w" || propertyName == "RootQ.w")
          curveRw = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
      }

      //        if (curveBindingsLength >= 7)
      //        {
      //            //Position Curves
      //            curveTx = AnimationUtility.GetEditorCurve(clip, curveBindings[0]);
      //            curveTy = AnimationUtility.GetEditorCurve(clip, curveBindings[1]);
      //            curveTz = AnimationUtility.GetEditorCurve(clip, curveBindings[2]);
      //
      //            //Rotation Curves
      //            curveRx = AnimationUtility.GetEditorCurve(clip, curveBindings[3]);
      //            curveRy = AnimationUtility.GetEditorCurve(clip, curveBindings[4]);
      //            curveRz = AnimationUtility.GetEditorCurve(clip, curveBindings[5]);
      //            curveRw = AnimationUtility.GetEditorCurve(clip, curveBindings[6]);
      //        }
      
      bool hasPosition = curveTx != null && curveTy != null && curveTz != null;
      bool hasRotation = curveRx != null && curveRy != null && curveRz != null && curveRw != null;

      if (!hasPosition) Debug.LogWarning("No movement data was found in the animation: " + clip.name);
      if (!hasRotation) Debug.LogWarning("No rotation data was found in the animation: " + clip.name);

      // The initial pose might not be the first frame and might not face foward
      // calculate the initial direction and create an offset Quaternion to apply to transforms;

      Quaternion startRotUq = Quaternion.identity;
      FPQuaternion startRot = FPQuaternion.Identity;
      if (hasRotation)
      {
        float srotxu = curveRx.Evaluate(settings.startTime);
        float srotyu = curveRy.Evaluate(settings.startTime);
        float srotzu = curveRz.Evaluate(settings.startTime);
        float srotwu = curveRw.Evaluate(settings.startTime);

        FP srotx = FP.FromFloat_UNSAFE(srotxu);
        FP sroty = FP.FromFloat_UNSAFE(srotyu);
        FP srotz = FP.FromFloat_UNSAFE(srotzu);
        FP srotw = FP.FromFloat_UNSAFE(srotwu);

        startRotUq = new Quaternion(srotxu, srotyu, srotzu, srotwu);
        startRot = new FPQuaternion(srotx, sroty, srotz, srotw);
      }

      Quaternion offsetRotUq = Quaternion.Inverse(startRotUq);
      FPQuaternion offsetRot = FPQuaternion.Inverse(startRot);

      for (int i = 0; i < frameCount; i++)
      {
        var frameData = new AnimatorFrame();
        frameData.Id = i;
        float percent = i / (frameCount - 1f);
        float frameTime = usedTime * percent;
        frameData.Time = FP.FromFloat_UNSAFE(frameTime);
        float clipTIme = settings.startTime + percent * (settings.stopTime - settings.startTime);

        if (hasPosition)
        {
          FP posx = FP.FromFloat_UNSAFE(i > 0 ? curveTx.Evaluate(clipTIme) - curveTx.Evaluate(settings.startTime) : 0);
          FP posy = FP.FromFloat_UNSAFE(i > 0 ? curveTy.Evaluate(clipTIme) - curveTy.Evaluate(settings.startTime) : 0);
          FP posz = FP.FromFloat_UNSAFE(i > 0 ? curveTz.Evaluate(clipTIme) - curveTz.Evaluate(settings.startTime) : 0);
          FPVector3 newPosition = offsetRot * new FPVector3(posx, posy, posz);
          if (settings.mirror) newPosition.X = -newPosition.X;
          frameData.Position = newPosition;
        }

        if (hasRotation)
        {
          float curveRxEval = curveRx.Evaluate(clipTIme);
          float curveRyEval = curveRy.Evaluate(clipTIme);
          float curveRzEval = curveRz.Evaluate(clipTIme);
          float curveRwEval = curveRw.Evaluate(clipTIme);
          Quaternion curveRotation = offsetRotUq * new Quaternion(curveRxEval, curveRyEval, curveRzEval, curveRwEval);
          if (settings.mirror) //mirror the Y axis rotation
          {
            Quaternion mirrorRotation =
              new Quaternion(curveRotation.x, -curveRotation.y, -curveRotation.z, curveRotation.w);

            if (Quaternion.Dot(curveRotation, mirrorRotation) < 0)
            {
              mirrorRotation = new Quaternion(-mirrorRotation.x, -mirrorRotation.y, -mirrorRotation.z,
                -mirrorRotation.w);
            }

            curveRotation = mirrorRotation;
          }

          FP rotx = FP.FromFloat_UNSAFE(curveRotation.x);
          FP roty = FP.FromFloat_UNSAFE(curveRotation.y);
          FP rotz = FP.FromFloat_UNSAFE(curveRotation.z);
          FP rotw = FP.FromFloat_UNSAFE(curveRotation.w);
          FPQuaternion newRotation = new FPQuaternion(rotx, roty, rotz, rotw);
          frameData.Rotation = FPQuaternion.Product(offsetRot, newRotation);

          float rotY = curveRotation.eulerAngles.y * Mathf.Deg2Rad;
          while (rotY < -Mathf.PI) rotY += Mathf.PI * 2;
          while (rotY > Mathf.PI) rotY += -Mathf.PI * 2;
          frameData.RotationY = FP.FromFloat_UNSAFE(rotY);
        }

        animationData.Frames[i] = frameData;
      }

      return animationData;
    }

    private static AnimatorEvent[] ProcessEvents(AnimationClip unityClip)
    {
      var clipEvents = new List<AnimatorEvent>();
      for (int i = 0; i < unityClip.events.Length; i++)
      {
        var unityEvent = unityClip.events[i];
        var animationEventData = (IAnimatorEventAsset)unityEvent.objectReferenceParameter;
        if (animationEventData != null)
        {
          var newEvent = animationEventData.OnBake(unityClip, unityEvent);
          if (newEvent != null)
          {
            clipEvents.Add(newEvent);
          }
        }
      }

      return clipEvents.ToArray();
    }
  }
}