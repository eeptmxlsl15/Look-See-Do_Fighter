using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using UnityEngine.Windows;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class StunWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        
        //player->isAttack = true;
        //player->isDashFront = false;
        //player->isDashBack = false;
        //player->canCounter = true;

        //앉은 자세 여부
        player->isSit = false;
        player->isAttack = false;
        Debug.Log("스턴 시작");

        player->isStun = true;

        //상하단 회피 판정 초기화
        player->isDodgeHigh = false;
        player->isJump = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

        //공격 시 전진 상태가 끝나기 전에 판정 나와서 
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);
        body->Velocity.X = 0;
    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //var entity = animatorComponent->Self;

        //// PlayerRef 가져오기
        //if (!f.Unsafe.TryGetPointer<PlayerLink>(entity, out var playerLink))
        //    return;

        //var input = f.GetPlayerInput(playerLink->PlayerRef);
        //if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        //    return;

        //// 입력에 따라 isSit 제어
        //if (input->Down)
        //{
        //    player->isSit = true;
        //}
        //else
        //{
        //    player->isSit = false;
        //}
        //Debug.Log($"앉기 여부 : {player->isSit}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);
        body->Velocity.X = 0;

    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        //앉은 자세 여부
        //player->isSit = false;
        Debug.Log("스턴 끝");
        player->isStun = false;
    }
}
