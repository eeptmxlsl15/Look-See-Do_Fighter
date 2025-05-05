using UnityEngine.Scripting;
using UnityEngine;
using Quantum.Collections;
using static UnityEngine.EventSystems.EventTrigger;
using System.Net.Http.Headers;
using UnityEngine.Rendering;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D,ISignalOnCollisionEnter2D, ISignalOnCollisionExit2D
    {

        public void OnCollisionEnter2D(Frame f, CollisionInfo2D info) 
        {
            
            #region 벽에 부딪힐 경우
            
            Debug.Log(info);
            //벽에 부딪히는 경우if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var player))
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Other, out var player))
            {
                f.Unsafe.TryGetPointer<AnimatorComponent>(info.Other, out var defenderAnimator);
                // info.Entity가 플레이어

                //벽에 닿을 경우
                if (f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall))
                {
                    Debug.Log("벽앞");
                    f.Signals.OnCollisionEnterWall(info,player, defenderAnimator, wall);

                    
                    if(player->isAir || player->hitWallLauncher){
                        // 벽에 부딪힘 처리
                        Debug.Log("벽");
                        //여기서 벽 애니메이션 재생하는 시그널 만들어줘야함
                        f.Signals.OnCollisionWallHitEnter(info, player, defenderAnimator, wall);

                    }
                }

                //바닥에 들어올 경우
                if(f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground))
                {
                    f.Signals.OnCollisionGroundEnter(info, player, defenderAnimator, ground);
                }
            }
            #endregion

        }

        public void OnCollisionExit2D(Frame f, ExitInfo2D info)
        {
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Other, out var player))
            {
                f.Unsafe.TryGetPointer<AnimatorComponent>(info.Other, out var defenderAnimator);

                //땅에서 벗어날 경우
                if (f.Unsafe.TryGetPointer<LSDF_Ground>(info.Entity, out var ground))
                {
                    f.Signals.OnCollisionGroundExit(info, player, defenderAnimator, ground);
                }

                //벽에서 벗어날 경우
                if(f.Unsafe.TryGetPointer<LSDF_Wall>(info.Entity, out var wall))
                {
                    f.Signals.OnCollisionExitWall(info, player, defenderAnimator, wall);
                }
            }
        }


        static public bool isGuard;
        public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
        {

            // 내가 맞는 상황이다 내가 defender
            // 피격자 
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var defender))
            {
                // 히트박스에 공격자 정보 포함
                if (f.Unsafe.TryGetPointer<LSDF_HitboxInfo>(info.Other, out var hitbox))
                {
                    // 피격자의 애니메이터
                    f.Unsafe.TryGetPointer<AnimatorComponent>(info.Entity, out var defenderAnimator);

                    //  상태 확인
                    string stateName = f.FindAsset<AnimatorGraph>(defenderAnimator->AnimatorGraph)
                        .GetState(f.ResolveList(defenderAnimator->Layers).GetPointer(0)->CurrentStateId)
                        .Name;

                    //상단 회피, 하단 회피
                    if(defender->isDodgeHigh==true && hitbox->AttackType == HitboxAttackType.High)
                    {
                        Debug.Log("상단 회피");
                        return;
                    }
                    else if(defender->isJump == true && hitbox->AttackType == HitboxAttackType.Low)
                    {
                        Debug.Log("하단 회피");
                        return;
                    }



                    //벽꽝기 맞을때
                    if(defender->hitWallLauncher && defender->isOnWall)
                    {
                        f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                        return;
                    }


                    //벽콤 여부
                    if (defender->isAir&& defender->isOnWall)
                    {
                        Debug.Log("공중에서 벽으로 감");
                        f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                        return;
                    }
                    //카운터 여부
                    if (defender->canCounter)
                    {
                        
                        f.Signals.OnTriggerCounterHit(info, defender, defenderAnimator, hitbox);
                        return;
                    }
                    
                    //10프레임 확정 딜캐 상황일때
                    if (stateName=="Stun")
                    {
                        //10프레임이면 무조건 맞고
                        if (hitbox->startFrame <= 10)
                        {
                            f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                        }

                        //그 외에 가드
                        else if (hitbox->AttackType == HitboxAttackType.High || hitbox->AttackType == HitboxAttackType.Mid)
                        {
                            f.Signals.OnTriggerGuard(info, defender, defenderAnimator, hitbox);
                        }
                        else if (hitbox->AttackType == HitboxAttackType.Low)
                        {
                            f.Signals.OnTriggerGuard(info, defender, defenderAnimator, hitbox);
                        }
                        defender->isStun = false;
                        return;
                    }
                    //막히고 뜨는 경우 
                    else if(stateName == "Combo")
                    {
                        f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);

                        defender->isCombo = false;
                        return;
                    }
                    //그 외에 움직이는 경우(앉기 , 서기 , 앞, 뒤 )
                    else if (isGuard = ShouldGuard(hitbox->AttackType, stateName,defender->isSit))
                    {
                        f.Signals.OnTriggerGuard(info, defender, defenderAnimator, hitbox);

                        //  공격자 휘청
                        if (hitbox->AttackerEntity.IsValid && hitbox->DelayGuardTpye != DelayGuardType.Normal)
                        {
                            f.Unsafe.TryGetPointer<LSDF_Player>(hitbox->AttackerEntity, out var attackerPlayer);
                            f.Unsafe.TryGetPointer<AnimatorComponent>(hitbox->AttackerEntity, out var attackerAnimator);
                            
                            f.Signals.OnTriggerEnemyGuard(info, attackerPlayer, attackerAnimator, hitbox);

                        }
                    }
                    //내가 패링중일때
                    else if(stateName == "Parring")
                    {
                        switch (hitbox->HomingReturnType)
                        {
                            case HomingType.Homing:
                                f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                                break;

                            case HomingType.Stun:
                                
                            case HomingType.Combo:
                                f.Unsafe.TryGetPointer<LSDF_Player>(hitbox->AttackerEntity, out var attackerPlayer);
                                f.Unsafe.TryGetPointer<AnimatorComponent>(hitbox->AttackerEntity, out var attackerAnimator);

                                f.Signals.OnTriggerEnemyParring(info, attackerPlayer, attackerAnimator, hitbox);
                                break;
                        
                        }
                    }
                    else
                    {
                        f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                    }
                }
            }
        }
        private bool ShouldGuard(HitboxAttackType attackType, string state, bool isSit)
        {
            switch (attackType)
            {
                case HitboxAttackType.High:

                case HitboxAttackType.Mid:
                    return state == "Idle" || state == "Move Back" || state == "Dash Back" || state == "Rising"; //일어날때 상하단 가드
                case HitboxAttackType.Low:
                    return state == "Sit Enter" || state == "Siting" || isSit;//일어날때 앉은자세면 가드
                default:
                    return false;
            }
        }

        
    }
}