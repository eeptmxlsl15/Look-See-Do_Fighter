using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class FirstWallHitWindowEvent : AnimatorTimeWindowEventAsset
{




    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);



        ////������ ���� �ʱ�ȭ
        ////player->isAttack = true;
        ////player->canCounter = true;

        player->isDashFront = false;
        player->isDashBack = false;

        player->isSit = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);
        Debug.Log($"air ���� ������ : {f.Number}");

        player->isWallHit = true;
        player->isAir = false;
        Debug.Log($"��Ʈ ī��Ʈ : {player->hitCount}");
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);

        body->Velocity.Y = -FP._0_50;

    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }
}