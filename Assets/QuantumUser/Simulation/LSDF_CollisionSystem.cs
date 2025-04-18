using UnityEngine.Scripting;
using UnityEngine;
using Quantum.Collections;
using static UnityEngine.EventSystems.EventTrigger;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D
    {
        public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
        {
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


                        else if (hitbox->AttackType == HitboxAttackType.High || hitbox->AttackType == HitboxAttackType.Mid)
                        {
                            f.Signals.OnTriggerGuard(info, defender, defenderAnimator, hitbox);
                        }
                        else if (hitbox->AttackType == HitboxAttackType.Low)
                        {
                            if (defender->isSit == true)
                            {
                                f.Signals.OnTriggerGuard(info, defender, defenderAnimator, hitbox);
                            }
                            else if (defender->isSit == false)
                            {
                                f.Signals.OnTriggerNormalHit(info, defender, defenderAnimator, hitbox);
                            }
                        }
                        return;
                    }

                    bool isGuard = ShouldGuard(hitbox->AttackType, stateName);

                    if (isGuard)
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