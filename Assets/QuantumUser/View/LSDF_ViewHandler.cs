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
    private static LSDF_ViewHandler player1;
    private static LSDF_ViewHandler player2;
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
        
        if (playerLink.PlayerRef == (PlayerRef)0)
            player1 = this;
        else if (playerLink.PlayerRef == (PlayerRef)1)
            player2 = this;

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
    public override void OnUpdateView()
    {
        
        
        base.OnUpdateView();

        if (player1 != null && player2 != null)
        {
            // ���� Ȱ��ȭ�� ī�޶� ���� flip ���� ����
            GameObject activeCamera = cameraPlayer1 != null && cameraPlayer1.activeSelf ? cameraPlayer1 : cameraPlayer2;
            int flip = activeCamera == cameraPlayer1 ? 1 : -1;

            var center = (player1.transform.position + player2.transform.position) * 0.5f;
            float distance = Vector3.Distance(player1.transform.position, player2.transform.position);

            float baseZ = -0.8f * flip;
            float zoomFactor = -1f * flip;
            float targetZ = baseZ;

            if (distance > 1)
            {
                targetZ = baseZ + zoomFactor * (distance - 1f);
            }

            Vector3 targetPos = new Vector3(center.x, 0, targetZ);

            if (activeCamera != null)
            {
                activeCamera.transform.position = Vector3.Lerp(activeCamera.transform.position, targetPos, Time.deltaTime * 5f);
            }
        }
    }
    



}
