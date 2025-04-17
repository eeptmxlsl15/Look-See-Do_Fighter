using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class Bry2RkWindowEvent : AnimatorTimeWindowEventAsset
{
    private int currentFrame;
    private int HitFrame = 15; // 아래 관련 기술은 -1프레임ㅇ해줘야한다 애니메이션이 처음부터 재생은 되는데 2번째 프레임부터 OnEnter가 된다
    //private const int TotalFrameCount = 34;//실제 발동 프레임 + 20

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"브라2rk 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;
        player->canCounter = true;
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);
        Debug.Log($"현재 프레임 {currentFrame}");
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
        if (currentFrame == HitFrame-1)
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_10 - FP._0_02) / 2))
            });

            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                AttackType = HitboxAttackType.Low,
                CountType = CountAttackType.Normal,
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

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }

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