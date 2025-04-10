using UnityEngine.Scripting;
using UnityEngine;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D
    {
        public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
        {
            Debug.Log("1");
            //플레이어가 맞았을때
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var player))
            {
                Debug.Log("2");
                //기준(플레이어) 말고 다른게 공격판정일 때
                if (f.Unsafe.TryGetPointer<TickToDestroy>(info.Other, out var hitbox))
                {
                    Debug.Log("3");
                    //카운터 일때
                    //상단
                    //중단
                    //하단

                    //노멀히트 일때
                    //상단
                    //중단
                    //하단

                    //가드 됐을때
                    f.Signals.OnTriggerNormalHit(info,player, hitbox);
                    //상단
                    //중단
                    //하단
                }
            }
        }
    }
}