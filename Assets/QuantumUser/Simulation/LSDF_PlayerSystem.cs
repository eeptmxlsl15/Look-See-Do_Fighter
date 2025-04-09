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
            //Unsate.TryGetPointer�� ���� �а� ������ �����ϴ�
            //f.Get<PlayerLink> �� �ϸ� �д� �͸� �����ϴ�.
            //������ TryGetPointer�� �ð����⵵�� 1�̴�. ������� ECS ��Ű���ı� ������
            //TODO ������� ECS ��Ű���� ����
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

            //��Ʈ�ڽ� ũ��
            

            //TODO �ȱ� �ӵ� ���߿� �ۿ��� ���� �� �� �ְ� ������
            FP walkSpeed = FP._0_50; ;

            //�ι�° �÷��̾��� ��� ����
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            //�ƹ� �Է� ���� �� ������ ����
            filter.Body->Velocity = FPVector2.Zero;

            //ȸ�� ����
            filter.Transform->Rotation = FP._0;

            if (input->Down || (input->Down && input->Left) || (input->Down && input->Right))
            {
                //�ɱ� ����
                //�ɾ��մ� �ݶ��̴���
                if (filter.LSDF_Player->isSit == false)
                {
                    
                    AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", true);
                    //�˿��� �¿��� �κп� �־���ϴµ� �ȵǴ� ��
                    filter.LSDF_Player->isDashFront = false;
                    filter.LSDF_Player->isDashBack = false;

                    AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", false);
                    AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", false);
                    //�������
                    filter.LSDF_Player->isSit = true;
                }
            }
            else
            {
                //���� ����
                //���ִ� �ݶ��̴��� 
                AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", false);
                filter.LSDF_Player->isSit = false;
            }

            //�浹 ũ�� ����
            CollisionControll(f,ref filter);

            //�ɾ��ִ� ���� �ٸ� ������ �Ұ���
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
            //ũ��
            FPVector2 standingColliderExtents = new FPVector2(FP._0_10, FP._0_33);
            FPVector2 sitColliderExtents = new FPVector2(FP._0_10, FP._0_25);

            //��ġ
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
            
            //���� ũ��� ��ġ
            var currentExtents = collider->Shape.Box.Extents;
            var currentCenter = collider->Shape.LocalTransform;

            //�ɱ�,���⿡ ���� ũ��� ��ġ ����
            FPVector2 targetExtents = filter.LSDF_Player->isSit ? sitColliderExtents : standingColliderExtents;
            Transform2D targetCenter = filter.LSDF_Player->isSit ? crouchingColliderCenter : standingColliderCenter;

            //����� ������ �ٲ��� ����
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

            //�ι�° �÷��̾��� ��� ����
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            var buffer = f.Unsafe.GetPointer<DashInputBuffer>(filter.Entity);

            DirectionType dir = DirectionType.None;

            if (input->Left) dir = DirectionType.Left;
            else if (input->Right) dir = DirectionType.Right;

            bool isPressed = dir != DirectionType.None;

            if (isPressed && !buffer->LastInputPressed)
            {
                var now = f.Number;

                // ���� ������ �� �� ���Ȱ�, �Է� �� ������ DashInputWindow �̳��� ���
                if (buffer->LastDirection == dir && (now - buffer->LastInputTick) <= buffer->DashInputWindow)
                {

                    // ���⿡ �뽬 ���� �����̳� �ִϸ��̼� Ʈ���� �ֱ�
                    if (dir == DirectionType.Right)
                    {
                        
                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", true);
                    }
                    else
                    {
                        
                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", true);
                    }

                }

                // ����
                buffer->PrevDirection = buffer->LastDirection;
                buffer->LastDirection = dir;
                buffer->LastInputTick = now;
            }

            buffer->LastInputPressed = isPressed;

        }
    }
}
