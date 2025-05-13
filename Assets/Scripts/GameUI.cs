using System.Collections;
using TMPro;
using UnityEngine;

namespace Quantum.LSDF
{
    /// <summary>
    /// UI ���: Round N - Fight! �ִϸ��̼� �� ���� Ȯ��(������/�޴� ���� ��)�� ���� UI ����
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
        /// ���� ���� ������ �����Ѵ� (Round N -> Fight!)
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
        /// ���� ���� �� ������/�޴� UI Ȱ��ȭ
        /// </summary>
        public void ShowEndGameOptions()
        {
            EndGamePanel.SetActive(true);
        }

        // ��ư �̺�Ʈ�� - �ʿ��� ��� ���� ����
        public void OnRematchPressed()
        {
            Debug.Log("Rematch pressed");
            // TODO: �ٽ� ���� ���� ���� ����
        }

        public void OnReturnToMenuPressed()
        {
            Debug.Log("Return to Menu pressed");
            // TODO: ���� �޴� �ε� ���� ����
        }
    }
}
