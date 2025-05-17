using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using Photon.Deterministic;

[Serializable]
public class MoveFrontWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);

        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        player->isDashBack = false;

        

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body);
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        //var input = f.GetPlayerInput(playerLink.PlayerRef);


        //¹æÇâ
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        body->Velocity.X = FP._0_50 * flip;

        player->isDashBack = false;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
       
    }
}
