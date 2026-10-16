using Quantum.Menu;
using Quantum.LSDF;
public static class SkillSaveManager {
  public static void SaveSkillSelection(int index, int skillIndex) { LSDFLocalLoadout.TrySet(index, skillIndex); }
  public static int LoadSkillSelection(int index) { return LSDFLoadout.ReadSkill(LSDFLocalLoadout.Read(), index); }
}
