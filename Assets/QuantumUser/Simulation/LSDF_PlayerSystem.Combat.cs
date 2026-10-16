using Photon.Deterministic;
using UnityEngine;

namespace Quantum.LSDF
{
    public unsafe partial class LSDF_PlayerSystem
    {
        public void OnTriggerNormalHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            f.Events.OnHitEffect(hitbox->position);
            Debug.Log("이펙트 : 힛 플레이어 시스템");
            f.Events.OnHitSound();

            player->isAttack = false;

            //가드,히트백 전달
            player->forceBack = hitbox->forceBack;


            //상단에 히트할 경유

            //벽에서 벽꽝기 맞았을 때
            if (player->isWallHit == true)
            {
                if (player->wallCount < 2 && hitbox->wallLauncher == false)
                {
                    AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
                    player->wallCount++;
                    Debug.Log("벽콤 카운터 : " + player->wallCount);
                }
                else if (hitbox->wallLauncher == true)
                {
                    Debug.Log("벽콤 벽 부숴짐");
                }
                else if(player->wallCount >= 2)
                {
                    Debug.Log("벽콤 벽 부숴짐");
                }
            }
            else if (hitbox->wallLauncher == true && player->isOnWall == true && player->isWallHit == false)
            {
                Debug.Log("벽콤 벽꽝기 맞음");
                AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");

            }
            //벽콤 맞는 중일 떄
            //콤보 시동기 맞았을 때
            else if (hitbox->launcher == true)
            {
                Debug.Log("콤보 : 런쳐 힛");
                //앉아서 노멀 히트 할때 안뜨는 어퍼라면 히트만 함
                if (hitbox->notSitLauncher == true && player->isSit)
                {
                    Debug.Log("콤보 : 앉아서 맞아서 안 뜸");
                    AnimatorComponent.SetTrigger(f, animator, "MiddleHit");

                }
                else
                {

                    //그게 아니면 띄움
                    Debug.Log("콤보 : 뜸");

                    //플레이어가 벽에 붙어있다면 바로 벽콤

                    if (player->isOnWall)                   {
                        AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
                    }
                    //그게 아니면 공중모션
                    else
                        AnimatorComponent.SetTrigger(f, animator, "Air");


                    //player->isAir = true;

                    //if (f.Unsafe.TryGetPointer<PhysicsBody2D>(info.Entity, out var body))
                    //{

                    //    body->Velocity.X =hitbox->forceBack;    // 필요시 좌우도 밀 수 있음
                    //}
                    //떠서 때리는 애니메니션
                }
            }
            //공중 콤보를 맞는 중일때
            else if (player->isAir)
            {
                Debug.Log("콤보 맞는 중");
                if (f.Unsafe.TryGetPointer<PhysicsBody2D>(info.Entity, out var body))
                {
                    // 필요시 좌우도 밀 수 있음

                    AnimatorComponent.SetTrigger(f, animator, "Air");
                }
            }
            else if (hitbox->GuaranteeAttackType == GuaranteeType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");
            }
            else if ( hitbox->GuaranteeAttackType == GuaranteeType.Combo)
            {
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }
            //상단
            else if (hitbox->AttackType == HitboxAttackType.High)
            {
                Debug.Log($"히트 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "HighHit");

            }
            //중단
            else if (hitbox->AttackType == HitboxAttackType.Mid)
            {
                Debug.Log($"히트 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "MiddleHit");

            }
            //하단
            else if(hitbox->AttackType == HitboxAttackType.Low)
            {
                Debug.Log($"히트 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "LowHit");

            }


            player->isHit = true;
            player->DelayFrame = f.Number + hitbox->enemyHitTime;


            LSDFPlayerHealth.ApplyNormalHit(ref *player, hitbox->attackDamage);
            if (player->hitCount == 0)
            {

                Debug.Log($"현재 체력 : {player->playerHp}/170");
            }
            else if (player->hitCount == 2)
            {

                Debug.Log($"히트 카운트 2 / 데미지 {hitbox->attackDamage *FP._0_75}");
            }
            else if (player->hitCount == 4)
            {

                Debug.Log($"히트 카운트 4 / 데미지 {hitbox->attackDamage * FP._0_50}");
            }
            else if (player->hitCount > 4)
            {

                Debug.Log($"히트 카운트 4 이상  / 데미지 {hitbox->attackDamage * FP._0_33}");
            }



        }


        public void OnTriggerCounterHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            f.Events.OnCounterEffect(hitbox->position);

            f.Events.OnCounterSound();
            //히트 가드백 전달
            player->forceBack = hitbox->forceBack;
            //카운터 시 설정
            player->canCounter = false;
            player->isAttack = false;

            if (hitbox->CountType == CountAttackType.Normal)
            {
                if (hitbox->AttackType == HitboxAttackType.High)
                {
                    Debug.Log($"히트 시작 프레임{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "HighHit");

                }
                //중단
                else if (hitbox->AttackType == HitboxAttackType.Mid)
                {
                    Debug.Log($"히트 시작 프레임{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "MiddleHit");

                }
                //하단
                else if (hitbox->AttackType == HitboxAttackType.Low)
                {
                    Debug.Log($"히트 시작 프레임{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "LowHit");

                }

            }
            //콤보 상황일 경우 띄움
            else if (hitbox->CountType == CountAttackType.Combo)
            {

                AnimatorComponent.SetTrigger(f, animator, "Combo");
                //player->isAir = true;

            }
            else if(hitbox->CountType == CountAttackType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");
            }
            else if (hitbox->CountType == CountAttackType.Air)
            {
                AnimatorComponent.SetTrigger(f, animator, "Air");
            }
            player->isHit = true;
            player->DelayFrame = f.Number + hitbox->enemyCountTime;

            LSDFPlayerHealth.ApplyCounterHit(ref *player, hitbox->attackDamage);//여기서 데미지 보정

            Debug.Log($"현재 체력 : {player->playerHp}/170");


        }
        public void OnTriggerGuard(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator,LSDF_HitboxInfo* hitbox)
        {
            f.Events.OnGuardEffect(hitbox->position);
            f.Events.OnGuardSound();
            //히트 가드백 전달
            player->forceBack = hitbox->forceBack;

            //하단을 가드할 경우
            if (hitbox->AttackType == HitboxAttackType.Low)
            {
                Debug.Log($"가드 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "LowGuard");


            }
            //상,중단을 가드할 경우
            else
            {
                Debug.Log($"가드 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "StandGuard");

            }
            player->isGuard = true;
            player->DelayFrame = f.Number + hitbox->enemyGuardTime;

        }

        public void OnTriggerEnemyGuard(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            if(hitbox->DelayGuardTpye == DelayGuardType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // 필요 시 상태값도 세팅 가능


                //10프레임만 맞음 상태 30프레임
            }
            else if(hitbox->DelayGuardTpye == DelayGuardType.Combo)
            {
                //그냥 노가드 상태 30프레임
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }
        }

        public void OnTriggerEnemyParring(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            f.Events.OnParringEffect(hitbox->position);
            f.Events.OnParringSound();
            if (hitbox->HomingReturnType == HomingType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // 필요 시 상태값도 세팅 가능


                //10프레임만 맞음 상태 30프레임
            }
            else if (hitbox->HomingReturnType == HomingType.Combo)
            {
                //그냥 노가드 상태 30프레임
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }

        }

        public void OnTriggerLauncherHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            //아직 안만듦 -> 필요 없나?
            if (hitbox->HomingReturnType == HomingType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // 필요 시 상태값도 세팅 가능


                //10프레임만 맞음 상태 30프레임
            }
            else if (hitbox->HomingReturnType == HomingType.Combo)
            {
                //그냥 노가드 상태 30프레임
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }

        }
    }
}
