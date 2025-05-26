using Photon.Deterministic;
using Quantum.Addons.Animator;
using Quantum;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class QuickHookWindowEvent : AnimatorTimeWindowEventAsset
{
    /// <summary>
    /// ���� ������
    /// </summary>
    private const int HitFrame = 13;


    private int currentFrame;
    bool bufferedNextAttack;

    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region �ʼ� ��� 
        Debug.Log($"���� ���� ������{f.Number}");

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

        //���� ó������ ���� �ڼ�,���� �����̶��
        player->isDodgeHigh = false;
        player->isJump = false;

        //---��Ÿ ��� ---//
        //���� �ؽ�Ʈ �ʱ�ȭ
        bufferedNextAttack = false;
        Debug.Log($"�� ���� ������ : {currentFrame}");


    }
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
        #region ���� X
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
        #endregion

        //���� �ӵ�
        if (currentFrame < HitFrame)
        {
            body->Velocity.X = flip;
            Debug.Log("�� Execute");
            //if (input->LeftPunch)
            //{
            //    Debug.Log("��Ʈ����");
            //    AnimatorComponent.SetTrigger(f, animatorComponent, "Rp");
            //}
        }

        ////��Ÿ ����
        //if (5 <= currentFrame && currentFrame <= 15) // ���� �Է� ���� �� �ִ� ����
        //{
        //    //����
        //    if (input->RightPunch && (AnimatorComponent.GetInteger(f, animatorComponent, "FinalNum") == 0))
        //    {

        //        bufferedNextAttack = true;  // �ϴ� ���ุ ��
        //        Debug.Log("�� �߿� Lp �Է� �� ���� ���� �Ϸ�");
        //    }

        //    //����
        //    if (input->LeftPunch && (AnimatorComponent.GetInteger(f, animatorComponent, "FinalNum") == 1))
        //    {

        //        bufferedNextAttack = true;  // �ϴ� ���ุ ��
        //        Debug.Log("�� �߿� Lp �Է� �� ���� ���� �Ϸ�");
        //    }
        //}
        //else if (currentFrame >= 16 && bufferedNextAttack)
        //{

        //    AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", true);
        //    // AnimatorComponent.SetTrigger(f, animatorComponent, "Lp");

        //    Debug.Log("����� Lp �ߵ�");
        //}


        ////���ȸ��, ���� 
        //if(1<currentFrame && currentFrame< HitFrame-1)
        //{
        //    player->isJump = true;
        //    player->isDodgeHigh = true;
        //}

        //��Ʈ �ڽ� ����
        if (currentFrame == HitFrame - 1)//��Ʈ �ڽ� ���� ������ �� ������ ���� �����Ǿ����
        {

            EntityRef hitbox = f.Create();

            //�÷��̾� ���� �ʱ�ȭ
            player->isJump = false;
            player->isDodgeHigh = false;

            //-------------------------------------------���-----------------------------------------//
            FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25 + FP._0_05) * flip, FP._0_25);

            f.Add(hitbox, new Transform2D
            {
                //��ġ
                Position = hitboxPosition,
                Rotation = FP._0
            });

            f.Add(hitbox, new PhysicsCollider2D
            {

                IsTrigger = true,
                //�ڽ� ũ��
                Shape = Shape2D.CreateBox(new FPVector2(FP._0_20 / 2, (FP._0_10 - FP._0_02) / 2))
            });

            ////-------------------------------------------�ߴ�-----------------------------------------//
            //FPVector2 hitboxPosition = f.Get<Transform2D>(entity).Position + new FPVector2((FP._0_25) * flip, 0);
            //f.Add(hitbox, new Transform2D
            //{
            //    //Change
            //    //��ġ
            //    Position = hitboxPosition,
            //    Rotation = FP._0
            //});

            //f.Add(hitbox, new PhysicsCollider2D
            //{
            //    IsTrigger = true,
            //    //Change
            //    //�ڽ� ũ��
            //    Shape = Shape2D.CreateBox(new FPVector2(FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2))
            //});

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

                AttackType = HitboxAttackType.High,
                CountType = CountAttackType.Combo,
                DelayGuardTpye = DelayGuardType.Normal,
                HomingReturnType = HomingType.Stun,

                jumpAttack = false,
                dodgeHigh = false,
                launcher = false,
                wallLauncher = false,
                notSitLauncher = false,

                enemyGuardTime = 19,
                enemyHitTime = 22,
                enemyCountTime = 28,
                attackDamage = 17,
                forceBack = 0
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
        ////��Ÿ ������ ���
        // AnimatorComponent.SetBoolean(f, animatorComponent, "NextAttack", false);
        var entity = animatorComponent->Self;
        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);
        if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;
        body->Velocity.X = 0;

        player->isAttack = false;
        player->isJump = false;
        player->isDodgeHigh = false;

        Debug.Log($"���� �� ������ : {f.Number}");
    }
}