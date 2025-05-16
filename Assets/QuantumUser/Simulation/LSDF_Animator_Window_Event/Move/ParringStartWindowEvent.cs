using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using Photon.Deterministic;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class ParringStartWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
            AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

            player->isSit = false;
        }
        Debug.Log("¿©±â");


    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isParring = true;
        }

    }
}
