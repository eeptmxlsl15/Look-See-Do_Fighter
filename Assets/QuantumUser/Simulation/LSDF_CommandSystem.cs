using Quantum;
using Photon.Deterministic;
using UnityEngine.Scripting;
using UnityEngine;
namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_CommandSystem : SystemMainThreadFilter<LSDF_CommandSystem.Filter>
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
            if (!f.Unsafe.TryGetPointer<PlayerLink>(filter.Entity, out var playerLink)) return;
            var input = f.GetPlayerInput(playerLink->PlayerRef);

            var direction = GetDirection(input);

            

            if (input->LeftPunch || input->RightPunch || input->LeftKick || input->RightKick)
            {
                //Debug.Log($"공격 입력 프레임 : {f.Number}");
                //애니메이터의 커맨드 맵 스테이트로 이동
                AnimatorComponent.SetTrigger(f, filter.Animator, "InputCommand");
                CheckCommand(f, filter.Entity, input, direction, filter.LSDF_Player, filter.Animator);
            }

            
        }

        private CommandDirection GetDirection(Input* input)
        {
            if (input->Down)
            {
                if (input->Left)
                {
                    
                    return CommandDirection.DownLeft;
                }
                if (input->Right) return CommandDirection.DownRight;
                return CommandDirection.Down;
            }
            if (input->Up) return CommandDirection.Up;
            if (input->Left) return CommandDirection.Left;
            if (input->Right) return CommandDirection.Right;
            return CommandDirection.Neutral;
        }

        private void CheckCommand(Frame f, EntityRef entity, Input* input, CommandDirection direction, LSDF_Player* player, AnimatorComponent* animator)
        {
            if (input->LeftPunch)
            {
                AnimatorComponent.SetTrigger(f, animator, "Lp");
                TryTriggerSkill(f, input->LeftPunch, CommandButton.LeftPunch, direction, player, animator);
            }
            else if (input->RightPunch)
            {
                AnimatorComponent.SetTrigger(f, animator, "Rp");
                TryTriggerSkill(f, input->RightPunch, CommandButton.RightPunch, direction, player, animator);
            }
            else if(input->LeftKick)
            {
                AnimatorComponent.SetTrigger(f, animator, "Lk");
                TryTriggerSkill(f, input->LeftKick, CommandButton.LeftKick, direction, player, animator);
            }
            else if (input->RightKick)
            {
                AnimatorComponent.SetTrigger(f, animator, "Rk");
                TryTriggerSkill(f, input->RightKick, CommandButton.RightKick, direction, player, animator);
            }

        }

        private void TryTriggerSkill(Frame f, bool buttonPressed, CommandButton button, CommandDirection direction, LSDF_Player* player, AnimatorComponent* animator)
        {
            if (!buttonPressed) return;
            
            AnimatorComponent.SetInteger(f, animator, "CommandNum", (int)direction);

            int index = ((int)direction * 4) + (int)button;
            int skillId = player->CommandSkillMap[index];


            AnimatorComponent.SetInteger(f, animator, "FinalNum", skillId);

        }
    }

    public enum CommandDirection
    {
        Neutral,
        Left,
        DownLeft,
        Down,
        DownRight,
        Right,
        Up,
    }

    public enum CommandButton
    {
        LeftPunch,
        RightPunch,
        LeftKick,
        RightKick
    }
}