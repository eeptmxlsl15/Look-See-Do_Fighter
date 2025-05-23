using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using UnityEngine.Windows;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class StunWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        
        //player->isAttack = true;
        //player->isDashFront = false;
        //player->isDashBack = false;
        //player->canCounter = true;

        //���� �ڼ� ����
        player->isSit = false;
        player->isAttack = false;
        Debug.Log("���� ����");

        player->isStun = true;

        //���ϴ� ȸ�� ���� �ʱ�ȭ
        player->isDodgeHigh = false;
        player->isJump = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

        //���� �� ���� ���°� ������ ���� ���� ���ͼ� 
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);
        body->Velocity.X = 0;
    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //var entity = animatorComponent->Self;

        //// PlayerRef ��������
        //if (!f.Unsafe.TryGetPointer<PlayerLink>(entity, out var playerLink))
        //    return;

        //var input = f.GetPlayerInput(playerLink->PlayerRef);
        //if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        //    return;

        //// �Է¿� ���� isSit ����
        //if (input->Down)
        //{
        //    player->isSit = true;
        //}
        //else
        //{
        //    player->isSit = false;
        //}
        //Debug.Log($"�ɱ� ���� : {player->isSit}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);
        body->Velocity.X = 0;

    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        //���� �ڼ� ����
        //player->isSit = false;
        Debug.Log("���� ��");
        player->isStun = false;
    }
}
