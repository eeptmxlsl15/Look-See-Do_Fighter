using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using UnityEngine.Windows;
using Photon.Deterministic;

[Serializable]
public class HitWindowEvent : AnimatorTimeWindowEventAsset
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
        player->isParring = false;
        
        Debug.Log("힛 시작");

        //상하단 회피 판정 초기화
        player->isDodgeHigh = false;
        player->isJump = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //플레이어
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //회전 고정, 바디
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        //플레이어 링크
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? -1 : 1;

        //히트,가드 백
        body->Velocity.X = player->forceBack * flip;

        //플레이어 앉지 못하게 해야함
        player->isSit = false;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }
}
