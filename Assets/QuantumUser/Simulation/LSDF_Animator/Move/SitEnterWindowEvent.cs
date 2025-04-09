using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;

[Serializable]
public class SitEnterWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("導晦 衛濛");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        player->isDashFront = false;
        player->isDashBack = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("導晦 褒ч");
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("導晦 部");
    }
}
