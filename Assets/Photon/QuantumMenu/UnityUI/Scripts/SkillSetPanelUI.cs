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
        Debug.Log("���� �Ϸ�!");
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
        // UI ���� (��: �� ���Կ� ����� skillId�� �̸����� ǥ�� ��)
    }

    public SkillSet GetCurrentSkillSet() => currentSkillSet;
}
