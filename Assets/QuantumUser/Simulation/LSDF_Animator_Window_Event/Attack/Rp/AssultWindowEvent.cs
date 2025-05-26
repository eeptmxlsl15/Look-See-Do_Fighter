using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class AssultWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    private const int HitFrame = 18; // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다 = 이벤트 시작 위치때문에 -1
    bool bufferedNextAttack;

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 필수 요소 
        Debug.Log($"어썰트 시작 프레임{f.Number}");

        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //공격일 때만 초기화
        player->isAttack = true;
        player->canCounter = true;

        player->isDashFront = false;
        player->isDashBack = false;
        //앉은 자세 여부
        player->isSit = false;
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);

        #endregion
        //---연타 기술 ---//
        //버퍼 넥스트 초기화
        bufferedNextAttack = false;
        Debug.Log($"잽 현재 프레임 : {currentFrame}");


    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //플레이어 정보
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //현재 프레임
        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);

        //회전 고정
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        //인풋
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        var input = f.GetPlayerInput(playerLink.PlayerRef);


        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //전진 속도
        if (currentFrame < 38)
        {
            body->Velocity.X = FP._1_50*flip;
            Debug.Log("잽 Execute");
            //if (input->LeftPunch)
            //{
            //    Debug.Log("셋트리거");
            //    AnimatorComponent.SetTrigger(f, animatorComponent, "Rp");
            //}
        }
        
        


        //히트 박스 생성
        if (currentFrame == HitFrame - 1)//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            EntityRef hitbox = f.Create();

            //-------------------------------------------상단-----------------------------------------//
            //FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25 + FP._0_05) * flip, FP._0_25);

            //f.Add(hitbox, new Transform2D
            //{
            //    //위치
            //    Position = hitboxPosition,
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{

            //    IsTrigger = true,
            //    //박스 크기
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2))
            //});

            ////-------------------------------------------중단-----------------------------------------//
            FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25) * flip, 0);
            f.Add(hitbox, new Transform2D
            {
                //Change
                //위치
                Position = hitboxPosition,
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //Change
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2))
            });

            //---------------------------------------하단---------------------------------------------//
            //FPVector2 hitboxPosition=f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25) * flip, -(FP._0_25 + FP._0_03));

            //f.Add(hitbox, new Transform2D
            //{
            //    //위치
            //    Position =hitboxPosition,
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{
            //    IsTrigger = true,
            //    //박스 크기
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_10 - FP._0_02) / 2))
            //});
            //------------------------------------------------------------------------------------//
            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                position = hitboxPosition,
                startFrame = HitFrame,
                AttackerEntity = entity,

                AttackType = HitboxAttackType.Mid,
                CountType = CountAttackType.Combo,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Combo,

                

                jumpAttack = false,
                dodgeHigh = false,
                wallLauncher = true,

                enemyGuardTime = 26,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 25,
                forceBack = 2
            });



            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"잽 히트박스 생성된 프레임{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;

        Debug.Log($"원잽 끝 프레임 : {f.Number}");
    }
}