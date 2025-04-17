using UnityEngine.Scripting;
using UnityEngine;
using Quantum.Collections;
using static UnityEngine.EventSystems.EventTrigger;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CollisionSystem : SystemSignalsOnly, ISignalOnTriggerEnter2D
    {
        public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
        {
            
            //�÷��̾ �¾�����
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var player))
            {


                //����(�÷��̾�) ���� �ٸ��� ���������� ��
                if (f.Unsafe.TryGetPointer<LSDF_HitboxInfo>(info.Other, out var hitbox))
                {
                    //�ִϸ����� ��������
                    f.Unsafe.TryGetPointer<AnimatorComponent>(info.Entity, out var animator);


                    //�ִϸ����� �׷��� ã�ƿͼ� ù��° ���̾�� 
                    var animatorGraph = f.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
                    var layerDataList = f.ResolveList<LayerData>(animator->Layers);

                    //���� ���� ���̵� ������
                    var currentLayer = layerDataList.GetPointer(0); // ù ��° ���̾� ����
                    var currentStateId = currentLayer->CurrentStateId;

                    
                    var currentState = animatorGraph.GetState(currentStateId);

                    //��Ʈ������ ��ȯ
                    
                    string currentStateName = currentState.Name;


                    if (player->canCounter)
                    {
                       
                        f.Signals.OnTriggerCounterHit(info, player, animator, hitbox);
                        return;
                    }


                
                    //��� ����
                    //
                    //�´� ���
                    //MoveFront,DashFront,��� �Է� �� �ߵ� ��(ī����),��� �ߵ� �� ������ ��(��ĳ, ��ģ��)
                    //
                    //�ȸ´� ���
                    //Idle,MoveBack,DashBack

                    //�ߴ� ����
                    //
                    //�´� ���
                    //��ܰ� ���� + �ɾ����� ��
                    //
                    //�ȸ´� ���
                    //��ܰ� ����

                    //�ϴ� ����
                    //
                    //�´°��
                    //���� �ʾ��� ��
                    //
                    //���� ���
                    //�ɾ� ���� ��

                    switch (hitbox->AttackType)
                    {
                        

                        case HitboxAttackType.High:
                            //��� ���� ��Ʈ
                            if (currentStateName == "Move Front" || currentStateName == "Dash Front")//ī����,��ĳ �߰�
                            {
                                f.Signals.OnTriggerNormalHit(info, player, animator, hitbox);
                            }
                            //��� ���� ����
                            else if (currentStateName == "Move Back" || currentStateName== "Dash Back" || currentStateName =="Idle" )
                            {
                                if (hitbox->DelayGuardTpye != DelayGuardType.Normal)
                                {
                                    f.Signals.OnTriggerEnemyGuard(info, player, animator, hitbox);
                                }
                                else
                                    f.Signals.OnTriggerGuard(info, player,animator, hitbox);
                                //AnimatorComponent.SetTrigger(f, animator, "StandGuard");
                            }
                            
                            break;

                        case HitboxAttackType.Mid:
                            if (currentStateName == "Move Front" || currentStateName == "Dash Front"|| currentStateName =="Sit Enter"|| currentStateName =="Siting")//ī����,��ĳ �߰�
                            {
                                f.Signals.OnTriggerNormalHit(info, player, animator, hitbox);
                            }
                            else if (currentStateName == "Move Back" || currentStateName == "Dash Back" || currentStateName == "Idle")
                            {
                                if (hitbox->DelayGuardTpye != DelayGuardType.Normal)
                                {
                                    f.Signals.OnTriggerEnemyGuard(info, player, animator, hitbox);
                                }
                                else
                                    f.Signals.OnTriggerGuard(info, player, animator, hitbox);
                            }

                            break;

                        case HitboxAttackType.Low:
                            //���� ��Ʈ,���尡 �ݴ�
                            if(currentStateName == "Sit Enter" || currentStateName == "Siting")
                            {
                                if (hitbox->DelayGuardTpye != DelayGuardType.Normal)
                                {
                                    f.Signals.OnTriggerEnemyGuard(info, player, animator, hitbox);
                                }
                                else
                                    f.Signals.OnTriggerGuard(info, player, animator, hitbox);
                            }
                            else
                            {
                                f.Signals.OnTriggerNormalHit(info, player, animator, hitbox);
                            }
                            break;

                    }

                    //ī���� �϶�
                    //���
                    //�ߴ�
                    //�ϴ�

                    //�����Ʈ �϶�
                    //f.Signals.OnTriggerNormalHit(info,player, hitbox);
                    //���
                    //�ߴ�
                    //�ϴ�

                    //���� ������
                    //���
                    //�ߴ�
                    //�ϴ�
                }
            }
        }
    }
}