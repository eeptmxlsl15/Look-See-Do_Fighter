using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class ShaheenCutKickWindowEvent : AnimatorTimeWindowEventAsset
{

    private int currentFrame;
    private const int HitFrame = 15; // ���� �ߵ� �����Ӻ��� 1 ���ƾ� ���� �����ӿ� ��Ʈ �ڽ��� ���� �ȴ� = �̺�Ʈ ���� ��ġ������ -1
    bool bufferedNextAttack;

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region �ʼ� ��� 
        Debug.Log($"������ű ���� ������{f.Number}");

        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //������ ���� �ʱ�ȭ
        player->isAttack = true;
        player->canCounter = true;

        player->isDashFront = false;
        player->isDashBack = false;
        //���� �ڼ� ����
        player->isSit = false;
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "DashBack", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveFront", false);
        AnimatorComponent.SetBoolean(f, animatorComponent, "MoveBack", false);

        #endregion
        //---��Ÿ ��� ---//
        //���� �ؽ�Ʈ �ʱ�ȭ

        //���ȸ��,��������
        player->isJump = true;
        player->isDodgeHigh = true;


        bufferedNextAttack = false;
        Debug.Log($"�� ���� ������ : {currentFrame}");


    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        //�÷��̾� ����
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);

        //���� ������
        currentFrame = (int)(layerData->Time.AsFloat * 60.0f);

        //ȸ�� ����
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->FreezeRotation = true;

        //��ǲ
        if (!f.TryGet<PlayerLink>(entity, out var playerLink)) return;
        var input = f.GetPlayerInput(playerLink.PlayerRef);


        //����
        int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

        //���� �ӵ�
        //if (currentFrame < HitFrame)
        //{
        //    body->Velocity.X = flip;
        //    Debug.Log("�� Execute");
        //    //if (input->LeftPunch)
        //    //{
        //    //    Debug.Log("��Ʈ����");
        //    //    AnimatorComponent.SetTrigger(f, animatorComponent, "Rp");
        //    //}
        //}

        //��Ÿ ����
        


        //��Ʈ �ڽ� ����
        if (currentFrame == HitFrame - 1)//��Ʈ �ڽ� ���� ������ �� ������ ���� �����Ǿ����
        {
            //�÷��̾� ���� �ʱ�ȭ 
            player->isJump = false;
            player->isDodgeHigh = false;

            EntityRef hitbox = f.Create();

            f.Add(hitbox, new Transform2D
            {
                //Change
                //��ġ
                Position = f.Get<Transform2D>(entity).Position + new FPVector2(FP._0_25 * flip, 0),
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {
                IsTrigger = true,
                //Change
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2))
            });

            //���� ����
            f.Add(hitbox, new LSDF_HitboxInfo
            {
                startFrame = HitFrame,
                AttackerEntity = entity,

                AttackType = HitboxAttackType.Mid,
                CountType = CountAttackType.Combo,
                DelayGuardTpye = DelayGuardType.Stun,
                HomingReturnType = HomingType.Combo,
                

                launcher = true,
                jumpAttack = true,
                dodgeHigh = true,

                enemyGuardTime = 21,
                enemyHitTime = 28,
                enemyCountTime = 28,
                attackDamage = 13,
            });



            f.Add(hitbox, new TickToDestroy
            {
                TickToDestroyAt = f.Number + 1
            });

            if (player->canCounter == true)
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
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;
        player->isAttack = false;

        Debug.Log($"���� �� ������ : {f.Number}");
    }
}