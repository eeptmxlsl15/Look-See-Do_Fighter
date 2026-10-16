namespace Quantum.LSDF {
  /// <summary>Exactly two match slots. An existing fighter belongs to its slot for the match lifetime.</summary>
  public static class LSDFRoster {
    public static bool IsFighter(PlayerRef player) { return player == (PlayerRef)0 || player == (PlayerRef)1; }
    public static bool TryFind(Frame frame, PlayerRef player, out EntityRef entity) {
      entity = default;
      if (!IsFighter(player)) return false;
      foreach (var pair in frame.GetComponentIterator<PlayerLink>()) {
        if (frame.Get<PlayerLink>(pair.Entity).PlayerRef == player && frame.Has<LSDF_Player>(pair.Entity)) {
          entity = pair.Entity;
          return true;
        }
      }
      return false;
    }
    public static bool TryPair(Frame frame, out EntityRef first, out EntityRef second) {
      bool a = TryFind(frame, (PlayerRef)0, out first);
      bool b = TryFind(frame, (PlayerRef)1, out second);
      return a && b;
    }
  }
}
