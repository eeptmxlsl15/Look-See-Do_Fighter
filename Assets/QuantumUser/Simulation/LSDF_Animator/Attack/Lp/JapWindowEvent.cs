using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class JapWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    private const int HitFrame = 10; // ���� �ߵ� �����Ӻ��� 1 ���ƾ� ���� �����ӿ� ��Ʈ �ڽ��� ���� �ȴ� = �̺�Ʈ ���� ��ġ������ -1
    

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        Debug.Log($"���� ���� ������{f.Number}");
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
       
        player->isAttack = true;
        player->isDashFront = false;
        player->isDashBack = false;
        player->canCounter = true;
        //���� �ڼ� ����
        player->isSit = false;


        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        
    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);

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

        Debug.Log("�� Execute");
        //��Ʈ �ڽ� ����
        if (currentFrame == HitFrame-1)//��Ʈ �ڽ� ���� ������ �� ������ ���� �����Ǿ����
        {

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new PhysicsCollider2D
            {

                IsTrigger = true,
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_20/2, (FP._0_10 - FP._0_02)/2))
            });

            //���� ����
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                startFrame = HitFrame,
                AttackerEntity = entity,
                AttackType = HitboxAttackType.High,
                CountType=CountAttackType.Normal,
                DelayGuardTpye=DelayGuardType.Normal,
                enemyGuardTime = 21,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 7,
            });

            f.Set(hitbox, new Transform2D
            {
                //��ġ
                Position = f.Get<Transform2D>(entity).Position +  new FPVector2((FP._0_25+FP._0_05)*flip, FP._0_25),
                Rotation = FP._0
            });

            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            if(player->canCounter == true)
            {
                player->canCounter = false;
            }
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