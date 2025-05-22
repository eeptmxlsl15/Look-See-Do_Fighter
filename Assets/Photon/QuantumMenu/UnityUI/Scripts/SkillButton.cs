using Quantum.LSDF;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    public CommandDirection Direction;
    public CommandButton ButtonType;
    public int SkillIndex; //  ��� ���� ����
    public int Cost;  
    [HideInInspector]
    public Button UIButton;

    void Awake()
    {
        UIButton = GetComponent<Button>();
    }
}