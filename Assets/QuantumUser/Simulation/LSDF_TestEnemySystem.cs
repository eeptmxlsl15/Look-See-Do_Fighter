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

            //// ��: 60������ �������� �� �� ����
            //bool shouldAttack = tick % 60 == 0;

            //// ��: �׻� ������ �ȴ� ���� ����
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