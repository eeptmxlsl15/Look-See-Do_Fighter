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
            UpdateCollider(f, filter.Entity, IsGettingUp(f, filter.Entity));
        }

        public static bool IsGettingUp(Frame f, EntityRef entity)
        {
            if (!f.TryGet<AnimatorComponent>(entity, out var animator) || !animator.AnimatorGraph.Id.IsValid ||
                animator.Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) return false;
            var graph = f.FindAsset<AnimatorGraph>(animator.AnimatorGraph);
            var layers = f.ResolveList(animator.Layers);
            return graph != null && layers.Count > 0 && graph.GetState(layers.GetPointer(0)->CurrentStateId)?.Name == "Rising";
        }

        // Entry/exit events may supply the new pose before the graph finishes its transition.
        public static void UpdateCollider(Frame f, EntityRef entity, bool gettingUp)
        {
            if (!f.Unsafe.TryGetPointer<PhysicsCollider2D>(entity, out var collider) ||
                !f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
            f.TryGet<LSDF_AirCombo>(entity, out var air);
            UpdateCollider(ref *collider, *player, air, LSDFBalance.Config(f)?.AirCombo, gettingUp);
        }

        public static void UpdateCollider(ref PhysicsCollider2D collider, in LSDF_Player player,
            in LSDF_AirCombo air, LSDFJuggleSettings settings, bool gettingUp = false)
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

            var currentExtents = collider.Shape.Box.Extents;
            var currentCenter = collider.Shape.LocalTransform;

            FPVector2 targetExtents = player.isSit ? sitColliderExtents : standingColliderExtents;
            Transform2D targetCenter = player.isSit ? crouchingColliderCenter : standingColliderCenter;
            // Juggle transitions first. Ground-contact fluctuations must not alternate
            // the collider center while the phase still owns the terminal flight.
            bool flying = air.Phase == LSDFAirComboPhase.BlownAway && !player.isRoundEnd && player.playerHp > 0;
            if (flying && settings != null) targetCenter.Position = settings.EnderColliderCenter;
            else if (gettingUp && !player.isAir && !player.isRoundEnd && player.playerHp > 0 &&
                !LSDFJuggle.BlocksHit(air) && settings != null) targetCenter.Position = settings.GetUpColliderCenter;

            if (!currentExtents.Equals(targetExtents) || !currentCenter.Position.Equals(targetCenter.Position) ||
                currentCenter.Rotation != targetCenter.Rotation)
            {
                collider.Shape.Box.Extents = targetExtents;
                collider.Shape.LocalTransform = targetCenter;
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
