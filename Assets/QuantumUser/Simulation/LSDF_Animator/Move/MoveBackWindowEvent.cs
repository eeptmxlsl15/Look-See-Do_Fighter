using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;

[Serializable]
public class MoveBackWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);

        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        player->isDashFront = false;

        Debug.Log("무브백 시작");
        

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("뒤로 걷기 끝");
    }
}
