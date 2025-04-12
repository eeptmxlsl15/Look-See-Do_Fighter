using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class JapWindowEvent : AnimatorTimeWindowEventAsset
{
    private const int TotalFrameCount = 30;
    private const int HitFrame = 9; // ���� �ߵ� �����Ӻ��� 1 ���ƾ� ���� �����ӿ� ��Ʈ �ڽ��� ���� �ȴ�

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"���� ���� ������{f.Number}");
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
        var entity = animatorComponent->Self;
        int currentFrame = (int)((layerData->Time / layerData->Length) * TotalFrameCount);

        //�������� ���⼺
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation=true;
       
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //����
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;
        
        //���� �ӵ�
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
        }


        //��Ʈ �ڽ� ����
        if (currentFrame == HitFrame)
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_25/2, (FP._0_10 - FP._0_02)/2))
            });

            //���� ����
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                AttackType= HitboxAttackType.High,
                
                enemyGuardTime = 21,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 7,
            });

            f.Set(hitbox, new Transform2D
            {
                //��ġ
                Position = f.Get<Transform2D>(entity).Position +  new FPVector2(FP._0_25*flip, FP._0_25),
                Rotation = FP._0
            });

            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            Debug.Log($"�� ��Ʈ�ڽ� ������ ������{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        
        player->isAttack = false;

        Debug.Log($"���� �� ������ : {f.Number}");
    }
}