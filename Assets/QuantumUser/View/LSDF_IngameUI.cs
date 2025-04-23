using Quantum;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LSDF_IngameUI : QuantumEntityViewComponent
{
    private Image LeftHpGage;
    private Image RightHpGage;

    private EntityRef myPlayerEntity;
    private EntityRef opponentEntity;
    private bool initialized = false;

    public override void OnActivate(Frame frame)
    {
        base.OnActivate(frame);
        
        var myRef = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();

        // � �÷��̾����� Ȯ��
        string canvasName = (myRef == (PlayerRef)0) ? "Player1 Canvas" : "Player2 Canvas";
        var canvas = GameObject.Find(canvasName);

        if (canvas == null)
        {
            Debug.LogError($"[{name}] Canvas '{canvasName}'�� ã�� �� �����ϴ�.");
            return;
        }

        // �ڽ��� UI ��Ʈ���� HP ������ ��������
        var left = canvas.transform.Find("Player1 UI/Left Current Health");
        var right = canvas.transform.Find("Player2 UI/Right Current Health");

        if (left != null) LeftHpGage = left.GetComponent<Image>();
        if (right != null) RightHpGage = right.GetComponent<Image>();

        if (LeftHpGage == null || RightHpGage == null)
        {
            Debug.LogError("HP ������ ������Ʈ�� ã�� �� �����ϴ�.");
        }
    }
    public override void OnUpdateView()
    {
        var game = QuantumRunner.Default.Game;
        var frame = game.Frames?.Predicted;

        if (frame == null) return;

        PlayerRef myRef = game.GetLocalPlayers().FirstOrDefault();

        // �÷��̾�� ��� ��ƼƼ �� ���� ã��
        if (!initialized)
        {
            foreach (var pair in frame.GetComponentIterator<PlayerLink>())
            {
                var entity = pair.Entity;
                var link = frame.Get<PlayerLink>(entity);

                if (link.PlayerRef == myRef)
                {
                    myPlayerEntity = entity;

                }
                else
                {
                    opponentEntity = entity;

                }
            }

            if (myPlayerEntity.IsValid && opponentEntity.IsValid)
            {
                initialized = true;

            }

        }

        // �� ü��
        if (frame.TryGet<LSDF_Player>(myPlayerEntity, out var myPlayer))
        {
            float ratio = myPlayer.playerHp / 170f;
            LeftHpGage.fillAmount = ratio;

        }

        // ��� ü��
        if (frame.TryGet<LSDF_Player>(opponentEntity, out var oppPlayer))
        {
            float ratio = oppPlayer.playerHp / 170f;
            RightHpGage.fillAmount = ratio;

        }
    }
}
