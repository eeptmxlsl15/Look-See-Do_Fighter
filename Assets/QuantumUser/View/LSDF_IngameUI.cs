using Quantum;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LSDF_IngameUI : QuantumEntityViewComponent
{
    private Image LeftHpGage;
    private Image RightHpGage;
    public TextMeshProUGUI TimerText;

    public EntityRef myPlayerEntity;
    public EntityRef opponentEntity;

    public Image[] LeftRoundWins = new Image[3];
    public Image[] RightRoundWins = new Image[3];

    public GameObject RoundMessageRoot;
    public TMP_Text RoundText;
    public TMP_Text FightText;

    private bool initialized = false;
    private bool isRoundIntroPlaying = false;

    
    public override void OnActivate(Frame frame)
    {
        base.OnActivate(frame);

        var myRef = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();

        string canvasName = (myRef == (PlayerRef)0) ? "Player1 Canvas" : "Player2 Canvas";
        var canvas = GameObject.Find(canvasName);

        if (canvas == null)
        {
            Debug.LogError($"[{name}] Canvas '{canvasName}'�� ã�� �� �����ϴ�.");
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

        RoundMessageRoot = canvas.transform.Find("Round Text")?.gameObject;
        RoundText = RoundMessageRoot?.transform.Find("Round")?.GetComponent<TMP_Text>();
        FightText = RoundMessageRoot?.transform.Find("Fight")?.GetComponent<TMP_Text>();

        if (LeftHpGage == null || RightHpGage == null || TimerText == null || RoundText == null || FightText == null)
        {
            Debug.LogError("UI �ٿ�� ����!");
        }

        RoundMessageRoot?.SetActive(false);
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

        if(myPlayer.playerHp<=0 || oppPlayer.playerHp <= 0)
        {
            Debug.Log("���� 0");
            
            ShowRoundIntro(myPlayer.loseRound + oppPlayer.loseRound+1);
        }
    }

    
        
    

    public void ShowRoundIntro(int roundNumber)
    {
        Debug.Log("���� 1");
        if (isRoundIntroPlaying) return;
        Debug.Log("���� 2");

        StartCoroutine(PlayRoundIntroCoroutine(roundNumber));
    }

    private IEnumerator PlayRoundIntroCoroutine(int roundNumber)
    {
        Debug.Log("���� 3");
        isRoundIntroPlaying = true;
        RoundMessageRoot.SetActive(true);

        RoundText.text = $"Round {roundNumber}";
        RoundText.gameObject.SetActive(true);
        FightText.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.2f);

        RoundText.gameObject.SetActive(false);
        FightText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);

        RoundMessageRoot.SetActive(false);
        isRoundIntroPlaying = false;
    }
}
