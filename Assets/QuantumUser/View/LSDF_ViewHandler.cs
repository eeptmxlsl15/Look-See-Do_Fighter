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

        // 에니메이터 컨트롤러 바꾸기!
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
        // 현재 구조
        // 플레이어2는 다른 방향의 카메라를 쓴다 -> View
        // 플레이어2의 이동 방향은 PlayerSystem에서 filp 변수의 영향을 받아 반대로 이동한다 -> Simulaion
        // 플레이어1 입장에서는 이미 플레이어를 바라보는 enemy 애니메이터를 쓰고 있다 -> View
        // 플레이어2 입장에서 모든 플레이어들은 flip 되어있다. -> View
        // 실제 시뮬레이션에서 flip되어 충돌이 일어나게 해야한다.
        
        

        
        
    }
    public override void OnUpdateView()
    {
        
        base.OnUpdateView();
        
        if (cameraPlayer1 != null && player1 != null && player2 != null)
        {
            var center = (player1.transform.position + player2.transform.position) * 0.5f;
            //cameraPlayer1.transform.position = new Vector3(center.x, 0, cameraPlayer1.transform.position.z);

            //거리계산
            float distance = Vector3.Distance(player1.transform.position, player2.transform.position);
            Debug.Log($"현재 거리 {distance}");

            float baseZ = -0.8f;
            float maxZoomOut = -1.45f;
            float zoomFactor = -0.97857f;
            float targetZ= -0.8f;

            if (distance > 1)
            {
                targetZ = baseZ + zoomFactor * (distance - 1f);
                targetZ = Mathf.Clamp(targetZ, maxZoomOut, baseZ); // 줌 인/아웃 제한
            }
            // 최종 위치 설정 (부드럽게 보간)
            Vector3 targetPos = new Vector3(center.x, 0, targetZ);
            
            cameraPlayer1.transform.position = Vector3.Lerp(cameraPlayer1.transform.position, targetPos, Time.deltaTime * 5f);


        }
        else if (cameraPlayer2 != null && player1 != null && player2 != null)
        {
            var center = (player1.transform.position + player2.transform.position) * 0.5f;
            cameraPlayer2.transform.position = new Vector3(center.x, 0, cameraPlayer2.transform.position.z);
        }
    }
    



}
