using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEngine.TestTools;

public partial class LSDFOnlineSmokeTests {
  [UnityTest, Timeout(180000), Category("LSDFOnline")]
  public IEnumerator DifferentMenuBuildsExecuteTheirOwnSkillsAndReplayWithoutDisconnecting() {
    return RunScenario(true);
  }

  private static async Task VerifyDifferentLoadouts(QuantumRunner[] runners, RuntimePlayer[] players, CancellationToken token) {
    var samples = new[] { new Dictionary<int, ulong>(), new Dictionary<int, ulong>() };
    var seen = new bool[2, 2, 5]; // observer client, fighter, command.
    var expected = new[] {
      new[] { "1_Jap", "2_ClaHoming", "2_DraRightUpper", "1_Assult", "1_LarsSnake" },
      new[] { "0_Jap", "1_Chopping", "1_LawRightUpper", "0_DeathFist", "0_NarakTwo" }
    };
    int start = Math.Max(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) + 60;
    string error = null;
    using var input = QuantumCallback.SubscribeManual<CallbackPollInput>(callback => {
      int owner = IndexOf(runners, callback.Game);
      if (owner < 0) return;
      int relative = callback.Frame - start;
      var value = new Quantum.Input();
      if (relative >= 0 && relative < 1800) {
        int command = relative / 360;
        int phase = relative % 360;
        // Both players issue the same command on the same tick with different selected skills.
        if (phase >= 30 && phase < 33) {
          switch (command) {
            case 0: value.LeftPunch = true; break;
            case 1: value.Left = true; value.LeftPunch = true; break;
            case 2: value.Down = true; value.Right = true; value.RightPunch = true; break;
            case 3: value.Right = true; value.RightPunch = true; break;
            case 4: value.Down = true; value.Left = true; value.RightKick = true; break;
          }
        }
      }
      callback.SetInput(value, DeterministicInputFlags.Repeatable);
    });
    using var frames = QuantumCallback.SubscribeManual<CallbackSimulateFinished>(callback => {
      int observer = IndexOf(runners, callback.Game);
      if (observer < 0 || !callback.Frame.IsVerified) return;
      int count = 0;
      foreach (var pair in callback.Frame.GetComponentIterator<LSDF_Player>()) {
        if (!callback.Frame.TryGet<PlayerLink>(pair.Entity, out var link)) continue;
        var data = callback.Frame.GetPlayerData(link.PlayerRef);
        int fighter = data.PlayerNickname == "LSDF-test-A" ? 0 : 1;
        var player = callback.Frame.Get<LSDF_Player>(pair.Entity);
        if (data.CommandSkills == null || data.CommandSkills.Length != 28) {
          error = $"Observer {observer}, fighter {fighter}: the player payload is malformed.";
          continue;
        }
        for (int slot = 0; slot < 28; slot++)
          if (ReadSkill(player, slot) != players[fighter].CommandSkills[slot] ||
              data.CommandSkills[slot] != players[fighter].CommandSkills[slot])
            error = $"Observer {observer}, fighter {fighter}, slot {slot}: the personal build changed.";
        count++;
        if (!callback.Frame.TryGet<AnimatorComponent>(pair.Entity, out var animator)) continue;
        var graph = callback.Frame.FindAsset<AnimatorGraph>(animator.AnimatorGraph);
        var layers = callback.Frame.ResolveList(animator.Layers);
        if (layers.Count == 0) continue;
        var state = graph.GetState(layers[0].CurrentStateId);
        for (int command = 0; command < 5; command++) {
          if (state == null || state.Name != expected[fighter][command] || seen[observer, fighter, command]) continue;
          seen[observer, fighter, command] = true;
          TestContext.Out.WriteLine($"BUILD observer={observer}, fighter={fighter}, attack={state.Name}, tick={callback.Frame.Number}");
        }
      }
      if (count > 2) error = "The loadout scenario spawned duplicate fighters.";
      if (count == 2 && callback.Frame.Number % 30 == 0)
        samples[observer][callback.Frame.Number] = callback.Frame.CalculateChecksum();
    });
    using var checksum = QuantumCallback.SubscribeManual<CallbackChecksumError>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Different personal builds caused a checksum error.";
    });
    using var plugin = QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(callback => {
      if (IndexOf(runners, callback.Game) >= 0) error = "Photon plugin disconnect: " + callback.Reason;
    });
    runners[0].Game.AddPlayer(0, players[0]);
    runners[1].Game.AddPlayer(0, players[1]);
    var deadline = DateTime.UtcNow.AddSeconds(75);
    while (Math.Min(runners[0].Game.Frames.Verified.Number, runners[1].Game.Frames.Verified.Number) < start + 1980) {
      if (error != null) throw new InvalidOperationException(error);
      if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("The different-build scenario did not finish.");
      await Task.Delay(100, token);
    }
    if (error != null) throw new InvalidOperationException(error);
    for (int observer = 0; observer < 2; observer++)
      for (int fighter = 0; fighter < 2; fighter++)
        for (int command = 0; command < 5; command++)
          if (!seen[observer, fighter, command])
            throw new InvalidOperationException($"Observer {observer} never saw fighter {fighter}'s {expected[fighter][command]}.");
    int comparisons = 0, target = 0;
    foreach (var sample in samples[0]) {
      if (!samples[1].TryGetValue(sample.Key, out var other)) continue;
      if (sample.Value != other) throw new InvalidOperationException("Different-build checksum mismatch at tick " + sample.Key);
      comparisons++;
      target = Math.Max(target, sample.Key);
    }
    if (comparisons < 50) throw new InvalidOperationException("Insufficient common verified frames for different builds.");
    var replay = runners[0].Game.GetRecordedReplay(includeChecksums: true, includeDb: false);
    if (replay == null) throw new InvalidOperationException("Different-build replay is missing.");
    await VerifyReplay(replay, samples[0], target, start + 60, "build-A", token, requirePendingBuffer: false);
    await VerifyReplay(replay, samples[0], target, start + 60, "build-B", token, requirePendingBuffer: false);
    TestContext.Out.WriteLine($"DIFFERENT BUILDS PASS: 28 slots per fighter, 10 selected attack routes on both clients, {comparisons} matching checksums, two replays and Frame restore/resimulation; no plugin disconnect.");
  }
}
