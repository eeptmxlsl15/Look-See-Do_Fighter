using Photon.Deterministic;
using Quantum;
using System.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Windows;
using static UnityEngine.EventSystems.EventTrigger;

//플레이어 움직임,가드,히트

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_PlayerSystem : SystemMainThreadFilter<LSDF_PlayerSystem.Filter>, ISignalOnTriggerNormalHit, ISignalOnTriggerGuard, ISignalOnTriggerCounterHit , ISignalOnTriggerEnemyGuard
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform2D* Transform;
            public PhysicsBody2D* Body;
            public LSDF_Player* LSDF_Player;
            public AnimatorComponent* Animator;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            Input* input = default;
            CollisionControll(f, ref filter);
            //공격중일 경우 리턴
            f.Unsafe.TryGetPointer<LSDF_Player>(filter.Entity, out var player);

            


            if (player->isAttack == true || player->isStun==true)
            {
                //Debug.Log("공격 중");
                return;
            }
            
                
            //유저가 가드 중이면서 가드 프레임이 지나면 idle상태로 감
            if (filter.LSDF_Player->isGuard && f.Number >= filter.LSDF_Player->DelayFrame)
            {
                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle로 전이하는 트리거
                Debug.Log($"가드 상태 끝 프레임 : {f.Number}");
                
              
                
                filter.LSDF_Player->isGuard = false;
            }
            else
            {
                
                //Debug.Log($"IsGuard{filter.LSDF_Player->isHit}/현재 프레임{f.Number}/가드 딜레이 프레임{filter.LSDF_Player->DelayFrame}");
            }
            //유저가 히트 중이면서 히트 프레임이 지나면 idle상태로 감
            if (filter.LSDF_Player->isHit && f.Number >= filter.LSDF_Player->DelayFrame)
            {
                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle로 전이하는 트리거
                Debug.Log($"히트 상태 끝 프레임 : {f.Number}");
                
                
                
                filter.LSDF_Player->isHit = false;
            }
            else
            {
               
                //Debug.Log($"IsHit:{filter.LSDF_Player->isHit}/현재 프레임{f.Number}/히트 딜레이 프레임{filter.LSDF_Player->DelayFrame}");
            }


            //Unsate.TryGetPointer는 값을 읽고 수정도 가능하다
            //f.Get<PlayerLink> 로 하면 읽는 것만 가능하다.
            //퀀텀은 TryGetPointer의 시간복잡도가 1이다. 희소집합 ECS 아키텍쳐기 때문에
            //TODO 희소집합 ECS 아키텍쳐 공부

            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                input = f.GetPlayerInput(playerLink->PlayerRef);
                
                    
            }

            //2p에 대한 실험

            //bool shouldAttack = true;
            
            bool shouldAttack = f.Number % 60 < 30;
            if (playerLink->PlayerRef == (PlayerRef)1)
            {
                if (shouldAttack)
                {
                    input->Down = true;
                    //input->RightKick = true;

                }
                else
                {
                    input->LeftPunch = true;
                }
            }
            


            //if (playerLink->PlayerRef == (PlayerRef)0)
            //{

            //}
            //AnimatorComponent.GetBoolean(f, filter.Animator, "MoveBack");

            
            DetectDashCommand(f, ref filter, input);
            UpdateMovement(f, ref filter, input);

        }
        private void UpdateMovement(Frame f, ref Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);
            var playerState = f.Get<LSDF_Player>(filter.Entity);

            //히트박스 크기


            //TODO 걷기 속도 나중에 밖에서 설정 할 수 있게 빼야함
            FP walkSpeed = FP._0_50; ;

            //두번째 플레이어일 경우 반전
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            //아무 입력 없을 때 가만히 있음
            filter.Body->Velocity = FPVector2.Zero;

            //회전 고정
            filter.Transform->Rotation = FP._0;

            

            if (input->Down || (input->Down && input->Left) || (input->Down && input->Right))
            {
                
                //앉기 시작
                //앉아잇는 콜라이더로
                if (filter.LSDF_Player->isSit == false)
                {
                    
                    AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", true);

                    filter.LSDF_Player->isSit = true;
                }
            }
            else
            {
                //서기 시작
                //서있는 콜라이더로 
                
                AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", false);
                filter.LSDF_Player->isSit = false;
            }

            

            //앉아있는 동안 다른 움직임 불가능
            if (filter.LSDF_Player->isSit == true) return;
            
            if (input->Left && input->Right)
            {
                
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);
                return;
            }

            if (input->Up)
            {
                //filter.Body->AddForce(filter.Transform->Up * 8);
                Debug.Log("Parry");
                return;
            }

            if (input->Left && playerState.isDashBack == false)
            {
                filter.Body->Velocity.X = -walkSpeed * flip;

                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
            }

            if (input->Right && playerState.isDashFront == false)
            {
                filter.Body->Velocity.X = walkSpeed * flip;
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);

                //filter.Body->AngularVelocity = FPMath.Clamp(filter.Body->AngularVelocity, -8, 8);
            }


        }

        private void CollisionControll(Frame f, ref Filter filter)
        {
            //크기
            FPVector2 standingColliderExtents = new FPVector2(FP._0_20-FP._0_02, FP._0_33);
            FPVector2 sitColliderExtents = new FPVector2(FP._0_20 - FP._0_02, FP._0_25);

            //위치
            Transform2D standingColliderCenter = new Transform2D
            {
                Position = new FPVector2(FP._0, FP._0_01),
                Rotation = FP._0
            };
            Transform2D crouchingColliderCenter = new Transform2D
            {
                Position = new FPVector2(FP._0, FP._0_25 - FP._0_33 + FP._0_02),
                Rotation = FP._0
            };



            f.Unsafe.TryGetPointer<PhysicsCollider2D>(filter.Entity, out var collider);

            //현재 크기와 위치
            var currentExtents = collider->Shape.Box.Extents;
            var currentCenter = collider->Shape.LocalTransform;

            //앉기,서기에 따라 크기와 위치 변경
            FPVector2 targetExtents = filter.LSDF_Player->isSit ? sitColliderExtents : standingColliderExtents;
            Transform2D targetCenter = filter.LSDF_Player->isSit ? crouchingColliderCenter : standingColliderCenter;

            //현재와 같으면 바꾸지 않음
            if (!currentExtents.Equals(targetExtents))
            {
                collider->Shape.Box.Extents = targetExtents;
                collider->Shape.LocalTransform = targetCenter;
            }
        }

        private void DetectDashCommand(Frame f, ref Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);

            FP walkSpeed = FP._0_50; ;

            //두번째 플레이어일 경우 반전
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            var buffer = f.Unsafe.GetPointer<DashInputBuffer>(filter.Entity);

            DirectionType dir = DirectionType.None;

            if (input->Left) dir = DirectionType.Left;
            else if (input->Right) dir = DirectionType.Right;

            bool isPressed = dir != DirectionType.None;

            if (isPressed && !buffer->LastInputPressed)
            {
                var now = f.Number;

                // 같은 방향이 두 번 눌렸고, 입력 간 간격이 DashInputWindow 이내일 경우
                if (buffer->LastDirection == dir && (now - buffer->LastInputTick) <= buffer->DashInputWindow)
                {

                    // 여기에 대쉬 상태 세팅이나 애니메이션 트리거 넣기
                    if (dir == DirectionType.Right)
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", true);
                    }
                    else
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", true);
                    }

                }

                // 갱신
                buffer->PrevDirection = buffer->LastDirection;
                buffer->LastDirection = dir;
                buffer->LastInputTick = now;
            }

            buffer->LastInputPressed = isPressed;

        }

        public void OnTriggerNormalHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            player->isAttack = false;
            //상단에 히트할 경유
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
            else if(hitbox->AttackType == HitboxAttackType.Low)
            {
                Debug.Log($"히트 시작 프레임{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "LowHit");
                
            }
            player->isHit = true;
            player->DelayFrame = f.Number + hitbox->enemyHitTime;
            player->playerHp -= hitbox->attackDamage;
            Debug.Log($"현재 체력 : {player->playerHp}/170");

        }
        public void OnTriggerCounterHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            //카운터 시 설정
            player->canCounter = false;
            player->isAttack = false;

            if (hitbox->CountType==CountAttackType.Normal)
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
                player->isHit = true;
                player->DelayFrame = f.Number + hitbox->enemyCountTime;

                player->playerHp -= (int)(hitbox->attackDamage *1.2f);//여기서 데미지 보정
                
                Debug.Log($"현재 체력 : {player->playerHp}/170");
            }
            else
            {
                //떠서 때리는 애니메니션 
            }
        }
        public void OnTriggerGuard(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator,LSDF_HitboxInfo* hitbox)
        {
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
            }
        }
    }

}
