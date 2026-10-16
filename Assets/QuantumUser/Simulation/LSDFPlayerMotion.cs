using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
  public static unsafe class LSDFPlayerMotion {
        public static void UpdateMovement(Frame f, ref LSDF_PlayerSystem.Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);
            var playerState = f.Get<LSDF_Player>(filter.Entity);

            filter.Body->Velocity = FPVector2.Zero;

            if (input->Up)
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "Parring", true);
                return;
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "Parring", false);
            }
            if (input->Down || (input->Down && input->Left) || (input->Down && input->Right))
            {

                if (filter.LSDF_Player->isSit == false)
                {

                    AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", true);

                    filter.LSDF_Player->isSit = true;
                }
            }
            else
            {

                AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", false);
                filter.LSDF_Player->isSit = false;
            }

            if (filter.LSDF_Player->isSit == true || filter.LSDF_Player->isHit || filter.LSDF_Player->isGuard) return;

            if (input->Left && input->Right)
            {

                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);
                return;
            }

            if (input->Left && playerState.isDashBack == false)
            {

                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", true);
            }
            else
            {

                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
            }

            if (input->Right && playerState.isDashFront == false)
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);

            }

        }

        public static void UpdateCollider(Frame f, ref LSDF_PlayerSystem.Filter filter)
        {
            FPVector2 standingColliderExtents = new FPVector2(FP._0_20-FP._0_02, FP._0_33);
            FPVector2 sitColliderExtents = new FPVector2(FP._0_20 - FP._0_02, FP._0_25);

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

            if (!f.Unsafe.TryGetPointer<PhysicsCollider2D>(filter.Entity, out var collider)) return;

            var currentExtents = collider->Shape.Box.Extents;
            var currentCenter = collider->Shape.LocalTransform;

            FPVector2 targetExtents = filter.LSDF_Player->isSit ? sitColliderExtents : standingColliderExtents;
            Transform2D targetCenter = filter.LSDF_Player->isSit ? crouchingColliderCenter : standingColliderCenter;

            if (!currentExtents.Equals(targetExtents))
            {
                collider->Shape.Box.Extents = targetExtents;
                collider->Shape.LocalTransform = targetCenter;
            }
        }

        public static void DetectDashCommand(Frame f, ref LSDF_PlayerSystem.Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);

            if (!f.Unsafe.TryGetPointer<DashInputBuffer>(filter.Entity, out var buffer)) return;

            DirectionType dir = DirectionType.None;

            if (input->Left) dir = DirectionType.Left;
            else if (input->Right) dir = DirectionType.Right;

            bool isPressed = dir != DirectionType.None;

            if (isPressed && !buffer->LastInputPressed)
            {
                var now = f.Number;

                if (buffer->LastDirection == dir && (now - buffer->LastInputTick) <= buffer->DashInputWindow)
                {

                    if (dir == DirectionType.Right)
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", true);
                    }
                    else
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", true);
                    }

                }

                buffer->PrevDirection = buffer->LastDirection;
                buffer->LastDirection = dir;
                buffer->LastInputTick = now;
            }

            buffer->LastInputPressed = isPressed;

        }

  }
}
