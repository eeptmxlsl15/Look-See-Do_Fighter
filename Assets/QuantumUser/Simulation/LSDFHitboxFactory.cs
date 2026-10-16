using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Stateless single-hit materialization. Multi-hit scheduling is intentionally separate.</summary>
  public static class LSDFHitboxFactory {
    public static EntityRef Spawn(Frame frame, EntityRef attacker, int facing, LSDFSingleHitDefinition hit,
        FPVector2? effectPositionOverride = null) {
      FPVector2 position = hit.PositionAt(frame.Get<Transform2D>(attacker).Position, facing);
      EntityRef entity = frame.Create();
      frame.Add(entity, new Transform2D { Position = position, Rotation = FP._0 });
      frame.Add(entity, new PhysicsCollider2D {
        IsTrigger = true,
        Shape = Shape2D.CreateBox(hit.HalfExtents)
      });
      frame.Add(entity, hit.CreateInfo(attacker, effectPositionOverride ?? position));
      frame.Add(entity, new TickToDestroy { TickToDestroyAt = DestroyTick(frame.Number) });
      return entity;
    }

    public static int DestroyTick(int spawnTick) {
      return spawnTick + 1;
    }
  }
}
