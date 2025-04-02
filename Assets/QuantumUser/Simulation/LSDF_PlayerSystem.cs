using Photon.Deterministic;
using Quantum.Asteroids;
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
        }

        public override void Update(Frame f, ref Filter filter)
        {
            //Input* input = default;
            //if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            //{
            //    input = f.GetPlayerInput(playerLink->PlayerRef);
            //}
            Input* input;
            input = f.GetPlayerInput(0);
            UpdateShipMovement(f, ref filter, input);
            
        }
        private void UpdateShipMovement(Frame f, ref Filter filter, Input* input)
        {
            //TODO 나중에 밖에서 설정 할 수 있게 빼야함
            FP walkSpeed = 1;

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
            }

            if (input->Right)
            {
                filter.Body->Velocity.X = walkSpeed;
            }
            
            //filter.Body->AngularVelocity = FPMath.Clamp(filter.Body->AngularVelocity, -8, 8);
        }
    }
}
