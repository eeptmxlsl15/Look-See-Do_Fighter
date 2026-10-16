using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class SitPunchEvent : AnimatorTimeWindowEventAsset
{

    public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFRemainingAttackDefaults.SitPunch(); // 실제 발동 프레임보다 1 낮아야 다음 프레임에 히트 박스가 적용 된다 = 이벤트 시작 위치때문에 -1

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 필수 요소
        Debug.Log($"원잽 시작 프레임{f.Number}");

        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        player->isAttack = true;
        player->canCounter = true;

        player->isDashFront = false;
        player->isDashBack = false;
        player->isSit = false;
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);

        #endregion

        player->isDodgeHigh = true;
        player->isJump = false;
        Debug.Log($"잽 현재 프레임 : {Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time)}");

    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region 수정 안해도됨
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;

        int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);

        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        var input = f.GetPlayerInput(playerLink.PlayerRef);

        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
        #endregion

        if (0<=animationFrame && animationFrame< Hit.StartupFrames-1)
        {
            player->isDodgeHigh = true;
        }

        if (animationFrame == Hit.StartupFrames - 1)//히트 박스 적용 때문에 한 프레임 전에 생성되어야함
        {

            EntityRef hitbox = f.Create();

            player->isJump = false;
            player->isDodgeHigh = false;

            Quantum.LSDF.LSDFHitboxFactory.ConfigureBody(f, hitbox, entity, flip, Hit);

            Quantum.LSDF.LSDFHitboxFactory.AddPayload(f, hitbox, entity, Hit);

            Quantum.LSDF.LSDFHitboxFactory.ScheduleDestroy(f, hitbox);

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"잽 히트박스 생성된 프레임{f.Number}");
        }
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;
        player->isJump = false;
        player->isDodgeHigh = false;
        Debug.Log($"원잽 끝 프레임 : {f.Number}");
    }
}