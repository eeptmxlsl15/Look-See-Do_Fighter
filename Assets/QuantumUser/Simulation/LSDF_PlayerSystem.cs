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
            //Unsate.TryGetPointer는 값을 읽고 수정도 가능하다
            //f.Get<PlayerLink> 로 하면 읽는 것만 가능하다.
            //퀀텀은 TryGetPointer의 시간복잡도가 1이다. 희소집합 ECS 아키텍쳐기 때문에
            //TODO 희소집합 ECS 아키텍쳐 공부
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                input = f.GetPlayerInput(playerLink->PlayerRef);
            }

            UpdateMovement(f, ref filter, input);

        }
        private void UpdateMovement(Frame f, ref Filter filter, Input* input)
        {
            //TODO 나중에 밖에서 설정 할 수 있게 빼야함
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
