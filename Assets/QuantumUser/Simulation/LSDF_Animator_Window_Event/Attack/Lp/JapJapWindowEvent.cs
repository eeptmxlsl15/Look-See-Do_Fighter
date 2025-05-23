using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class JapJapWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    private const int HitFrame = 10; // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다 = 이벤트 시작 위치때문에 -1
    bool bufferedNextAttack;

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"잽잽 시작 프레임{f.Number}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;
        player->canCounter = true;
        //앉은 자세 여부
        player->isSit = false;

        //콤보에 사용 되는 불린
        

        Debug.Log($"잽잽 현재 프레임 : {currentFrame}");
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

        //인풋
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        var input = f.GetPlayerInput(playerLink.PlayerRef);


        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //전진 속도
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
            Debug.Log("잽 Execute");
            //if (input->LeftPunch)
            //{
            //    Debug.Log("셋트리거");
            //    AnimatorComponent.SetTrigger(f, animatorComponent, "Rp");
            //}
        }


        if (5 <= currentFrame && currentFrame <= 28) // 연계 입력 받을 수 있는 구간
        {
            

            //섬광
            if (input->Right)
            {

                bufferedNextAttack = true;  // 일단 예약만 함
                Debug.Log("잽 중에 Lp 입력 → 연계 예약 완료");
            }
        }
        else if (currentFrame >= 6 && bufferedNextAttack)
        {

            AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", true);
            // AnimatorComponent.SetTrigger(f, animatorComponent, "Lp");

            Debug.Log("예약된 Lp 발동");
        }
        //히트 박스 생성
        if (currentFrame == HitFrame - 1)//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new Transform2D
            {
                //위치
                Position = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25 + FP._0_05) * flip, FP._0_25),
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {

                IsTrigger = true,
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2))
            });

            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                startFrame = HitFrame,
                AttackerEntity = entity,

                AttackType = HitboxAttackType.High,
                CountType = CountAttackType.Normal,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Stun,

                jumpAttack = false,
                dodgeHigh = false,


                enemyGuardTime = 21,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 7,
                
            });



            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"잽잽 히트박스 생성된 프레임{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", false);
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;

        player->isAttack = false;

        Debug.Log($"잽잽 끝 프레임 : {f.Number}");
    }
}