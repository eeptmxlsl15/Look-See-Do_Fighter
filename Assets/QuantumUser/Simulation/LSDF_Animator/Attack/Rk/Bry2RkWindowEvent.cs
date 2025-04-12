using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class Bry2RkWindowEvent : AnimatorTimeWindowEventAsset
{
    private const int HitFrame = 14; // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다
    private const int TotalFrameCount = 35;//실제 발동 프레임 + 20

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"브라2rk 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        int currentFrame = (int)((layerData->Time / layerData->Length) * TotalFrameCount);

        //전진성과 방향성
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //전진 속도
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
        }

        Debug.Log("2rk 공격 중");
        //히트 박스 생성
        if (currentFrame == HitFrame)
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_25 / 2, (FP._0_10 - FP._0_02) / 2))
            });

            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                AttackType = HitboxAttackType.Low,

                enemyGuardTime = 10,
                enemyHitTime = 20,
                enemyCountTime = 20,
                attackDamage = 13,
            });

            f.Set(hitbox, new Transform2D
            {
                //위치
                Position = f.Get<Transform2D>(entity).Position + new FPVector2(FP._0_25 * flip, -(FP._0_25+FP._0_03)),
                Rotation = FP._0
            });

            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            Debug.Log($"브라2rk 히트박스 생성된 프레임{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = false;

        Debug.Log($"브라2rk 끝 프레임 : {f.Number}");
    }
}