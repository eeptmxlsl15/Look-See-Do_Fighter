using Photon.Deterministic;

namespace Quantum.LSDF {
  public static class LSDFBalance {
    public static LSDF_GameConfig Config(Frame frame) {
      return frame.RuntimeConfig.GameConfig.Id.IsValid ? frame.FindAsset<LSDF_GameConfig>(frame.RuntimeConfig.GameConfig) : null;
    }
    public static int MaximumHealth(Frame f) { var c = Config(f); return c != null && c.MaximumHealth > 0 ? c.MaximumHealth : LSDFPlayerHealth.Maximum; }
    public static int Healing(Frame f) { var c = Config(f); return c != null && c.HealingPerTick >= 0 ? c.HealingPerTick : LSDFPlayerHealth.HealingPerTick; }
    public static int RoundLock(Frame f) { var c = Config(f); return c != null && c.RoundLockTicks > 0 ? c.RoundLockTicks : 210; }
    public static int HealingLead(Frame f) { var c = Config(f); return c != null && c.HealingLeadTicks >= 0 ? c.HealingLeadTicks : 150; }
    public static int RoundSeconds(Frame f) { var c = Config(f); return c != null && c.RoundSeconds > 0 ? c.RoundSeconds : 60; }
    public static FP WalkSpeed(Frame f) { var c = Config(f); return c != null && c.WalkSpeed > FP._0 ? c.WalkSpeed : FP._0_50; }
    public static FP DashSpeed(Frame f) { var c = Config(f); return c != null && c.DashSpeed > FP._0 ? c.DashSpeed : (FP)2; }
    public static FP SpawnOffset(Frame f) { var c = Config(f); return c != null && c.SpawnOffset > FP._0 ? c.SpawnOffset : FP._0_50; }
  }
}
