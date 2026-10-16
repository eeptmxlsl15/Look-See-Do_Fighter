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
            LSDFCommandProcessor.Update(f, ref filter);
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