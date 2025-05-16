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
        //LSDF_Player 컴포넌트의 상태와
        //실제 애니메이션 상태도 고려해야함
        //엔터 시엔 PlayerSystem에서 애니메이션 true로 해주기때문에 안해도됨

        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashFront = true;
        }

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;

        //전진성
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
        body->Velocity.X = 2 * flip;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //엔티티 가져와서 LSDF에 상태 변환
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashFront = false;
        }

        //
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);

    }
}
