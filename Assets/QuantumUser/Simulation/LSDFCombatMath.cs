using Photon.Deterministic;

namespace Quantum.LSDF {
  public static class LSDFCombatMath {
    // Authored animation frames are 60 Hz. This is NOT the session update frequency.
    public const int AnimationFramesPerSecond = 60;

    public static int AnimationFrame(FP seconds) {
      return (seconds * AnimationFramesPerSecond).AsInt;
    }

    // Positive integer damage, exact rational 120%, truncation after multiplication.
    public static int CounterDamage(int baseDamage) {
      return checked((int)((long)baseDamage * 6 / 5));
    }
  }
}
