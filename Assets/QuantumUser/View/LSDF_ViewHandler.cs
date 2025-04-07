using Quantum;
using System.Linq;
using UnityEngine;

public class LSDF_ViewHandler : QuantumEntityViewComponent
{
    public Animator animator;

    public RuntimeAnimatorController playerController;
    public RuntimeAnimatorController enemyController;

    public override void OnActivate(Frame f)
    {
        if (animator == null || Game == null)
            return;

        if (!f.TryGet<PlayerLink>(EntityRef, out var playerLink))
            return;

        var localPlayer = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();
        bool isMine = playerLink.PlayerRef == localPlayer;

        // 에니메이터 컨트롤러 바꾸기!
        animator.runtimeAnimatorController = isMine ? playerController : enemyController;

        Debug.Log($"[AnimatorChange] Entity: {EntityRef}, IsMine: {isMine}, Controller: {animator.runtimeAnimatorController.name}");
    }
}
