using Photon.Deterministic;
using Quantum;
using UnityEngine;
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

            UpdateMovement(f, ref filter, input);

        }
        private void UpdateMovement(Frame f, ref Filter filter, Input* input)
        {
            //TODO ���߿� �ۿ��� ���� �� �� �ְ� ������
            FP walkSpeed = FP._0_50; ;
            var player = filter.LSDF_Player;

            filter.Body->Velocity = FPVector2.Zero;
            //filter.Body->AddForce(filter.Transform->Up);
            //var config = f.FindAsset(filter.AsteroidsShip->ShipConfig);
            //FP shipAcceleration = config.ShipAceleration;
            //FP turnSpeed = config.ShipTurnSpeed;

            if (input->Up)
            {
                //filter.Body->AddForce(filter.Transform->Up * 8);
                Debug.Log("Parry");
            }
            
            if (input->Left)
            {
                filter.Body->Velocity.X = -walkSpeed;
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
            }

            if (input->Right)
            {
                filter.Body->Velocity.X = walkSpeed;
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);

                //filter.Body->AngularVelocity = FPMath.Clamp(filter.Body->AngularVelocity, -8, 8);
            }
        }
    }
}
