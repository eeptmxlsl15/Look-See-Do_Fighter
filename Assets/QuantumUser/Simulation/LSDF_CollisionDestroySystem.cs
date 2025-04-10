using UnityEngine;
using UnityEngine.Scripting;
using static UnityEngine.EventSystems.EventTrigger;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionDestroySystem : SystemMainThreadFilter<LSDF_CollisionDestroySystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public TickToDestroy* TickToDestroy;
        }
        public override void Update(Frame f, ref Filter filter)
        {
            if (f.Number >= filter.TickToDestroy->TickToDestroyAt)
            {
                f.Destroy(filter.Entity);
                // 디버깅용 로그 (필요 시)
                 //Debug.Log($"[TickToDestroy] Entity {filter.Entity} destroyed at tick {f.Number}");
            }
        }
    }
}
