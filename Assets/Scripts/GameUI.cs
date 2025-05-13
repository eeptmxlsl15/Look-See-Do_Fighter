using System.Collections;
using TMPro;
using UnityEngine;

namespace Quantum.LSDF
{
    /// <summary>
    /// UI 담당: Round N - Fight! 애니메이션 및 이후 확장(리벤지/메뉴 선택 등)을 위한 UI 관리
    /// </summary>
    public class RoundIntroUI : MonoBehaviour
    {
        [Header("Round Start Elements")]
        public GameObject RoundMessageRoot;
        public TMP_Text RoundText;
        public TMP_Text FightText;

        [Header("Post-Game Options (for future use)")]
        public GameObject EndGamePanel;
        public GameObject RematchButton;
        public GameObject ReturnToMenuButton;

        private void Awake()
        {
            RoundMessageRoot.SetActive(false);
            //EndGamePanel.SetActive(false);
        }

        /// <summary>
        /// 라운드 시작 연출을 실행한다 (Round N -> Fight!)
        /// </summary>
        public void PlayRoundIntro(int roundNumber)
        {
            StartCoroutine(RoundIntroSequence(roundNumber));
        }

        private IEnumerator RoundIntroSequence(int roundNumber)
        {
            RoundMessageRoot.SetActive(true);
            RoundText.gameObject.SetActive(true);
            FightText.gameObject.SetActive(false);

            RoundText.text = $"Round {roundNumber}";
            yield return new WaitForSeconds(1.2f);

            RoundText.gameObject.SetActive(false);
            FightText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.0f);

            FightText.gameObject.SetActive(false);
            RoundMessageRoot.SetActive(false);
        }

        /// <summary>
        /// 게임 종료 후 리벤지/메뉴 UI 활성화
        /// </summary>
        public void ShowEndGameOptions()
        {
            EndGamePanel.SetActive(true);
        }

        // 버튼 이벤트용 - 필요한 경우 연결 가능
        public void OnRematchPressed()
        {
            Debug.Log("Rematch pressed");
            // TODO: 다시 게임 시작 로직 연결
        }

        public void OnReturnToMenuPressed()
        {
            Debug.Log("Return to Menu pressed");
            // TODO: 메인 메뉴 로드 로직 연결
        }
    }
}
