using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;

[Serializable]
public class StandUpWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {

        Debug.Log("Stand Up enter");
    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {

    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("Stand Up OnExit");
    }
}
