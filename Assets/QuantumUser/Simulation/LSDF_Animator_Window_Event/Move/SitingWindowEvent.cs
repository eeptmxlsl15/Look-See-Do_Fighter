using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;

[Serializable]
public class SitingWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {

        Debug.Log("siting enter");
    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("siting OnExit");
    }
}
