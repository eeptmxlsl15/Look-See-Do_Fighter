using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class AirWindowEvent : AnimatorTimeWindowEventAsset
{




    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        

        //공격일 때만 초기화
        //player->isAttack = true;
        //player->canCounter = true;
        
        player->isDashFront = false;
        player->isDashBack = false;
        
        player->isSit = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);
        Debug.Log($"air 시작 프레임 : {f.Number}");
        player->hitCount++;
        player->isAir = true;

        //상하단 회피 판정 초기화
        player->isDodgeHigh = false;
        player->isJump = false;

        //player->isAir = true;
        //Debug.Log($"히트 카운트 : {player->hitCount}");
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);

        int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);

        if(animationFrame <= 10)
        {
            if(player->hitCount>2)
                body->Velocity.Y = FP._0_50;
            else if(player->hitCount<=2)
                body->Velocity.Y = 1;
            
        }
        else if(animationFrame <= 30)
        {
                body->Velocity.Y = FP._0_50;
        }
        
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }
}