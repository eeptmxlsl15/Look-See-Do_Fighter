using Quantum.LSDF;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SkillOption
{
    public string SkillName;
    public string Description;
    public int Cost;
}

public class SkillDatabase : MonoBehaviour
{
    public List<SkillGroup> Skills;

    [System.Serializable]
    public class SkillGroup
    {
        public CommandDirection Direction;
        public CommandButton Button;
        public List<SkillOption> Options;
    }

    public SkillGroup GetSkillGroup(CommandDirection dir, CommandButton btn)
    {
        return Skills.FirstOrDefault(g => g.Direction == dir && g.Button == btn);
    }
}
