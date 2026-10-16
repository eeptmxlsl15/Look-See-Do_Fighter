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
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        player->isDashBack = false;

        

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        //var input = f.GetPlayerInput(playerLink.PlayerRef);


        //¹æÇâ
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        body->Velocity.X = Quantum.LSDF.LSDFBalance.WalkSpeed(f) * flip;

        player->isDashBack = false;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
       
    }
}
