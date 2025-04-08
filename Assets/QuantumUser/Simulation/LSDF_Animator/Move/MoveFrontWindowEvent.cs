using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;

[Serializable]
public class MoveFrontWWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"[Quantum Animator ({f.Number})] OnEnter animator time window event.");
    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"[Quantum Animator ({f.Number})] Execute animator time window event.");
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"[Quantum Animator ({f.Number})] OnExit animator time window event.");
    }
}
