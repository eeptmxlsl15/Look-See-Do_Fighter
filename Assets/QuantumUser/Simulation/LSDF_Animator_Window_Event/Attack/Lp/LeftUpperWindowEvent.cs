using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class LeftUpperWindowEvent : AnimatorTimeWindowEventAsset
{


    //Change
    public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFSingleHitDefinition.CreateLeftUpper(); // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다 = 이벤트 시작 위치때문에 -1


    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"왼어퍼 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;
        player->canCounter = true;

        //앉은 자세 여부
        player->isSit = false;


        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);
        
        //전진성과 방향성
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //Change
        //전진 속도
        if (animationFrame < Hit.StartupFrames)
        {
            body->Velocity.X = flip;
        }

        Debug.Log("잽 Execute");
        //Change
        //히트 박스 생성
        if (Hit.IsSpawnFrame(animationFrame))//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, Hit);
            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"왼어퍼 히트박스 생성된 프레임{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;

        Debug.Log($"왼어퍼 끝 프레임 : {f.Number}");
    }
}