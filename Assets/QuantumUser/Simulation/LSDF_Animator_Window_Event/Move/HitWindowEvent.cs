using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using UnityEngine.Windows;
using Photon.Deterministic;

[Serializable]
public class HitWindowEvent : AnimatorTimeWindowEventAsset
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
        player->isParring = false;
        
        Debug.Log("�� ����");

        //���ϴ� ȸ�� ���� �ʱ�ȭ
        player->isDodgeHigh = false;
        player->isJump = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //�÷��̾�
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //ȸ�� ����, �ٵ�
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        //�÷��̾� ��ũ
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //����
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? -1 : 1;

        //��Ʈ,���� ��
        body->Velocity.X = player->forceBack * flip;

        //�÷��̾� ���� ���ϰ� �ؾ���
        player->isSit = false;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
    }
}
