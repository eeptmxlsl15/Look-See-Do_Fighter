using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using Photon.Deterministic;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class DashFrontWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //LSDF_Player ������Ʈ�� ���¿�
        //���� �ִϸ��̼� ���µ� ����ؾ���
        //���� �ÿ� PlayerSystem���� �ִϸ��̼� true�� ���ֱ⶧���� ���ص���

        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashFront = true;
        }

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;

        //������
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
        body->Velocity.X = 2 * flip;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //��ƼƼ �����ͼ� LSDF�� ���� ��ȯ
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashFront = false;
        }

        //
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);

    }
}
