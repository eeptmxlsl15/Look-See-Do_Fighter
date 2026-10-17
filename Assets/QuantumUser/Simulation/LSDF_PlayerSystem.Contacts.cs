using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
    public unsafe partial class LSDF_PlayerSystem {
        #region 바닥 관련
        public void OnCollisionGroundEnter(Frame f, CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Ground* ground)
        {
            player->isGround = true;
            AnimatorComponent.SetBoolean(f, animator, "Ground", true);
            Debug.Log($"isGround 온: {player->isGround}");
        }

        public void OnCollisionGroundExit(Frame f, ExitInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Ground* ground)
        {
            player->isGround = false;
            AnimatorComponent.SetBoolean(f, animator, "Ground", false);
            Debug.Log($"isGround 오프 : {player->isGround}");
        }
        #endregion

        #region 벽 관련

        //단순히 벽에 붙어 있는지 아닌지만 판별하는 시그널
        public void OnCollisionEnterWall(Frame f,CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall)
        {

            player->isOnWall = true;

            //공콤 도중에 닿으면
            //if (player->isAir)
            //{
            //    AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
            //}
            Debug.Log($"벽 isOnWall {player->isOnWall}");
        }
        public void OnCollisionExitWall(Frame f, ExitInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall)
        {

            player->isOnWall = false;

            Debug.Log($"벽 isOnWall {player->isOnWall}");
        }
        public void OnCollisionWallHitEnter(Frame f, CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall)
        {
            if (LSDFJuggle.BlocksHit(f, info.Other)) return;
            LSDFJuggle.Cancel(f, info.Other, preserveCombo: true);
            if (player->isAir)
            {
                AnimatorComponent.SetTrigger(f, animator, "FirstWallHit");
            }
            else
                AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
        }

        #endregion
    }
}
