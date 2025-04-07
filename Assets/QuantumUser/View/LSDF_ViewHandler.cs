using Quantum;
using System.Linq;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class LSDF_ViewHandler : QuantumEntityViewComponent
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public RuntimeAnimatorController playerController;
    public RuntimeAnimatorController enemyController;
    private static GameObject cameraPlayer1;
    private static GameObject cameraPlayer2;
    public override void OnActivate(Frame f)
    {
        if (cameraPlayer1 == null)
            cameraPlayer1 = GameObject.Find("Player1 Camera");
        if (cameraPlayer2 == null)
            cameraPlayer2 = GameObject.Find("Player2 Camera");

        if (animator == null || Game == null)
            return;

        if (!f.TryGet<PlayerLink>(EntityRef, out var playerLink))
            return;

        var localPlayer = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();
        bool isMine = playerLink.PlayerRef == localPlayer;

        // ���ϸ����� ��Ʈ�ѷ� �ٲٱ�!
        animator.runtimeAnimatorController = isMine ? playerController : enemyController;
        
        if (localPlayer == (PlayerRef)0)
        {
            cameraPlayer1?.SetActive(true);
            cameraPlayer2?.SetActive(false);
        }
        else if (localPlayer == (PlayerRef)1)
        {
            cameraPlayer1?.SetActive(false);
            cameraPlayer2?.SetActive(true);

            var handlers = FindObjectsOfType<LSDF_ViewHandler>();
            foreach (var handler in handlers)
            {
                
                handler.spriteRenderer.flipX = true;
                
            }
        }
        // ���� ����
        // �÷��̾�2�� �ٸ� ������ ī�޶� ���� -> View
        // �÷��̾�2�� �̵� ������ PlayerSystem���� filp ������ ������ �޾� �ݴ�� �̵��Ѵ� -> Simulaion
        // �÷��̾�1 ���忡���� �̹� �÷��̾ �ٶ󺸴� enemy �ִϸ����͸� ���� �ִ� -> View
        // �÷��̾�2 ���忡�� ��� �÷��̾���� flip �Ǿ��ִ�. -> View
        // ���� �ùķ��̼ǿ��� flip�Ǿ� �浹�� �Ͼ�� �ؾ��Ѵ�.
        
        

        
        
    }
   
}
