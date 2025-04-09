using Photon.Deterministic;
using Quantum;
using System.Data;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Scripting;
using UnityEngine.Windows;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_PlayerSystem : SystemMainThreadFilter<LSDF_PlayerSystem.Filter>
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
            //Unsate.TryGetPointer는 값을 읽고 수정도 가능하다
            //f.Get<PlayerLink> 로 하면 읽는 것만 가능하다.
            //퀀텀은 TryGetPointer의 시간복잡도가 1이다. 희소집합 ECS 아키텍쳐기 때문에
            //TODO 희소집합 ECS 아키텍쳐 공부
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                input = f.GetPlayerInput(playerLink->PlayerRef);
            }

            
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
                    //싯엔터 온엔터 부분에 넣어야하는데 안되는 중
                    filter.LSDF_Player->isDashFront = false;
                    filter.LSDF_Player->isDashBack = false;

                    AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", false);
                    AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", false);
                    //여기까지
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

            //충돌 크기 제어
            CollisionControll(f,ref filter);

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

            if (input->Left&& playerState.isDashBack == false)
            {
                filter.Body->Velocity.X = -walkSpeed * flip;
                
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
            }

            if (input->Right&& playerState.isDashFront == false)
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
            FPVector2 standingColliderExtents = new FPVector2(FP._0_10, FP._0_33);
            FPVector2 sitColliderExtents = new FPVector2(FP._0_10, FP._0_25);

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
    }
}
