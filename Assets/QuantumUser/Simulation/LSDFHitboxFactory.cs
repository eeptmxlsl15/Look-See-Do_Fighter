using Photon.Deterministic;

namespace Quantum.LSDF {
  /// <summary>Stateless single-hit materialization. Multi-hit scheduling is intentionally separate.</summary>
  public static class LSDFHitboxFactory {
    public static EntityRef Spawn(Frame frame, EntityRef attacker, int facing, LSDFSingleHitDefinition hit,
        FPVector2? effectPositionOverride = null) {
      EntityRef entity = frame.Create();
      ConfigureBody(frame, entity, attacker, facing, hit);
      AddPayload(frame, entity, attacker, hit, effectPositionOverride);
      ScheduleDestroy(frame, entity);
      return entity;
    }

    public static void ConfigureBody(Frame frame, EntityRef entity, EntityRef attacker, int facing, LSDFSingleHitDefinition hit) {
      FPVector2 position = hit.PositionAt(frame.Get<Transform2D>(attacker).Position, facing);
      frame.Add(entity, new Transform2D { Position = position, Rotation = FP._0 });
      frame.Add(entity, new PhysicsCollider2D {
        IsTrigger = true,
        Shape = Shape2D.CreateBox(hit.HalfExtents)
      });
    }

    public static void AddPayload(Frame frame, EntityRef entity, EntityRef attacker, LSDFSingleHitDefinition hit,
        FPVector2? effectPositionOverride = null) {
      frame.Add(entity, hit.CreateInfo(attacker, effectPositionOverride ?? frame.Get<Transform2D>(entity).Position));
    }

    public static void ScheduleDestroy(Frame frame, EntityRef entity) {
      frame.Add(entity, new TickToDestroy { TickToDestroyAt = DestroyTick(frame.Number) });
    }

    public static int DestroyTick(int spawnTick) {
      return spawnTick + 1;
    }
  }
}
