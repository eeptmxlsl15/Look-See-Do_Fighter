using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class Bry2RkWindowEvent : AnimatorTimeWindowEventAsset
{

    public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFRemainingAttackDefaults.Bry2Rk(); // 아래 관련 기술은 -1프레임ㅇ해줘야한다 애니메이션이 처음부터 재생은 되는데 2번째 프레임부터 OnEnter가 된다

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"브라2rk 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;
        player->canCounter = true;

        player->isSit = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        if (animationFrame < Hit.StartupFrames)
        {
            body->Velocity.X = flip;
        }

        Debug.Log("2rk 공격 중");
        if (animationFrame == Hit.StartupFrames-1)
        {

            EntityRef hitbox = f.Create();

            Quantum.LSDF.LSDFHitboxFactory.ConfigureBody(f, hitbox, entity, flip, Hit);

            Quantum.LSDF.LSDFHitboxFactory.AddPayload(f, hitbox, entity, Hit, sourceAttack: Guid);

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }

            Quantum.LSDF.LSDFHitboxFactory.ScheduleDestroy(f, hitbox);

            Debug.Log($"브라2rk 히트박스 생성된 프레임{f.Number}");
        }
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;

        Debug.Log($"브라2rk 끝 프레임 : {f.Number}");
    }
}