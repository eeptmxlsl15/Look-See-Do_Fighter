using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class LawRightUpperWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    private const int HitFrame = 15; 


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
        body->FreezeRotation = true;

        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;

        //����
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //���� �ӵ�
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
        }

        //Debug.Log("�� Execute");
        //��Ʈ �ڽ� ����
        if (currentFrame == HitFrame - 1)//��Ʈ �ڽ� ���� ������ �� ������ ���� �����Ǿ����
        {

            EntityRef hitbox = f.Create();

            //-------------------------------------------���-----------------------------------------//
            //FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25 + FP._0_05) * flip, FP._0_25);

            //f.Add(hitbox, new Transform2D
            //{
            //    //��ġ
            //    Position = hitboxPosition,
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{

            //    IsTrigger = true,
            //    //�ڽ� ũ��
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2))
            //});

            ////-------------------------------------------�ߴ�-----------------------------------------//
            FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25) * flip, 0);
            f.Add(hitbox, new Transform2D
            {
                //Change
                //��ġ
                Position = hitboxPosition,
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //Change
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2))
            });

            //---------------------------------------�ϴ�---------------------------------------------//
            //FPVector2 hitboxPosition=f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25) * flip, -(FP._0_25 + FP._0_03));

            //f.Add(hitbox, new Transform2D
            //{
            //    //��ġ
            //    Position =hitboxPosition,
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{
            //    IsTrigger = true,
            //    //�ڽ� ũ��
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_10 - FP._0_02) / 2))
            //});
            //------------------------------------------------------------------------------------//

            //���� ����
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                position = hitboxPosition,
                startFrame = HitFrame,
                AttackerEntity = entity,
                
                AttackType = HitboxAttackType.Mid,
                CountType = CountAttackType.Air,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Homing,

                launcher = true,
                notSitLauncher = true,
                jumpAttack = false,
                dodgeHigh = false,

                enemyGuardTime = 13,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 12,
            });



            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            if (player->canCounter == true)
            {
                player->canCounter = false;
            }
            Debug.Log($"���� ��Ʈ�ڽ� ������ ������{f.Number}");
        }
    }


    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;

        Debug.Log($"���� �� ������ : {f.Number}");
    }
}