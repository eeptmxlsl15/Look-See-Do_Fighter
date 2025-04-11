using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;

[Serializable]
public class JapWindowEvent : AnimatorTimeWindowEventAsset
{
    private const int TotalFrameCount = 31;
    private const int HitFrame = 9; // 0부터 시작하니까 9가 10번째 프레임

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
       
        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;

        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        int currentFrame = (int)((layerData->Time / layerData->Length) * TotalFrameCount);

        if (currentFrame == HitFrame)
        {
            var entity = animatorComponent->Self;

            
            EntityRef hitbox = f.Create();

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //박스 크기
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_25/2, (FP._0_10 - FP._0_02)/2))
            });

            //공격 정보
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                AttackType= HitboxAttackType.High,
                
                enemyGuardTime = 0,
                enemyHitTime = 0,
                attackDamage = 0,
            });


            //2p일 경우 flip
            if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
            


            f.Set(hitbox, new Transform2D
            {
                //위치
                Position = f.Get<Transform2D>(entity).Position +  new FPVector2(FP._0_25*flip, FP._0_25),
                Rotation = FP._0
            });

            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            //Debug.Log($"잽 히트박스 생성된 프레임{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        
        player->isAttack = false;
        
    }
}