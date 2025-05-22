using UnityEngine;
using System.Collections.Generic;
using Quantum.LSDF;

public class SkillScrollToggle : MonoBehaviour
{
    [System.Serializable]
    public struct ButtonScrollGroup
    {
        public CommandButton buttonType;
        public GameObject scrollContent;
    }

    public List<ButtonScrollGroup> groups;
    public void Start()
    {
        ShowScrollForButton(0);
    }
    public void ShowScrollForButton(int buttonIndex)
    {
        CommandButton targetButton = (CommandButton)buttonIndex;

        foreach (var group in groups)
        {
            bool isActive = group.buttonType == targetButton;
            group.scrollContent.SetActive(isActive);
        }
    }
}