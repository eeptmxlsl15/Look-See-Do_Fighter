using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CameraSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            //EntityRef player1 = EntityRef.None;
            //EntityRef player2 = EntityRef.None;

            //Transform2D* t1 = null;
            //Transform2D* t2 = null;

            //// 플레이어 위치 찾기
            //var playerFilter = f.Filter<Transform2D, LSDF_Player>();
            //while (playerFilter.NextUnsafe(out var entity, out var transform, out var player))
            //{
                
            //    var link = f.Get<PlayerLink>(entity);
            //    if (link.PlayerRef == (PlayerRef)0)
            //    {
                    
            //        player1 = entity;
            //        t1 = transform;
            //        Debug.Log($"1p 위치 {t1->Position}");
            //    }
            //    else if (link.PlayerRef == (PlayerRef)1)
            //    {
                    
            //        player2 = entity;
            //        t2 = transform;
            //        t1 = transform;
            //        Debug.Log($"2p 위치 {t2->Position}");
            //    }
            //}

            //if (t1 == null || t2 == null)
            //    return;

            //// 중앙 계산
            //var center = (t1->Position + t2->Position) * FP._0_50;

            //// 카메라 이동
            //var cameraFilter = f.Filter<Transform3D, LSDF_CameraInfo>();
            
            //while (cameraFilter.NextUnsafe(out var entity, out var transform, out var _))
            //{
            //    Debug.Log("카메라");
            //    transform->Position.X = center.X;
            //    transform->Position.Y = center.Y;
            //    // Z축은 유지
            //}
        }
    }
}