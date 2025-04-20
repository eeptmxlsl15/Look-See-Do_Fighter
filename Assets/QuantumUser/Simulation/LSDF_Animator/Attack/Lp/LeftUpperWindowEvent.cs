using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class LeftUpperWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    //Change
    private const int HitFrame = 13; // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다 = 이벤트 시작 위치때문에 -1


    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"왼어퍼 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

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
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);
        
        //전진성과 방향성
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //Change
        //전진 속도
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
        }

        Debug.Log("잽 Execute");
        //Change
        //히트 박스 생성
        if (currentFrame == HitFrame - 1)//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new Transform2D
            {
                //Change
                //위치
                Position = f.Get<Transform2D>(entity).Position + new FPVector2(FP._0_25 * flip, 0),
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //Change
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33-FP._0_03) / 2))
            });

            //Change
            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                startFrame = HitFrame,
                AttackerEntity = entity,

                AttackType = HitboxAttackType.Mid,
                CountType = CountAttackType.Normal,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Homing,

                jumpAttack = false,
                dodgeHigh = false,

                enemyGuardTime = 19,
                enemyHitTime = 25,
                enemyCountTime = 25,
                attackDamage = 12,
            });

            

            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });
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
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = false;

        Debug.Log($"왼어퍼 끝 프레임 : {f.Number}");
    }
}