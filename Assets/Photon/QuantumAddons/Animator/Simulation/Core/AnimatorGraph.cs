using UnityEngine.Serialization;

namespace Quantum
{
  using System;
  using System.Collections.Generic;
  using Photon.Deterministic;
  using UnityEngine;
  using Addons.Animator;

  public unsafe partial class AnimatorGraph : AssetObject
  {
    public enum Resolutions
    {
      _8 = 8,
      _16 = 16,
      _32 = 32,
      _64 = 64
    }

    [HideInInspector] public bool IsValid = false;

    public Resolutions WeightTableResolution = Resolutions._32;

    public RuntimeAnimatorController Controller;

    public List<AnimationClip> Clips = new List<AnimationClip>();

    public AnimatorLayer[] Layers;
    public AnimatorVariable[] Variables;

    [Tooltip("If true, Calling Animator.Graph FadeTo will function properly; otherwise, an error message will be thrown.")]
    public bool AllowFadeToTransitions = true;

    [Tooltip("If true, root notion will be applied to the Transform2D Component of the animated Entity.")]
    public bool RootMotion = false;

    [Tooltip("If both RootMotion and this are set to true, Root Motion from the animation will be applied to the PhysicsBody2D Component of the animated Entity; note, both RootMotion must also be set to true")]
    public bool RootMotionAppliesPhysics = false;

    [Tooltip("If true, normalized time will be clamp between 0 and 1; if any transitions require the use an exit time greater trhan 1, this should be set to false.")]
    public bool ClampTime = true;

    [Tooltip("If true, transitions can be interrupted by the next state.")]
    public bool AllowTransitionInterruption = false;

    [Tooltip("If true, transitions will be muted when importing the Animator Graph.  This is important to prevent conflicting transitions from occurring when using the AnimatorMecanim.")]
    public bool MuteGraphTransitionsOnExport = false;

    [Tooltip("If true, debug messaging regarding errors will be printed to the console.")]
    public bool DebugMode = true;

    public void Initialise(Frame f, AnimatorComponent* animatorComponent)
    {
      animatorComponent->AnimatorGraph = this.Guid;

      if (animatorComponent->AnimatorVariables.Ptr != default)
      {
        f.FreeList(animatorComponent->AnimatorVariables);
      }

      if (Variables.Length > 0)
      {
        var variablesList = f.AllocateList<AnimatorRuntimeVariable>(Variables.Length);

        // set variable defaults
        for (Int32 variableIndex = 0; variableIndex < Variables.Length; variableIndex++)
        {
          AnimatorRuntimeVariable newParameter = new AnimatorRuntimeVariable();
          switch (Variables[variableIndex].Type)
          {
            case AnimatorVariable.VariableType.FP:
              *newParameter.FPValue = Variables[variableIndex].DefaultFp;
              break;

            case AnimatorVariable.VariableType.Int:
              *newParameter.IntegerValue = Variables[variableIndex].DefaultInt;
              break;

            case AnimatorVariable.VariableType.Bool:
              *newParameter.BooleanValue = Variables[variableIndex].DefaultBool;
              break;

            case AnimatorVariable.VariableType.Trigger:
              *newParameter.BooleanValue = Variables[variableIndex].DefaultBool;
              break;
          }

          variablesList.Add(newParameter);
        }

        animatorComponent->AnimatorVariables = variablesList;
      }

      var layers = f.AllocateList<LayerData>(Layers.Length);
      for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
      {
        var layer = Layers[layerIndex];
        var layerData = new LayerData();
        layerData.Speed = FP._1;
        layerData.CurrentStateId = 0;
        layerData.ToStateId = 0;
        layerData.TransitionTime = FP._0;
        layerData.TransitionDuration = FP._0;

        var blendTreeWeights = f.AllocateDictionary<int, BlendTreeWeights>();
        for (int stateIndex = 0; stateIndex < layer.States.Length; stateIndex++)
        {
          var state = layer.States[stateIndex];
          var weightsList = f.AllocateList<FP>();
          if (state.Motion is AnimatorBlendTree tree)
          {
            for (int motionIndex = 0; motionIndex < tree.MotionCount; motionIndex++)
            {
              weightsList.Add(0);
            }
          }

          if (blendTreeWeights.ContainsKey(state.Id) == false)
          {
            blendTreeWeights.Add(state.Id, new BlendTreeWeights { Values = weightsList });
          }
        }
        layerData.BlendTreeWeights = blendTreeWeights;
        layers.Add(layerData);
      }
      animatorComponent->Layers = layers;
    }
    
    /// <summary>
    /// Updates the state machine graph
    /// </summary>
    public void UpdateGraphState(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, int layerIndex,
      FP deltaTime)
    {
      Layers[layerIndex].Update(f, animatorComponent, layerData, deltaTime);
    }

    /// <summary>
    /// Generates a list of weighted animations used for blending poses in an animation.
    /// </summary>
    /// <param name="f">The Quantum Game Frame.</param>
    /// <param name="animatorComponent">The AnimatorComponent being evaluated.</param>
    /// <param name="output">A list to store the generated <see cref="AnimatorRuntimeBlendData"/>.</param>
    public void GenerateBlendList(Frame f, AnimatorComponent* animatorComponent, List<AnimatorRuntimeBlendData> output)
    {
      var layers = f.ResolveList<LayerData>(animatorComponent->Layers);
      for (Int32 layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
      {
        var layerData = layers.GetPointer(layerIndex);
        Layers[layerIndex].GenerateBlendList(f, animatorComponent, layerData, output);
      }
    }

    public AnimatorFrame CalculateRootMotion(Frame f, AnimatorComponent* animatorComponent,
      List<AnimatorRuntimeBlendData> blendList, List<AnimatorMotion> motionList)
    {
      GenerateBlendList(f, animatorComponent, blendList);
      AnimatorFrame output = new AnimatorFrame();
      
      for (Int32 i = 0; i < blendList.Count; i++)
      {
        var blendData = blendList[i];
        if (blendData.StateId == 0)
        {
          continue;
        }

        var state = GetState(blendData.StateId);
        if (state == null)
        {
          continue;
        }

        var motion = state.GetMotion(blendData.AnimationIndex, motionList);
        if (motion != null)
        {
          if (motion is AnimatorClip clip)
          {
            output += clip.Data.CalculateDelta(blendData.LastTime, blendData.CurrentTime) * blendData.Weight;
          }
        }
      }

      return output;
    }

    public AnimatorState GetState(int stateId)
    {
      for (Int32 layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
      {
        for (Int32 s = 0; s < Layers[layerIndex].States.Length; s++)
        {
          if (Layers[layerIndex].States[s].Id == stateId)
          {
            return Layers[layerIndex].States[s];
          }
        }
      }

      return null;
    }

    public void FadeTo(Frame frame, AnimatorComponent* animatorComponent, string stateName, bool setIgnoreTransitions, FP deltaTime)
    {
      if (AllowFadeToTransitions == false)
      {
        if (DebugMode)
        {
          Debug.LogWarning(
            $"[Quantum Animator] It is not possible to transition to state {stateName}. Enable AllowFadeToTransitions on {name}.");
        }
        return;
      }
      
      var layers = frame.ResolveList<LayerData>(animatorComponent->Layers);
      for (Int32 layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
      {
        for (Int32 stateIndex = 0; stateIndex < Layers[layerIndex].States.Length; stateIndex++)
        {
          if (Layers[layerIndex].States[stateIndex].Name == stateName)
          {
            var state = Layers[layerIndex].States[stateIndex]; 
            var layerData = layers.GetPointer(layerIndex);
            layerData->IgnoreTransitions = setIgnoreTransitions;
            state.FadeTo(frame, animatorComponent, layerData, state, deltaTime, false);
          }
        }
      }
    }

    [Obsolete("GetStateByName is deprecated.")]
    private AnimatorState GetStateByName(string stateName)
    {
      for (Int32 layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
      {
        for (Int32 stateIndex = 0; stateIndex < Layers[layerIndex].States.Length; stateIndex++)
        {
          if (Layers[layerIndex].States[stateIndex].Name == stateName)
          {
            return Layers[layerIndex].States[stateIndex];
          }
        }
      }
      return null;
    }

    public Int32 VariableIndex(string name)
    {
      for (Int32 v = 0; v < Variables.Length; v++)
      {
        if (Variables[v].Name == name)
        {
          return v;
        }
      }

      return -1;
    }
  }
}