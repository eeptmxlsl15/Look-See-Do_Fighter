using Photon.Deterministic;
using UnityEngine;
namespace Quantum.LSDF {
  public class LSDF_GameConfig : AssetObject {
    public int MaximumHealth = 170;
    public int HealingPerTick = 2;
    public int RoundLockTicks = 210;
    public int HealingLeadTicks = 150;
    public int RoundSeconds = 60;
    [Min(30), Tooltip("KO result hold before resetting the next round, in simulation ticks.")]
    public int RoundResultTicks = 120;
    [Min(1), Tooltip("Round/Fight introduction lock after resetting a round, in simulation ticks.")]
    public int RoundIntroTicks = 150;
    [Min(0), Tooltip("Lifetime of a newly pressed attack command in simulation ticks; 0 disables pre-input.")]
    public int InputBufferFrames = LSDFInputBuffer.DefaultWindowFrames;
    public FP WalkSpeed = FP._0_50;
    public FP DashSpeed = 2;
    public FP SpawnOffset = FP._0_50;
    public int MaximumLoadoutCost = 140;
    public int[] SkillCosts = DefaultCosts();
    // Retain the serialized legacy field for asset compatibility; not a fighter setting.
    [HideInInspector] public FP AsteroidInitialSpeed = 8;
    private static int[] DefaultCosts() {
      var values = new int[LSDFLoadout.SlotCount * LSDFLoadout.SkillsPerSlot];
      for (int slot = 0; slot < LSDFLoadout.SlotCount; slot++)
        for (int skill = 0; skill < LSDFLoadout.SkillsPerSlot; skill++)
          values[slot * LSDFLoadout.SkillsPerSlot + skill] = LSDFLoadout.DefaultCost(slot, skill);
      return values;
    }
  }
}
