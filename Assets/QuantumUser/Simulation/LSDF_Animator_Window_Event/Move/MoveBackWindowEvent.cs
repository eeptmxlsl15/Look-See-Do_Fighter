using Quantum;
using UnityEngine;
using System;
using Quantum.Addons.Animator;
using Photon.Deterministic;

[Serializable]
public class MoveBackWindowEvent : AnimatorTimeWindowEventAsset
{
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);

        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        player->isDashFront = false;

        Debug.Log("무브백 시작");
        

    }

    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        //var input = f.GetPlayerInput(playerLink.PlayerRef);


        //방향
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        body->Velocity.X = -Quantum.LSDF.LSDFBalance.WalkSpeed(f) * flip;

        player->isDashFront = false;
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log("뒤로 걷기 끝");
    }
}
