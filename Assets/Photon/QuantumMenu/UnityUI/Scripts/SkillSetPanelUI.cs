using Photon.Deterministic.Protocol;
using Quantum;
using Quantum.LSDF;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectionUI : MonoBehaviour
{
    //public int parentIndex; // 방향
    public CommandDirection parentIndex;
    public CommandButton Button;
    private UnityEngine.UI.Button[] buttons;
    private int selectedIndex = -1; //고유 값


    void Awake()
    {
        buttons = GetComponentsInChildren<UnityEngine.UI.Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            int buttonIndex = i; // 로컬 변수로 캡처 방지
            buttons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
        }
    }

    void OnButtonClicked(int buttonIndex)
    {
        // 이전에 선택된 버튼 비활성화
        if (selectedIndex != -1 && selectedIndex != buttonIndex)
        {
            // 예: 색상 초기화
            buttons[selectedIndex].GetComponent<Image>().color = Color.white;
        }

        // 현재 선택된 버튼 강조
        buttons[buttonIndex].GetComponent<Image>().color = Color.green;

        // 선택 인덱스 업데이트
        selectedIndex = buttonIndex;


        int index = ((int)parentIndex * 4) + (int)Button;

        // 필요한 곳에 index 전달 (예시용 로그)
        PlayerPrefs.SetInt($"Skill_{index}", selectedIndex);
        //
        

    }
}
