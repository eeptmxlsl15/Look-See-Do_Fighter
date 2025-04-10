using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;

[Serializable]
public class JapWindowEvent : AnimatorTimeWindowEventAsset
{
    private const int TotalFrameCount = 31;
    private const int HitFrame = 9; // 0���� �����ϴϱ� 9�� 10��° ������

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

            // ��Ʈ�ڽ� ���� ���� (Entity ������ �װ� ������ ��� Ŀ���͸���¡ �ʿ�)
            EntityRef hitbox = f.Create(); // �Ǵ� �����տ��� Create

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_25/2, (FP._0_10 - FP._0_02)/2))
            });

            //2p�� ��� flip
            if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
            


            f.Set(hitbox, new Transform2D
            {
                //��ġ
                Position = f.Get<Transform2D>(entity).Position +  new FPVector2(FP._0_25, FP._0_25)* flip,
                Rotation = FP._0
            });
            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            //Debug.Log($"�� ��Ʈ�ڽ� ������ ������{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        
        player->isAttack = false;
        
    }
}