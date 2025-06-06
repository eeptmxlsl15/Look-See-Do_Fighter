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
    //public int parentIndex; // ����
    public CommandDirection parentIndex;
    public CommandButton Button;
    private UnityEngine.UI.Button[] buttons;
    private int selectedIndex = -1; //���� ��


    void Awake()
    {
        buttons = GetComponentsInChildren<UnityEngine.UI.Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            int buttonIndex = i; // ���� ������ ĸó ����
            buttons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
        }
    }

    void OnButtonClicked(int buttonIndex)
    {
        // ������ ���õ� ��ư ��Ȱ��ȭ
        if (selectedIndex != -1 && selectedIndex != buttonIndex)
        {
            // ��: ���� �ʱ�ȭ
            buttons[selectedIndex].GetComponent<Image>().color = Color.white;
        }

        // ���� ���õ� ��ư ����
        buttons[buttonIndex].GetComponent<Image>().color = Color.green;

        // ���� �ε��� ������Ʈ
        selectedIndex = buttonIndex;


        int index = ((int)parentIndex * 4) + (int)Button;

        // �ʿ��� ���� index ���� (���ÿ� �α�)
        PlayerPrefs.SetInt($"Skill_{index}", selectedIndex);
        //
        

    }
}
