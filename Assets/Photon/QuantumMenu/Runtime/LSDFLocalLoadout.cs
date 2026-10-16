namespace Quantum.Menu {
  using Quantum.LSDF;
  using UnityEngine;
  public static class LSDFLocalLoadout {
    public static LSDF_GameConfig Rules => Resources.Load<LSDF_GameConfig>("LSDF_GameConfig");
    public static int[] Read(LSDF_GameConfig rules = null) {
      var values = new int[LSDFLoadout.SlotCount];
      for (int slot = 0; slot < values.Length; slot++) {
        string key = "Skill_" + slot;
        values[slot] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) : PlayerPrefs.GetInt("SkillMap_" + slot, 0);
      }
      return LSDFLoadout.CopyBudgetValidated(values, rules != null ? rules : Rules);
    }
    public static void Save(int[] values) {
      var validated = LSDFLoadout.CopyBudgetValidated(values, Rules);
      for (int slot = 0; slot < validated.Length; slot++) PlayerPrefs.SetInt("Skill_" + slot, validated[slot]);
      PlayerPrefs.SetInt("Initialized", 1);
      PlayerPrefs.Save();
    }
    public static bool TrySet(int slot, int skill) {
      var rules = Rules;
      var selection = new LSDFLoadoutSelection(Read(rules), rules, rules != null ? rules.MaximumLoadoutCost : 140);
      if (!selection.TrySelect(slot, skill)) return false;
      Save(selection.Snapshot());
      return true;
    }
    public static void Reset() { Save(new int[LSDFLoadout.SlotCount]); }
  }
}
