using Quantum;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LSDF_IngameUI : QuantumEntityViewComponent
{
    private Image LeftHpGage;
    private Image RightHpGage;
    public TextMeshProUGUI TimerText;

    private EntityRef myPlayerEntity;
    private EntityRef opponentEntity;

    public Image[] LeftRoundWins = new Image[3];
    public Image[] RightRoundWins = new Image[3];

    private bool initialized = false;

    public override void OnActivate(Frame frame)
    {
        base.OnActivate(frame);

        var myRef = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();

        string canvasName = (myRef == (PlayerRef)0) ? "Player1 Canvas" : "Player2 Canvas";
        var canvas = GameObject.Find(canvasName);

        if (canvas == null)
        {
            Debug.LogError($"[{name}] Canvas '{canvasName}'를 찾을 수 없습니다.");
            return;
        }

        var leftHp = canvas.transform.Find("Player1 UI/Left Current Health");
        var rightHp = canvas.transform.Find("Player2 UI/Right Current Health");

        if (leftHp != null) LeftHpGage = leftHp.GetComponent<Image>();
        if (rightHp != null) RightHpGage = rightHp.GetComponent<Image>();

        var timer = canvas.transform.Find("Time");
        if (timer != null) TimerText = timer.GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < 3; i++)
        {
            string roundName = $"{i + 1}Round/Win";

            var myRound = canvas.transform.Find($"Player1 UI/Round/{roundName}");
            if (myRound != null)
            {
                LeftRoundWins[i] = myRound.GetComponent<Image>();
                LeftRoundWins[i].gameObject.SetActive(false);
            }

                    
            

            var oppRound = canvas.transform.Find($"Player2 UI/Round/{roundName}");
            if (oppRound != null)
            {
                RightRoundWins[i] = oppRound.GetComponent<Image>();
                RightRoundWins[i].gameObject.SetActive(false);
            }
                
        }

        if (LeftHpGage == null || RightHpGage == null || TimerText == null)
        {
            Debug.LogError("UI 바운딩 누락!");
        }
    }

    public override void OnUpdateView()
    {
        var game = QuantumRunner.Default.Game;
        var frame = game.Frames?.Predicted;
        if (frame == null) return;

        PlayerRef myRef = game.GetLocalPlayers().FirstOrDefault();

        if (!initialized)
        {
            foreach (var pair in frame.GetComponentIterator<PlayerLink>())
            {
                var entity = pair.Entity;
                var link = frame.Get<PlayerLink>(entity);

                if (link.PlayerRef == myRef)
                    myPlayerEntity = entity;
                else
                    opponentEntity = entity;
            }
            initialized = myPlayerEntity.IsValid && opponentEntity.IsValid;
        }

        if (frame.TryGet<LSDF_Player>(myPlayerEntity, out var myPlayer))
        {
            float ratio = myPlayer.playerHp / 170f;
            LeftHpGage.fillAmount = ratio;

            for (int i = 0; i < 3; i++)
            {
                if (i < myPlayer.loseRound)
                    RightRoundWins[i]?.gameObject.SetActive(true);
            }
        }

        if (frame.TryGet<LSDF_Player>(opponentEntity, out var oppPlayer))
        {
            float ratio = oppPlayer.playerHp / 170f;
            RightHpGage.fillAmount = ratio;

            for (int i = 0; i < 3; i++)
            {
                if (i < oppPlayer.loseRound)
                    LeftRoundWins[i]?.gameObject.SetActive(true);
            }
        }
    }
}