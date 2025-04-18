using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_TestEnemySystem : SystemMainThreadFilter<LSDF_TestEnemySystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public LSDF_TestEnemy TestEnemy;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            //var tick = f.Number;

            //// 예: 60프레임 간격으로 한 번 공격
            //bool shouldAttack = tick % 60 == 0;

            //// 예: 항상 앞으로 걷는 상태 유지
            //if (f.Unsafe.TryGetPointer<AnimatorComponent>(filter.Entity, out var animator))
            //{
            //    AnimatorComponent.SetBoolean(f, animator, "MoveFront", true);

            //    if (shouldAttack)
            //    {
            //        //AnimatorComponent.SetTrigger(f, animator, "RightPunch");
            //    }
            //}

        }
    }
}