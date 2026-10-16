namespace Quantum.Menu {
  using Quantum.LSDF;
  /// <summary>Menu-owned mutable selection; never used as simulation state.</summary>
  public sealed class LSDFLoadoutSelection {
    private readonly LSDF_GameConfig _rules;
    private readonly int _maximum;
    private int[] _skills;
    public LSDFLoadoutSelection(int[] skills, LSDF_GameConfig rules, int maximum) {
      _rules = rules;
      _maximum = maximum;
      _skills = LSDFLoadout.CopyBudgetValidated(skills, rules);
    }
    public int Cost => LSDFLoadout.Cost(_skills, _rules);
    public int Skill(int slot) => LSDFLoadout.ReadSkill(_skills, slot);
    public int[] Snapshot() => (int[])_skills.Clone();
    public bool TrySelect(int slot, int skill) {
      if ((uint)slot >= LSDFLoadout.SlotCount || skill < 0 || skill > LSDFLoadout.MaxSkillId(slot)) return false;
      var candidate = (int[])_skills.Clone();
      candidate[slot] = skill;
      if (LSDFLoadout.Cost(candidate, _rules) > _maximum) return false;
      _skills = candidate;
      return true;
    }
  }
}
