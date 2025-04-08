using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using Photon.Deterministic;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class DashBackWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashBack = true;
        }

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;

        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
        body->Velocity.X = -3 * flip;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player))
        {
            player->isDashBack = false;
        }

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

    }
}
