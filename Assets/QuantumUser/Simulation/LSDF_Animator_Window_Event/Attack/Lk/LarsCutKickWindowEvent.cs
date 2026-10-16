using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class LarsCutKickWindowEvent : AnimatorTimeWindowEventAsset
{
    /// <summary>
    /// 시작 프레임
    /// </summary>
    public Quantum.LSDF.LSDFTimedHitDefinition FirstHit = Quantum.LSDF.LSDFTimedHitDefinition.CreateLarsFirst();
    public Quantum.LSDF.LSDFTimedHitDefinition SecondHit = Quantum.LSDF.LSDFTimedHitDefinition.CreateLarsSecond();




    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 필수 요소 
        Debug.Log($"원잽 시작 프레임{f.Number}");

        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

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
        Debug.Log($"잽 현재 프레임 : {Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time)}");


    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 수정 X
        //플레이어 정보
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        //현재 프레임
        int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);

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
        if (animationFrame < 26)
        {
            body->Velocity.X = flip;
            Debug.Log("잽 Execute");
            //if (input->LeftPunch)
            //{
            //    Debug.Log("셋트리거");
            //    AnimatorComponent.SetTrigger(f, animatorComponent, "Rp");
            //}
        }

        ////연타 관련
        //if (5 <= animationFrame && animationFrame <= 15) // 연계 입력 받을 수 있는 구간
        //{
        //    if (input->RightPunch && (AnimatorComponent.GetInteger(f, animatorComponent, "FinalNum") == 0))
        //    {

        //        Debug.Log("잽 중에 Lp 입력 → 연계 예약 완료");
        //    }
        //}
        //{

        //    AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", true);
        //    // AnimatorComponent.SetTrigger(f, animatorComponent, "Lp");

        //    Debug.Log("예약된 Lp 발동");
        //}


        //상단회피, 점프 
        if (1 < animationFrame && animationFrame < FirstHit.Hit.StartupFrames - 1)
        {
            player->isJump = true;
            player->isDodgeHigh = true;
        }
        #region 1타
        //히트 박스 생성
        if (FirstHit.IsSpawnFrame(animationFrame))//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            player->isJump = false;
            player->isDodgeHigh = false;
            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, FirstHit.Hit, effectPositionOverride: FPVector2.Zero);
            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"잽 히트박스 생성된 프레임{f.Number}");
        }

        #endregion 2타
        #region 1타
        if (SecondHit.IsSpawnFrame(animationFrame))//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            player->isJump = false;
            player->isDodgeHigh = false;
            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, SecondHit.Hit);
            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"잽 히트박스 생성된 프레임{f.Number}");
        }
        #endregion
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        ////연타 공격일 경우
        // AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", false);
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;

        player->isAttack = false;
        player->isJump = false;
        player->isDodgeHigh = false;

        Debug.Log($"원잽 끝 프레임 : {f.Number}");
    }
}