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
            
            //플레이어가 맞았을때
            if (f.Unsafe.TryGetPointer<LSDF_Player>(info.Entity, out var player))
            {


                //기준(플레이어) 말고 다른게 공격판정일 때
                if (f.Unsafe.TryGetPointer<LSDF_HitboxInfo>(info.Other, out var hitbox))
                {
                    //애니메이터 가져오기
                    f.Unsafe.TryGetPointer<AnimatorComponent>(info.Entity, out var animator);


                    //애니메이터 그래프 찾아와서 첫번째 레이어에서 
                    var animatorGraph = f.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
                    var layerDataList = f.ResolveList<LayerData>(animator->Layers);

                    //현재 상태 아이디 가져옴
                    var currentLayer = layerDataList.GetPointer(0); // 첫 번째 레이어 기준
                    var currentStateId = currentLayer->CurrentStateId;

                    
                    var currentState = animatorGraph.GetState(currentStateId);

                    //스트링으로 변환
                    
                    string currentStateName = currentState.Name;


                    if (player->canCounter)
                    {
                       
                        f.Signals.OnTriggerCounterHit(info, player, animator, hitbox);
                        return;
                    }


                
                    //상단 공격
                    //
                    //맞는 경우
                    //MoveFront,DashFront,기술 입력 후 발동 전(카운터),기술 발동 후 끝나기 전(딜캐, 헛친거)
                    //
                    //안맞는 경우
                    //Idle,MoveBack,DashBack

                    //중단 공격
                    //
                    //맞는 경우
                    //상단과 동일 + 앉아있을 때
                    //
                    //안맞는 경우
                    //상단과 동일

                    //하단 공격
                    //
                    //맞는경우
                    //앉지 않았을 때
                    //
                    //막는 경우
                    //앉아 있을 때

                    switch (hitbox->AttackType)
                    {
                        

                        case HitboxAttackType.High:
                            //상단 공격 히트
                            if (currentStateName == "Move Front" || currentStateName == "Dash Front")//카운터,딜캐 추가
                            {
                                f.Signals.OnTriggerNormalHit(info, player, animator, hitbox);
                            }
                            //상단 공격 가드
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
                            if (currentStateName == "Move Front" || currentStateName == "Dash Front"|| currentStateName =="Sit Enter"|| currentStateName =="Siting")//카운터,딜캐 추가
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
                            //여긴 히트,가드가 반대
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

                    //카운터 일때
                    //상단
                    //중단
                    //하단

                    //노멀히트 일때
                    //f.Signals.OnTriggerNormalHit(info,player, hitbox);
                    //상단
                    //중단
                    //하단

                    //가드 됐을때
                    //상단
                    //중단
                    //하단
                }
            }
        }
    }
}