using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using UnityEngine.Windows;

[Serializable]
public class ComboWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {

        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //player->isAttack = true;
        //player->isDashFront = false;
        //player->isDashBack = false;
        //player->canCounter = true;

        //앉은 자세 여부
        player->isSit = false;
        player->isAttack = false;
        Debug.Log("콤보 휘청 시작");

        player->isCombo = true;
        //상하단 회피 판정 초기화
        player->isDodgeHigh = false;
        player->isJump = false;
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //var entity = animatorComponent->Self;

        //// PlayerRef 가져오기
        //if (!f.Unsafe.TryGetPointer<PlayerLink>(entity, out var playerLink))
        //    return;

        //var input = f.GetPlayerInput(playerLink->PlayerRef);
        //if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        //    return;

        //// 입력에 따라 isSit 제어
        //if (input->Down)
        //{
        //    player->isSit = true;
        //}
        //else
        //{
        //    player->isSit = false;
        //}
        //Debug.Log($"앉기 여부 : {player->isSit}");

    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        //앉은 자세 여부
        //player->isSit = false;
        Debug.Log("콤보 휘청 끝");
        player->isCombo = false;
    }
}
