using Photon.Deterministic;
using Quantum;
using System.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Windows;
using static UnityEngine.EventSystems.EventTrigger;

//�÷��̾� ������,����,��Ʈ

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_PlayerSystem : SystemMainThreadFilter<LSDF_PlayerSystem.Filter>, 
        ISignalOnTriggerNormalHit, ISignalOnTriggerCounterHit , ISignalOnTriggerLauncherHit,
        ISignalOnTriggerEnemyGuard , ISignalOnTriggerGuard, 
        ISignalOnTriggerEnemyParring  ,
        ISignalOnCollisionGroundEnter,ISignalOnCollisionGroundExit,
        ISignalOnCollisionEnterWall,ISignalOnCollisionExitWall  , ISignalOnCollisionWallHitEnter
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform2D* Transform;
            public PhysicsBody2D* Body;
            public LSDF_Player* LSDF_Player;
            public AnimatorComponent* Animator;
            
        }

        public override void Update(Frame f, ref Filter filter)
        {
            Input* input = default;
            CollisionControll(f, ref filter);
            //�������� ��� ����
            f.Unsafe.TryGetPointer<LSDF_Player>(filter.Entity, out var player);
            
            //��ǲ ����
            //if (player->isStun || player->isGuard || player->isHit)
            //{
            //    if (inputBuffer->Count < 5)
            //    {
            //        int index = inputBuffer->Count;
            //        inputBuffer->BufferedFrames[index] = f.Number;
            //        inputBuffer->Directions[index] = (byte)GetDirection(input); // ��: 1~9 ����Ű
            //        inputBuffer->Buttons[index] = EncodeButtonInput(input);     // ��: 1 = LP, 2 = RP ��
            //        inputBuffer->Count++;
            //    }
            //}


            //ȸ�� ����
            filter.Transform->Rotation = FP._0;

            //������ ���������
            if(filter.Body->Velocity.Y==0 && player->isAir == true)
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "YZero",true);
                Debug.Log("��������");
            }

            if (player->isAttack == true || player->isStun == true || player->isCombo == true || player->isAir || player->isWallHit || player->isCantMove)
            {
                //Debug.Log("���� ��");
                return;
            }

            //������ ���� ���̸鼭 ���� �������� ������ idle���·� ��
            if (filter.LSDF_Player->isGuard && f.Number >= filter.LSDF_Player->DelayFrame)
            {
                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle�� �����ϴ� Ʈ����
                Debug.Log($"���� ���� �� ������ : {f.Number}");



                filter.LSDF_Player->isGuard = false;
            }
            else
            {

                //Debug.Log($"IsGuard{filter.LSDF_Player->isHit}/���� ������{f.Number}/���� ������ ������{filter.LSDF_Player->DelayFrame}");
            }
            //������ ��Ʈ ���̸鼭 ��Ʈ �������� ������ idle���·� ��
            if (filter.LSDF_Player->isHit && f.Number >= filter.LSDF_Player->DelayFrame)
            {
                AnimatorComponent.SetTrigger(f, filter.Animator, "DelayFrame"); // Idle�� �����ϴ� Ʈ����
                Debug.Log($"��Ʈ ���� �� ������ : {f.Number}");

                //���� ���� ������
                filter.LSDF_Player->hitWallLauncher = false;

                filter.LSDF_Player->isHit = false;
            }
            else
            {

                //Debug.Log($"IsHit:{filter.LSDF_Player->isHit}/���� ������{f.Number}/��Ʈ ������ ������{filter.LSDF_Player->DelayFrame}");
            }


            //Unsate.TryGetPointer�� ���� �а� ������ �����ϴ�
            //f.Get<PlayerLink> �� �ϸ� �д� �͸� �����ϴ�.
            //������ TryGetPointer�� �ð����⵵�� 1�̴�. ������� ECS ��Ű���ı� ������
            //TODO ������� ECS ��Ű���� ����

            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                input = f.GetPlayerInput(playerLink->PlayerRef);


            }

            //2p�� ���� ����

            bool shouldAttack = true;

            //bool shouldAttack = f.Number % 60 < 30;
            if (playerLink->PlayerRef == (PlayerRef)1)
            {
                if (shouldAttack)
                {
                    //input->Right = true;
                    //input->Down = true;
                    //input->LeftPunch = true;
                    //input->RightPunch = true;
                    //input->Up = true;
                    input->Right = true;

                }
                else
                {
                    //input->Left = true;
                    //input->LeftPunch = true;
                    input->Down = true;
                    input->RightKick = true;
                }

            }

            //�и� �� ��Ű �� �� �и� ���
            if (!input->Up)
            {
                player->isParring = false;
            }
            //�и� �� ���� �� �и� ���
            if (input->LeftPunch || input->RightPunch || input->LeftKick || input->RightKick)
            {
                player->isParring = false;
            }

            //if (playerLink->PlayerRef == (PlayerRef)0)
            //{

            //}
            //AnimatorComponent.GetBoolean(f, filter.Animator, "MoveBack");


            DetectDashCommand(f, ref filter, input);
            UpdateMovement(f, ref filter, input);

        }
        
        private void UpdateMovement(Frame f, ref Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);
            var playerState = f.Get<LSDF_Player>(filter.Entity);

            //��Ʈ�ڽ� ũ��


            //TODO �ȱ� �ӵ� ���߿� �ۿ��� ���� �� �� �ְ� ������
            FP walkSpeed = FP._0_50; ;

            //�ι�° �÷��̾��� ��� ����
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            //�ƹ� �Է� ���� �� ������ ����
            filter.Body->Velocity = FPVector2.Zero;

            

            if (input->Up)
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "Parring", true);
                return;
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "Parring", false);
            }
            if (input->Down || (input->Down && input->Left) || (input->Down && input->Right))
            {

                //�ɱ� ����
                //�ɾ��մ� �ݶ��̴���
                if (filter.LSDF_Player->isSit == false)
                {

                    AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", true);

                    filter.LSDF_Player->isSit = true;
                }
            }
            else
            {
                //���� ����
                //���ִ� �ݶ��̴��� 

                AnimatorComponent.SetBoolean(f, filter.Animator, "IsSit", false);
                filter.LSDF_Player->isSit = false;
            }
            

            

            //�ɾ��ִ� ���� �ٸ� ������ �Ұ���
            if (filter.LSDF_Player->isSit == true || filter.LSDF_Player->isHit || filter.LSDF_Player->isGuard) return;
            
            if (input->Left && input->Right)
            {
                
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);
                return;
            }

            

            if (input->Left && playerState.isDashBack == false)
            {
                filter.Body->Velocity.X = -walkSpeed * flip;

                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveBack", false);
            }

            if (input->Right && playerState.isDashFront == false)
            {
                filter.Body->Velocity.X = walkSpeed * flip;
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", true);
            }
            else
            {
                AnimatorComponent.SetBoolean(f, filter.Animator, "MoveFront", false);

                //filter.Body->AngularVelocity = FPMath.Clamp(filter.Body->AngularVelocity, -8, 8);
            }


        }

        private void CollisionControll(Frame f, ref Filter filter)
        {
            //ũ��
            FPVector2 standingColliderExtents = new FPVector2(FP._0_20-FP._0_02, FP._0_33);
            FPVector2 sitColliderExtents = new FPVector2(FP._0_20 - FP._0_02, FP._0_25);

            //��ġ
            Transform2D standingColliderCenter = new Transform2D
            {
                Position = new FPVector2(FP._0, FP._0_01),
                Rotation = FP._0
            };
            Transform2D crouchingColliderCenter = new Transform2D
            {
                Position = new FPVector2(FP._0, FP._0_25 - FP._0_33 + FP._0_02),
                Rotation = FP._0
            };



            f.Unsafe.TryGetPointer<PhysicsCollider2D>(filter.Entity, out var collider);

            //���� ũ��� ��ġ
            var currentExtents = collider->Shape.Box.Extents;
            var currentCenter = collider->Shape.LocalTransform;

            //�ɱ�,���⿡ ���� ũ��� ��ġ ����
            FPVector2 targetExtents = filter.LSDF_Player->isSit ? sitColliderExtents : standingColliderExtents;
            Transform2D targetCenter = filter.LSDF_Player->isSit ? crouchingColliderCenter : standingColliderCenter;

            //����� ������ �ٲ��� ����
            if (!currentExtents.Equals(targetExtents))
            {
                collider->Shape.Box.Extents = targetExtents;
                collider->Shape.LocalTransform = targetCenter;
            }
        }

        private void DetectDashCommand(Frame f, ref Filter filter, Input* input)
        {
            var playerLink = f.Get<PlayerLink>(filter.Entity);

            FP walkSpeed = FP._0_50; ;

            //�ι�° �÷��̾��� ��� ����
            int flip = playerLink.PlayerRef == (PlayerRef)0 ? 1 : -1;

            var buffer = f.Unsafe.GetPointer<DashInputBuffer>(filter.Entity);

            DirectionType dir = DirectionType.None;

            if (input->Left) dir = DirectionType.Left;
            else if (input->Right) dir = DirectionType.Right;

            bool isPressed = dir != DirectionType.None;

            if (isPressed && !buffer->LastInputPressed)
            {
                var now = f.Number;

                // ���� ������ �� �� ���Ȱ�, �Է� �� ������ DashInputWindow �̳��� ���
                if (buffer->LastDirection == dir && (now - buffer->LastInputTick) <= buffer->DashInputWindow)
                {

                    // ���⿡ �뽬 ���� �����̳� �ִϸ��̼� Ʈ���� �ֱ�
                    if (dir == DirectionType.Right)
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashFront", true);
                    }
                    else
                    {

                        AnimatorComponent.SetBoolean(f, filter.Animator, "DashBack", true);
                    }

                }

                // ����
                buffer->PrevDirection = buffer->LastDirection;
                buffer->LastDirection = dir;
                buffer->LastInputTick = now;
            }

            buffer->LastInputPressed = isPressed;

        }
        
        public void OnTriggerNormalHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            player->isAttack = false;
            //��ܿ� ��Ʈ�� ����

            //������ ���α� �¾��� ��
            if (player->isWallHit == true)
            {
                if (player->wallCount < 2 && hitbox->wallLauncher == false)
                {
                    AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
                    player->wallCount++;
                    Debug.Log("���� ī���� : " + player->wallCount);
                }
                else if (hitbox->wallLauncher == true)
                {
                    Debug.Log("���� �� �ν���");
                }
                else if(player->wallCount >= 2)
                {
                    Debug.Log("���� �� �ν���");
                }
            }
            else if (hitbox->wallLauncher == true && player->isOnWall == true && player->isWallHit == false)
            {
                Debug.Log("���� ���α� ����");
                AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
                
            }
            //���� �´� ���� ��
            //�޺� �õ��� �¾��� ��
            else if (hitbox->launcher == true)
            {
                Debug.Log("�޺� : ���� ��");
                //�ɾƼ� ��� ��Ʈ �Ҷ� �ȶߴ� ���۶�� ��Ʈ�� ��
                if (hitbox->notSitLauncher == true && player->isSit)
                {
                    Debug.Log("�޺� : �ɾƼ� �¾Ƽ� �� ��");
                    AnimatorComponent.SetTrigger(f, animator, "MiddleHit");

                }
                else
                {

                    //�װ� �ƴϸ� ���
                    Debug.Log("�޺� : ��");

                    //�÷��̾ ���� �پ��ִٸ� �ٷ� ����

                    if (player->isOnWall)                   {
                        AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
                    }
                    //�װ� �ƴϸ� ���߸��
                    else
                        AnimatorComponent.SetTrigger(f, animator, "Air");


                    //player->isAir = true;

                    //if (f.Unsafe.TryGetPointer<PhysicsBody2D>(info.Entity, out var body))
                    //{
                        
                    //    body->Velocity.X =hitbox->forceBack;    // �ʿ�� �¿쵵 �� �� ����
                    //}
                    //���� ������ �ִϸ޴ϼ� 
                }
            }
            //���� �޺��� �´� ���϶�
            else if (player->isAir)
            {
                Debug.Log("�޺� �´� ��");
                if (f.Unsafe.TryGetPointer<PhysicsBody2D>(info.Entity, out var body))
                {
                    // �ʿ�� �¿쵵 �� �� ����

                    AnimatorComponent.SetTrigger(f, animator, "Air");
                }
            }
            //���
            else if (hitbox->AttackType == HitboxAttackType.High)
            {
                Debug.Log($"��Ʈ ���� ������{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "HighHit");
                
            }
            //�ߴ�
            else if (hitbox->AttackType == HitboxAttackType.Mid)
            {
                Debug.Log($"��Ʈ ���� ������{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "MiddleHit");
                
            }
            //�ϴ�
            else if(hitbox->AttackType == HitboxAttackType.Low)
            {
                Debug.Log($"��Ʈ ���� ������{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "LowHit");
                
            }
            

            player->isHit = true;
            player->DelayFrame = f.Number + hitbox->enemyHitTime;

            
            if (player->hitCount == 0)
            {
                player->playerHp -= hitbox->attackDamage;
                Debug.Log($"���� ü�� : {player->playerHp}/170");
            }
            else if (player->hitCount == 2)
            {
                player->playerHp -=(int)(hitbox->attackDamage*FP._0_75);
                Debug.Log($"��Ʈ ī��Ʈ 2 / ������ {hitbox->attackDamage *FP._0_75}");
            }
            else if (player->hitCount == 4)
            {
                player->playerHp -= (int)(hitbox->attackDamage * FP._0_50);
                Debug.Log($"��Ʈ ī��Ʈ 4 / ������ {hitbox->attackDamage * FP._0_50}");
            }
            else if (player->hitCount > 4)
            {
                player->playerHp -= (int)(hitbox->attackDamage * FP._0_33);
                Debug.Log($"��Ʈ ī��Ʈ 4 �̻�  / ������ {hitbox->attackDamage * FP._0_33}");
            }
            
            

        }
        

        public void OnTriggerCounterHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            //ī���� �� ����
            player->canCounter = false;
            player->isAttack = false;

            if (hitbox->CountType==CountAttackType.Normal)
            {
                if (hitbox->AttackType == HitboxAttackType.High)
                {
                    Debug.Log($"��Ʈ ���� ������{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "HighHit");

                }
                //�ߴ�
                else if (hitbox->AttackType == HitboxAttackType.Mid)
                {
                    Debug.Log($"��Ʈ ���� ������{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "MiddleHit");

                }
                //�ϴ�
                else if (hitbox->AttackType == HitboxAttackType.Low)
                {
                    Debug.Log($"��Ʈ ���� ������{f.Number}");
                    AnimatorComponent.SetTrigger(f, animator, "LowHit");

                }
                player->isHit = true;
                player->DelayFrame = f.Number + hitbox->enemyCountTime;

                player->playerHp -= (int)(hitbox->attackDamage *1.2f);//���⼭ ������ ����
                
                Debug.Log($"���� ü�� : {player->playerHp}/170");
            }
            //�޺� ��Ȳ�� ��� ���
            else if(hitbox->CountType == CountAttackType.Combo)
            {
                
                AnimatorComponent.SetTrigger(f, animator, "Air");
                //player->isAir = true;
                
            }

            
            
        }
        public void OnTriggerGuard(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator,LSDF_HitboxInfo* hitbox)
        {
            //�ϴ��� ������ ���
            if (hitbox->AttackType == HitboxAttackType.Low)
            {
                Debug.Log($"���� ���� ������{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "LowGuard");
                

            }
            //��,�ߴ��� ������ ���
            else
            {
                Debug.Log($"���� ���� ������{f.Number}");
                AnimatorComponent.SetTrigger(f, animator, "StandGuard");
               
            }
            player->isGuard = true;
            player->DelayFrame = f.Number + hitbox->enemyGuardTime;

        }

        public void OnTriggerEnemyGuard(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            if(hitbox->DelayGuardTpye == DelayGuardType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // �ʿ� �� ���°��� ���� ����
                
                
                //10�����Ӹ� ���� ���� 30������
            }
            else if(hitbox->DelayGuardTpye == DelayGuardType.Combo)
            {
                //�׳� �밡�� ���� 30������
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }
        }

        public void OnTriggerEnemyParring(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {

            if (hitbox->HomingReturnType == HomingType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // �ʿ� �� ���°��� ���� ����


                //10�����Ӹ� ���� ���� 30������
            }
            else if (hitbox->HomingReturnType == HomingType.Combo)
            {
                //�׳� �밡�� ���� 30������
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }

        }

        public void OnTriggerLauncherHit(Frame f, TriggerInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_HitboxInfo* hitbox)
        {
            //���� �ȸ��� -> �ʿ� ����?
            if (hitbox->HomingReturnType == HomingType.Stun)
            {
                AnimatorComponent.SetTrigger(f, animator, "Stun");

                // �ʿ� �� ���°��� ���� ����


                //10�����Ӹ� ���� ���� 30������
            }
            else if (hitbox->HomingReturnType == HomingType.Combo)
            {
                //�׳� �밡�� ���� 30������
                AnimatorComponent.SetTrigger(f, animator, "Combo");
            }

        }
        #region �ٴ� ����
        public void OnCollisionGroundEnter(Frame f, CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Ground* ground)
        {
            player->isGround = true;
            AnimatorComponent.SetBoolean(f, animator, "Ground", true);
            Debug.Log($"isGround ��: {player->isGround}");
        }

        public void OnCollisionGroundExit(Frame f, ExitInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Ground* ground)
        {
            player->isGround = false;
            AnimatorComponent.SetBoolean(f, animator, "Ground", false);
            Debug.Log($"isGround ���� : {player->isGround}");
        }
        #endregion

        #region �� ����

        //�ܼ��� ���� �پ� �ִ��� �ƴ����� �Ǻ��ϴ� �ñ׳�
        public void OnCollisionEnterWall(Frame f,CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall) 
        {
            
            player->isOnWall = true;
            
            //���� ���߿� ������
            //if (player->isAir)
            //{
            //    AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
            //}
            Debug.Log($"�� isOnWall {player->isOnWall}");
        }
        public void OnCollisionExitWall(Frame f, ExitInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall)
        {

            player->isOnWall = false;
            
            Debug.Log($"�� isOnWall {player->isOnWall}");
        }
        public void OnCollisionWallHitEnter(Frame f, CollisionInfo2D info, LSDF_Player* player, AnimatorComponent* animator, LSDF_Wall* wall)
        {
            if (player->isAir)
            {
                AnimatorComponent.SetTrigger(f, animator, "FirstWallHit");
            }
            else
                AnimatorComponent.SetTrigger(f, animator, "SecondWallHit");
        }


       
        

        


        #endregion
    }

}
