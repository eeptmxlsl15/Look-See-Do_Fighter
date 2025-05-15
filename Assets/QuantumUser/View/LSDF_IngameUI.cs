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
    public TMP_Text TimerText;

    public EntityRef myPlayerEntity;
    public EntityRef opponentEntity;

    public Image[] LeftRoundWins = new Image[3];
    public Image[] RightRoundWins = new Image[3];

    public GameObject RoundMessageRoot;
    public TMP_Text RoundText;
    public TMP_Text FightText;

    

    private bool initialized = false;
    private bool isRoundIntroPlaying = false;
    private bool hasShownThisRound = false;


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

        // ü�� ������
        LeftHpGage = canvas.transform.Find("Player1 UI/Left Current Health")?.GetComponent<Image>();
        RightHpGage = canvas.transform.Find("Player2 UI/Right Current Health")?.GetComponent<Image>();

        // Ÿ�̸� �ؽ�Ʈ
        TimerText = canvas.transform.Find("Time")?.GetComponent<TextMeshProUGUI>();

        // ���� �� UI ���ε�
        for (int i = 0; i < 3; i++)
        {
            string roundName = $"{i + 1}Round/Win";

            LeftRoundWins[i] = canvas.transform.Find($"Player1 UI/Round/{roundName}")?.GetComponent<Image>();
            if (LeftRoundWins[i] != null)
                LeftRoundWins[i].gameObject.SetActive(false);

            RightRoundWins[i] = canvas.transform.Find($"Player2 UI/Round/{roundName}")?.GetComponent<Image>();
            if (RightRoundWins[i] != null)
                RightRoundWins[i].gameObject.SetActive(false);
        }

        // ���� �ؽ�Ʈ UI
        RoundMessageRoot = canvas.transform.Find("Round Text")?.gameObject;
        RoundText = RoundMessageRoot?.transform.Find("Round")?.GetComponent<TMP_Text>();
        FightText = RoundMessageRoot?.transform.Find("Fight")?.GetComponent<TMP_Text>();
        RoundMessageRoot?.SetActive(false);

        // �ʼ� ��� ���ε� üũ
        if (LeftHpGage == null || RightHpGage == null)
            Debug.LogError("HP �������� ã�� �� �����ϴ�.");
        if (TimerText == null)
            Debug.LogError("TimerText�� ã�� �� �����ϴ�.");
        if (RoundText == null || FightText == null)
            Debug.LogError("RoundText �Ǵ� FightText�� ã�� �� �����ϴ�.");
    }

    public override void OnUpdateView()
    {
        var game = QuantumRunner.Default.Game;
        var frame = game.Frames?.Predicted;
        if (frame == null) return;

        PlayerRef myRef = game.GetLocalPlayers().FirstOrDefault();

        //Ÿ�̸�
        //var roundTime = frame.GetSingleton<LSDF_Timer>();
        //Debug.Log("Ÿ�̸� : " + roundTime.currentTime);
        //TimerText.text = roundTime.currentTime.ToString();
        
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

        

        
        

        if ((myPlayer.isHealing ==true || oppPlayer.isHealing == true )&& !hasShownThisRound)
        {
            Debug.Log("���� 0");
            
            ShowRoundIntro(myPlayer.loseRound + oppPlayer.loseRound+1);
            hasShownThisRound = true;
        }

        if (!myPlayer.isHealing && !oppPlayer.isHealing)
        {
            hasShownThisRound = false;
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
        yield return new WaitForSeconds(1.5f);

        RoundText.gameObject.SetActive(false);
        FightText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);

        RoundMessageRoot.SetActive(false);
        isRoundIntroPlaying = false;
    }
}
