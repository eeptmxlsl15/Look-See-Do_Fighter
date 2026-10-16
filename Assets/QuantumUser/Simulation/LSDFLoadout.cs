namespace Quantum.LSDF {
  /// <summary>Wire/DSL contract: seven directions, four buttons, slot-local skill IDs.</summary>
  public static class LSDFLoadout {
    public const int DirectionCount = 7;
    public const int ButtonCount = 4;
    public const int SlotCount = DirectionCount * ButtonCount;
    public const string ProtocolVersion = "lsdf-loadout-v2";
    public const int SkillsPerSlot = 3;

    public static int Slot(CommandDirection direction, CommandButton button) {
      int d = (int)direction, b = (int)button;
      return (uint)d < DirectionCount && (uint)b < ButtonCount ? d * ButtonCount + b : -1;
    }

    // Preserve the menu's first-match restore costs; duplicate placeholder buttons are not a second catalog.
    public static int DefaultCost(int slot, int skill) {
      if (slot == 4) return skill == 2 ? 10 : skill == 1 ? 7 : 6;
      switch (slot) {
        case 0: return skill == 0 ? 5 : 0;
        case 8: return 8;
        case 12: return 5;
        case 16: return 5;
        case 20: return 8;
        case 24: return 10;
        default: return 0;
      }
    }

    public static int Cost(int[] skills, LSDF_GameConfig config = null) {
      int total = 0;
      for (int slot = 0; slot < SlotCount; slot++) {
        int skill = ReadSkill(skills, slot), index = slot * SkillsPerSlot + skill;
        int cost = config != null && config.SkillCosts != null && index < config.SkillCosts.Length
          ? config.SkillCosts[index] : DefaultCost(slot, skill);
        if (cost < 0) cost = 0;
        if (total > int.MaxValue - cost) return int.MaxValue;
        total += cost;
      }
      return total;
    }

    public static int[] CopyBudgetValidated(int[] skills, LSDF_GameConfig config = null) {
      var result = CopyValidated(skills);
      int maximum = config != null && config.MaximumLoadoutCost >= 0 ? config.MaximumLoadoutCost : 140;
      return Cost(result, config) <= maximum ? result : new int[SlotCount];
    }

    // Mirrors FinalNum transitions in LSDF_PlayerAnimatorGraph, not menu button costs.
    public static int MaxSkillId(int slot) {
      switch (slot) {
        case 0: return 1;  // Neutral LP: two jab variants
        case 4: return 2;  // Back LP
        case 17: return 2; // Down-forward RP
        case 21: return 1; // Forward RP
        case 11: return 1; // Down-back RK
        default: return 0;
      }
    }

    public static int ReadSkill(int[] skills, int slot) {
      if ((uint)slot >= SlotCount || skills == null || skills.Length != SlotCount) return 0;
      int id = skills[slot];
      return id >= 0 && id <= MaxSkillId(slot) ? id : 0;
    }

    public static int[] CopyValidated(int[] skills) {
      var copy = new int[SlotCount];
      for (int slot = 0; slot < SlotCount; ++slot) copy[slot] = ReadSkill(skills, slot);
      return copy;
    }
  }
}
