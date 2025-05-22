using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Quantum.LSDF;

public class SkillSelectionManager : MonoBehaviour
{
    public int MaxCost = 140;
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;

    public TextMeshProUGUI costText;

    private Dictionary<(CommandDirection, CommandButton), List<SkillButton>> buttonMap = new();
    private Dictionary<(CommandDirection, CommandButton), SkillButton> selectedButtons = new();
    private int currentCost = 0;

    void Start()
    {
        //레지스트리 초기화
        //ResetRegistry();

        var allSkillButtons = GetComponentsInChildren<SkillButton>(true);

        foreach (var sb in allSkillButtons)
        {
            var key = (sb.Direction, sb.ButtonType);

            if (!buttonMap.ContainsKey(key))
                buttonMap[key] = new List<SkillButton>();

            buttonMap[key].Add(sb);
            sb.UIButton.onClick.AddListener(() => OnSkillButtonClicked(sb));
        }

        LoadSelectionsFromPrefs();
        UpdateCostText();
    }

    public void ResetRegistry()
    {
        PlayerPrefs.DeleteAll(); // 모든 PlayerPrefs 삭제
        PlayerPrefs.Save();
    }

    void OnSkillButtonClicked(SkillButton clicked)
    {
        var key = (clicked.Direction, clicked.ButtonType);

        // 해제: 이미 선택된 버튼이면 해제
        //if (selectedButtons.TryGetValue(key, out var currentSelected) && currentSelected == clicked)
        //{
        //    clicked.UIButton.GetComponent<Image>().color = defaultColor;
        //    selectedButtons.Remove(key);
        //    currentCost -= clicked.Cost;
        //    PlayerPrefs.DeleteKey($"Skill_{GetIndex(key)}");
        //    UpdateCostText();
        //    return;
        //}

        // 새로 선택할 경우: 코스트 체크
        int tempCost = currentCost;

        // 기존 선택된 것이 있으면 코스트 빼고 교체
        if (selectedButtons.TryGetValue(key, out var prevButton))
            tempCost -= prevButton.Cost;

        tempCost += clicked.Cost;

        if (tempCost > MaxCost)
        {
            Debug.LogWarning("코스트 초과로 선택 불가!");
            return;
        }

        // 기존 선택 해제
        if (prevButton != null)
            prevButton.UIButton.GetComponent<Image>().color = defaultColor;

        // 새 선택 적용
        clicked.UIButton.GetComponent<Image>().color = selectedColor;
        selectedButtons[key] = clicked;
        currentCost = tempCost;

        PlayerPrefs.SetInt($"Skill_{GetIndex(key)}", clicked.SkillIndex);
        PlayerPrefs.Save();

        UpdateCostText();
    }

    void LoadSelectionsFromPrefs()
    {
        foreach (var kvp in buttonMap)
        {
            var key = kvp.Key;
            int index = GetIndex(key);

            if (!PlayerPrefs.HasKey($"Skill_{index}"))
                continue;

            int selectedSkillIndex = PlayerPrefs.GetInt($"Skill_{index}");

            var buttonList = kvp.Value;
            var match = buttonList.Find(b => b.SkillIndex == selectedSkillIndex);

            if (match != null)
            {
                match.UIButton.GetComponent<Image>().color = selectedColor;

                selectedButtons[key] = match;

                
                currentCost += match.Cost;
            }
        }

        
        UpdateCostText();
    }

    int GetIndex((CommandDirection, CommandButton) key)
    {
        return ((int)key.Item1 * 4) + (int)key.Item2;
    }

    void UpdateCostText()
    {
        if (costText != null)
            costText.text = $"Cost : {currentCost}";
    }
}