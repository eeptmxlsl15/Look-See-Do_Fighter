using Quantum.LSDF;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectionUI : MonoBehaviour
{
    private SkillSet currentSkillSet = new SkillSet();
    public int totalCost;

    public void OnSkillSelected(int direction, int button, int skillId , int cost)
    {
        int index = (direction * 4) + button;
        currentSkillSet.CommandSkillMap[index] = skillId;
        totalCost += cost;
    }

    public void SaveSkillSet()
    {
        string json = JsonUtility.ToJson(currentSkillSet);
        PlayerPrefs.SetString("CommandSkillSet", json);
        PlayerPrefs.Save();
        Debug.Log("저장 완료!");
    }

    public void LoadSkillSet()
    {
        if (PlayerPrefs.HasKey("CommandSkillSet"))
        {
            string json = PlayerPrefs.GetString("CommandSkillSet");
            currentSkillSet = JsonUtility.FromJson<SkillSet>(json);
            ApplyToUI();
        }
    }

    private void ApplyToUI()
    {
        // UI 갱신 (예: 각 슬롯에 저장된 skillId로 미리보기 표시 등)
    }

    public SkillSet GetCurrentSkillSet() => currentSkillSet;
}
