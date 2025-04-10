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
            //�÷��̾ �¾�����
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var player))
            {
                Debug.Log("2");
                //����(�÷��̾�) ���� �ٸ��� ���������� ��
                if (f.Unsafe.TryGetPointer<TickToDestroy>(info.Other, out var hitbox))
                {
                    Debug.Log("3");
                    //ī���� �϶�
                    //���
                    //�ߴ�
                    //�ϴ�

                    //�����Ʈ �϶�
                    //���
                    //�ߴ�
                    //�ϴ�

                    //���� ������
                    f.Signals.OnTriggerNormalHit(info,player, hitbox);
                    //���
                    //�ߴ�
                    //�ϴ�
                }
            }
        }
    }
}