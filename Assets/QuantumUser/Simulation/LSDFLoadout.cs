namespace Quantum.LSDF {
  /// <summary>Wire/DSL contract: seven directions, four buttons, slot-local skill IDs.</summary>
  public static class LSDFLoadout {
    public const int DirectionCount = 7;
    public const int ButtonCount = 4;
    public const int SlotCount = DirectionCount * ButtonCount;
    public const string ProtocolVersion = "lsdf-loadout-v1";

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
