using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class AirWindowEvent : AnimatorTimeWindowEventAsset
{
    private int currentFrame;



    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        Debug.Log($"air 시작 프레임 : {f.Number}");
        player->hitCount++;
        player->isAir = true;
        //player->isAir = true;
        //Debug.Log($"히트 카운트 : {player->hitCount}");
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);

        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);

        if(currentFrame <= 10)
        {
            if(player->hitCount>2)
                body->Velocity.Y = FP._0_50;
            else if(player->hitCount<=2)
                body->Velocity.Y = 1;
            
        }
        else if(currentFrame <= 30)
        {
                body->Velocity.Y = FP._0_50;
        }
        
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }
}