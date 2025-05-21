using UnityEngine;

public static class SkillSaveManager
{
    public static void SaveSkillSelection(int index, int skillIndex)
    {
        PlayerPrefs.SetInt($"SkillMap_{index}", skillIndex);
    }

    public static int LoadSkillSelection(int index)
    {
        return PlayerPrefs.GetInt($"SkillMap_{index}", 0);
    }
}
