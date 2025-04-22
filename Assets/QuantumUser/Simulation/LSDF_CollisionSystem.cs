using UnityEngine.Scripting;
using UnityEngine;
using Quantum.Collections;
using static UnityEngine.EventSystems.EventTrigger;
using System.Net.Http.Headers;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D
    {
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
                    else if (isGuard = ShouldGuard(hitbox->AttackType, stateName))
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
        private bool ShouldGuard(HitboxAttackType attackType, string state)
        {
            switch (attackType)
            {
                case HitboxAttackType.High:

                case HitboxAttackType.Mid:
                    return state == "Idle" || state == "Move Back" || state == "Dash Back";
                case HitboxAttackType.Low:
                    return state == "Sit Enter" || state == "Siting";
                default:
                    return false;
            }
        }

        
    }
}