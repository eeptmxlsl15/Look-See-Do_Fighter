namespace Quantum.LSDF {
  public static class LSDFSpawnPolicy {
    /// <summary>Keep an existing fighter on repeated AddPlayer/rejoin; never reset its round/HP.</summary>
    public static bool CanSpawn(Frame f, PlayerRef player) {
      if (!LSDFRoster.IsFighter(player)) return false;
      foreach (var pair in f.GetComponentIterator<PlayerLink>()) {
        if (f.Get<PlayerLink>(pair.Entity).PlayerRef == player && f.Has<LSDF_Player>(pair.Entity)) return false;
      }
      return true;
    }
  }
}
