namespace Quantum.Menu {
  using Quantum.LSDF;
  using UnityEngine;

  partial class QuantumMenuConnectionBehaviour {
    // Project-specific seam, called before connecting. No Simulation code reads local storage.
    protected static void PrepareLSDFPlayers(QuantumMenuConnectArgs args) {
      if (args.RuntimePlayers == null) args.RuntimePlayers = new RuntimePlayer[0];
      var players = new RuntimePlayer[args.RuntimePlayers.Length];
      for (int index = 0; index < players.Length; ++index) {
        var original = args.RuntimePlayers[index];
        var copy = original == null ? new RuntimePlayer() :
          JsonUtility.FromJson<RuntimePlayer>(JsonUtility.ToJson(original));
        if (index == 0 && !args.Reconnecting) {
          var skills = new int[LSDFLoadout.SlotCount];
          for (int slot = 0; slot < skills.Length; ++slot)
            skills[slot] = PlayerPrefs.GetInt($"Skill_{slot}", 0);
          copy.CommandSkills = LSDFLoadout.CopyValidated(skills);
        } else {
          // Additional local players must supply their own payload; do not copy player zero.
          copy.CommandSkills = LSDFLoadout.CopyValidated(copy.CommandSkills);
        }
        players[index] = copy;
      }
      args.RuntimePlayers = players;
      // Payload/DSL layout changed: never silently match with the old client protocol.
      string suffix = "-" + LSDFLoadout.ProtocolVersion;
      if (string.IsNullOrEmpty(args.AppVersion) || !args.AppVersion.EndsWith(suffix))
        args.AppVersion = (args.AppVersion ?? string.Empty) + suffix;
    }
  }
}
