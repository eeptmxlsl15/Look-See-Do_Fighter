using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe partial class LSDF_PlayerSystem : SystemMainThreadFilter<LSDF_PlayerSystem.Filter>,
        ISignalOnTriggerNormalHit, ISignalOnTriggerCounterHit , ISignalOnTriggerLauncherHit,
        ISignalOnTriggerEnemyGuard , ISignalOnTriggerGuard,
        ISignalOnTriggerEnemyParring  ,
        ISignalOnCollisionGroundEnter,ISignalOnCollisionGroundExit,
        ISignalOnCollisionEnterWall,ISignalOnCollisionExitWall  , ISignalOnCollisionWallHitEnter
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
            Input neutralInput = default;
            Input* input = &neutralInput;
            LSDFPlayerMotion.UpdateCollider(f, ref filter);
            var player = filter.LSDF_Player;

            filter.Transform->Rotation = FP._0;

            if(filter.Body->Velocity.Y==0 && player->isAir == true)
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "YZero",true);
                Debug.Log("와이제로");
            }
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                var polledInput = f.GetPlayerInput(playerLink->PlayerRef);
                if (polledInput != null) input = polledInput;

            }
            LSDFPlayerHealth.UpdateRecovery(ref *player, LSDFBalance.MaximumHealth(f), LSDFBalance.Healing(f));
            if (f.Number < filter.LSDF_Player->tickToEnableMove)
            {
                if(filter.LSDF_Player->tickToEnableMove-f.Number == LSDFBalance.HealingLead(f))
                {
                    player->isHealing = true;
                }

                return;
            }
            if (player->isAttack == true || player->isStun == true || player->isCombo == true || player->isAir || player->isWallHit || player->isCantMove)
            {

                return;
            }

            if (filter.LSDF_Player->isGuard && f.Number >= filter.LSDF_Player->DelayFrame)
            {

                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle로 전이하는 트리거
                Debug.Log($"가드 상태 끝 프레임 : {f.Number}");

                filter.LSDF_Player->isGuard = false;
            }

            if (filter.LSDF_Player->isHit && f.Number >= filter.LSDF_Player->DelayFrame)
            {

                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle로 전이하는 트리거
                Debug.Log($"히트 상태 끝 프레임 : {f.Number}");

                filter.LSDF_Player->hitWallLauncher = false;

                filter.LSDF_Player->isHit = false;
            }

            if (!input->Up)
            {
                player->isParring = false;
            }
            if (input->LeftPunch || input->RightPunch || input->LeftKick || input->RightKick)
            {
                player->isParring = false;
            }

            LSDFPlayerMotion.DetectDashCommand(f, ref filter, input);
            LSDFPlayerMotion.UpdateMovement(f, ref filter, input);

        }

    }

}
