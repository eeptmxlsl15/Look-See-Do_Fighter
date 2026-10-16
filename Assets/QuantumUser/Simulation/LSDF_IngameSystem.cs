using UnityEngine.Scripting;
namespace Quantum.LSDF {
  [Preserve]
  public unsafe class LSDF_IngameSystem : SystemMainThread {
    public override void Update(Frame f) {
      if (!LSDFRoster.TryPair(f, out var first, out var second)) return;
      if (!f.Unsafe.TryGetPointer<LSDF_Player>(first, out var a) || !f.Unsafe.TryGetPointer<LSDF_Player>(second, out var b)) return;
      if (LSDFRoundPolicy.Phase(true, *a, *b, f.Number) == LSDFRoundPhase.Recovering) return;
      a->isRoundEnd = false;
      b->isRoundEnd = false;
      if (!LSDFRoundPolicy.IsKnockedOut(*a) && !LSDFRoundPolicy.IsKnockedOut(*b)) return;
      LSDFRoundPolicy.BeginRecovery(ref *a, f.Number, LSDFBalance.RoundLock(f));
      LSDFRoundPolicy.BeginRecovery(ref *b, f.Number, LSDFBalance.RoundLock(f));
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(first, out var firstBody)) firstBody->Velocity.X = 0;
      if (f.Unsafe.TryGetPointer<PhysicsBody2D>(second, out var secondBody)) secondBody->Velocity.X = 0;
    }
  }
}
