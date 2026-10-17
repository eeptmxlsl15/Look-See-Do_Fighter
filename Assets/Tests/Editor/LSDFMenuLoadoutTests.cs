using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.Addons.Animator;
using Quantum.LSDF;
using Quantum.Menu;
using UnityEditor;
using UnityEngine;

public class LSDFMenuLoadoutTests {
  public static IEnumerable<int> Combinations() {
    // 2 neutral LP * 3 back LP * 3 down-forward RP * 2 forward RP * 2 down-back RK.
    for (int combination = 0; combination < 72; combination++) yield return combination;
  }

  [TestCaseSource(nameof(Combinations))]
  public void EverySelectableCombinationSurvivesTheRealMenuAndSdkSerializer(int combination) {
    var selected = new int[28];
    selected[0] = combination % 2; combination /= 2;
    selected[4] = combination % 3; combination /= 3;
    selected[17] = combination % 3; combination /= 3;
    selected[21] = combination % 2; combination /= 2;
    selected[11] = combination % 2;
    var template = LSDFMenuLoadoutFixture.Template("menu-contract");
    var args = LSDFMenuLoadoutFixture.Prepare(template, selected);
    CollectionAssert.AreEqual(selected, args.RuntimePlayers[0].CommandSkills);
    Assert.IsNull(template.CommandSkills, "Preparing one client must not modify the serialized template.");
    var serializer = new QuantumUnityJsonSerializer();
    using var stream = new MemoryStream();
    serializer.SerializePlayer(stream, args.RuntimePlayers[0]);
    stream.Position = 0;
    var received = (RuntimePlayer)serializer.DeserializePlayer(stream);
    CollectionAssert.AreEqual(selected, received.CommandSkills);

    // Use the independently authored graph as the contract for all 28 command routes.
    var graph = AssetDatabase.LoadAssetAtPath<AnimatorGraph>("Assets/QuantumUser/Animators/LSDF_PlayerAnimatorGraph.asset");
    var buttons = new[] { "Left Punch", "Right Punch", "Left Kick", "Right Kick" };
    for (int slot = 0; slot < 28; slot++) {
      var command = Find(graph, buttons[slot % 4]);
      string direction = Destination(command, "CommandNum", slot / 4);
      string attack = Destination(Find(graph, direction), "FinalNum", received.CommandSkills[slot]);
      Assert.IsNotNull(Find(graph, attack), $"Slot {slot}, skill {received.CommandSkills[slot]} has no attack destination.");
    }
  }

  [Test]
  public void TwoClientsKeepDifferentSnapshotsAndShareTheSameMatchmakingVersion() {
    var first = LSDFMenuLoadoutFixture.Selected(1, 2, 2, 1, 1);
    var second = LSDFMenuLoadoutFixture.Selected(0, 1, 1, 0, 0);
    var template = LSDFMenuLoadoutFixture.Template("menu-contract");
    var a = LSDFMenuLoadoutFixture.Prepare(template, first);
    var b = LSDFMenuLoadoutFixture.Prepare(template, second);
    CollectionAssert.AreEqual(first, a.RuntimePlayers[0].CommandSkills);
    CollectionAssert.AreEqual(second, b.RuntimePlayers[0].CommandSkills);
    Assert.AreEqual(a.AppVersion, b.AppVersion, "A personal skill selection is not a room compatibility version.");
    Assert.AreEqual(2, a.MaxPlayerCount);
    Assert.AreNotSame(a.RuntimePlayers[0], b.RuntimePlayers[0]);
    Assert.AreNotSame(a.RuntimePlayers[0].CommandSkills, b.RuntimePlayers[0].CommandSkills);
    first[4] = 0;
    Assert.AreEqual(2, a.RuntimePlayers[0].CommandSkills[4]);
    b.RuntimePlayers[0].CommandSkills[4] = 0;
    Assert.AreEqual(2, a.RuntimePlayers[0].CommandSkills[4]);
  }

  [Test]
  public void ReconnectPreservesTheSubmittedBuildInsteadOfReadingNewLocalPreferences() {
    var selected = LSDFMenuLoadoutFixture.Selected(1, 2, 2, 1, 1);
    var submitted = LSDFMenuLoadoutFixture.Prepare(LSDFMenuLoadoutFixture.Template("reconnect-contract"), selected);
    var reconnect = LSDFMenuLoadoutFixture.Prepare(submitted.RuntimePlayers[0], new int[28], reconnect: true);
    CollectionAssert.AreEqual(selected, reconnect.RuntimePlayers[0].CommandSkills);
  }

  private static AnimatorState Find(AnimatorGraph graph, string name) {
    foreach (var state in graph.Layers[0].States) if (state.Name == name) return state;
    return null;
  }

  private static string Destination(AnimatorState state, string variable, int value) {
    Assert.IsNotNull(state);
    foreach (var transition in state.Transitions)
      foreach (var condition in transition.Conditions)
        if (condition.VariableName == variable && condition.ThresholdInt == value)
          return transition.DestinationStateName;
    Assert.Fail($"No {variable}={value} destination in {state.Name}.");
    return null;
  }
}

internal static class LSDFMenuLoadoutFixture {
  public static int[] Selected(int neutralLp, int backLp, int downForwardRp, int forwardRp, int downBackRk) {
    var skills = new int[28];
    skills[0] = neutralLp; skills[4] = backLp; skills[17] = downForwardRp;
    skills[21] = forwardRp; skills[11] = downBackRk;
    return skills;
  }

  public static RuntimePlayer Template(string nickname) => new RuntimePlayer {
    PlayerNickname = nickname,
    PlayerAvatar = new AssetRef<EntityPrototype> { Id = new AssetGuid(1502267863199495006L) }
  };

  public static QuantumMenuConnectArgs Prepare(RuntimePlayer template, int[] selected, bool reconnect = false) {
    var menu = AssetDatabase.LoadAssetAtPath<QuantumMenuConfig>("Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
    var args = new QuantumMenuConnectArgs {
      AppVersion = "lsdf-menu-regression",
      RuntimeConfig = new QuantumUnityJsonSerializer().CloneConfig(menu.AvailableScenes[0].RuntimeConfig),
      RuntimePlayers = new[] { template },
      Reconnecting = reconnect
    };
    var existed = new bool[28];
    var previous = new int[28];
    try {
      for (int slot = 0; slot < 28; slot++) {
        string key = "Skill_" + slot;
        existed[slot] = PlayerPrefs.HasKey(key);
        previous[slot] = PlayerPrefs.GetInt(key, 0);
      }
      for (int slot = 0; slot < 28; slot++) PlayerPrefs.SetInt("Skill_" + slot, selected[slot]);
      var prepare = typeof(QuantumMenuConnectionBehaviour).GetMethod("PrepareLSDFPlayers", BindingFlags.NonPublic | BindingFlags.Static);
      if (prepare == null) throw new InvalidOperationException("Production menu loadout entry point is missing.");
      prepare.Invoke(null, new object[] { args });
      return args;
    } finally {
      for (int slot = 0; slot < 28; slot++) {
        string key = "Skill_" + slot;
        if (existed[slot]) PlayerPrefs.SetInt(key, previous[slot]); else PlayerPrefs.DeleteKey(key);
      }
    }
  }
}
