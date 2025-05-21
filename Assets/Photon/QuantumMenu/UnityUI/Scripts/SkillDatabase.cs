using Quantum.LSDF;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LSDF/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillGroup> Groups;

    public SkillGroup GetSkillGroup(CommandDirection direction, CommandButton button)
    {
        return Groups.Find(g => g.Direction == direction && g.Button == button);
    }
}

[System.Serializable]
public class SkillGroup
{
    public CommandDirection Direction;
    public CommandButton Button;
    //spublic List<SkillOption> Options;
}

[System.Serializable]
public class SkillSet
{
    public int[] CommandSkillMap = new int[28]; // 7방향 × 4버튼
}
