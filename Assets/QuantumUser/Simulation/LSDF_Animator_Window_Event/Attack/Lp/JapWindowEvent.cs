using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class JapWindowEvent : AnimatorTimeWindowEventAsset
{
    /// <summary>
    /// 시작 프레임
    /// </summary>
    private const int HitFrame = 10;


    private int currentFrame;
    bool bufferedNextAttack;

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 필수 요소 
        Debug.Log($"원잽 시작 프레임{f.Number}");

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

        //만약 처음부터 앉은 자세,점프 판정이라면
        player->isDodgeHigh = false; 
        player->isJump = false;
        
        //---연타 기술 ---//
        //버퍼 넥스트 초기화
        bufferedNextAttack = false;
        Debug.Log($"잽 현재 프레임 : {currentFrame}");


    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 수정 X
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
        #endregion

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

        //연타 관련
        if (5 <= currentFrame && currentFrame <= 15) // 연계 입력 받을 수 있는 구간
        {
            if (input->RightPunch && (AnimatorComponent.GetInteger(f,animatorComponent,"FinalNum")==0))
            {
                
                bufferedNextAttack = true;  // 일단 예약만 함
                Debug.Log("잽 중에 Lp 입력 → 연계 예약 완료");
            }
        }
        else if (currentFrame >= 16&& bufferedNextAttack)
        {
            
            AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", true);
           // AnimatorComponent.SetTrigger(f, animatorComponent, "Lp");

            Debug.Log("예약된 Lp 발동");
        }


        ////상단회피, 점프 
        //if(1<currentFrame && currentFrame< HitFrame-1)
        //{
        //    player->isJump = true;
        //    player->isDodgeHigh = true;
        //}

        //히트 박스 생성
        if (currentFrame == HitFrame - 1)//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            EntityRef hitbox = f.Create();
            
            //플레이어 상태 초기화
            player->isJump = false;
            player->isDodgeHigh = false;

            //-------------------------------------------상단-----------------------------------------//
            
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

            //-------------------------------------------중단-----------------------------------------//

            //f.Add(hitbox, new Transform2D
            //{
            //    //Change
            //    //위치
            //    Position = f.Get<Transform2D>(entity).Position + new FPVector2(FP._0_25 * flip, 0),
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{
            //    IsTrigger = true,
            //    //Change
            //    //박스 크기
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2))
            //});

            //---------------------------------------하단---------------------------------------------//
            ////
            //f.Add(hitbox, new Transform2D
            //{
            //    //위치
            //    Position = f.Get<Transform2D>(entity).Position + new FPVector2(FP._0_25 * flip, -(FP._0_25 + FP._0_03)),
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
                startFrame = HitFrame,
                AttackerEntity = entity,

                AttackType = HitboxAttackType.High,
                CountType = CountAttackType.Normal,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Stun,

                jumpAttack = false,
                dodgeHigh = false,
                launcher = false,
                wallLauncher = false,
                notSitLauncher = false,

                enemyGuardTime = 21,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 7,
                forceBack = 0
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
        ////연타 공격일 경우
        // AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", false);
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        player->isAttack = false;
        player->isJump = false;
        player->isDodgeHigh = false;

        Debug.Log($"원잽 끝 프레임 : {f.Number}");
    }
}