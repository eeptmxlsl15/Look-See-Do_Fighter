using System.Collections;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LSDFPlayer2TestInputTests {
  public static IEnumerable Commands {
    get {
      for (int direction = 0; direction < 7; direction++)
        for (int button = 0; button < 4; button++)
          yield return new TestCaseData((CommandDirection)direction, (CommandButton)button);
    }
  }

  [TestCaseSource(nameof(Commands))]
  public void All28CommandsReachTheExistingDirectionAndPressEdgeBuffer(CommandDirection direction, CommandButton button) {
    var input = LSDFPlayer2TestInputPlayback.CommandInput(direction, button);
    Assert.AreEqual(direction, LSDFInputBuffer.ReadDirection(input));
    Assert.AreEqual(1 << (int)button, LSDFInputBuffer.AttackButtons(input));
    var state = new LSDF_Player { playerHp = 170 };
    var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, input, state, 100, 10);
    Assert.IsTrue(LSDFInputBuffer.TryRoute(ref buffer, state, 100, 10, true, out var routedDirection, out var routedButton));
    Assert.AreEqual(direction, routedDirection); Assert.AreEqual(button, routedButton);
  }

  [Test]
  public void OnlyGlobal2PIsOverriddenAndOtherLocalPlayersDoNotRestartItsClock() {
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true; settings.InitialDelayTicks = 2;
      var playback = new LSDFPlayer2TestInputPlayback(); var game = new object();
      Assert.IsFalse(playback.TrySample(game, (PlayerRef)0, settings, true, 100, out _));
      Assert.IsFalse(playback.TrySample(game, PlayerRef.None, settings, true, 100, out _));
      Assert.IsTrue(playback.TrySample(game, (PlayerRef)1, settings, true, 100, out var waiting));
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(waiting));
      Assert.IsFalse(playback.TrySample(game, (PlayerRef)0, settings, true, 101, out _));
      Assert.IsTrue(playback.TrySample(game, (PlayerRef)1, settings, true, 102, out var attack));
      Assert.IsTrue(attack.LeftPunch);
      settings.Enabled = false;
      Assert.IsFalse(playback.TrySample(game, (PlayerRef)1, settings, true, 103, out _));
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void RepeatedCommandsReleaseButtonsAndDoNotMoveBetweenAttacks() {
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true; settings.Direction = CommandDirection.DownRight; settings.Button = CommandButton.RightPunch;
      settings.InitialDelayTicks = 0; settings.IntervalTicks = 4; settings.PressTicks = 1;
      var playback = new LSDFPlayer2TestInputPlayback(); var game = new object();
      var buffer = new LSDF_CommandBuffer(); var player = new LSDF_Player { playerHp = 170 };
      int edges = 0;
      for (int tick = 100; tick < 112; tick++) {
        Assert.IsTrue(playback.TrySample(game, (PlayerRef)1, settings, true, tick, out var input));
        bool attackTick = tick % 4 == 0;
        Assert.AreEqual(attackTick, (bool)input.RightPunch);
        Assert.AreEqual(attackTick, (bool)input.Down); Assert.AreEqual(attackTick, (bool)input.Right);
        LSDFInputBuffer.Capture(ref buffer, input, player, tick, 10);
        if (buffer.HasPending && buffer.InputTick == tick) { edges++; LSDFInputBuffer.Clear(ref buffer); }
      }
      Assert.AreEqual(3, edges, "Each cycle must create a fresh press edge.");
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void DelayBeginsAfterRoundUnlockAndStartsAgainForTheNextRound() {
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true; settings.InitialDelayTicks = 2;
      var playback = new LSDFPlayer2TestInputPlayback(); var game = new object();
      playback.TrySample(game, (PlayerRef)1, settings, false, 0, out var locked);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(locked));
      playback.TrySample(game, (PlayerRef)1, settings, true, 210, out var unlocked);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(unlocked));
      playback.TrySample(game, (PlayerRef)1, settings, true, 212, out var attack); Assert.IsTrue(attack.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, false, 300, out var ko);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(ko));
      playback.TrySample(game, (PlayerRef)1, settings, true, 510, out _);
      playback.TrySample(game, (PlayerRef)1, settings, true, 512, out attack); Assert.IsTrue(attack.LeftPunch);
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void OnceModeFinishesAndChangingSettingsGameOrActivationRestartsIt() {
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true; settings.Repeat = false; settings.InitialDelayTicks = 0;
      var playback = new LSDFPlayer2TestInputPlayback(); var game = new object();
      playback.TrySample(game, (PlayerRef)1, settings, true, 100, out var attack); Assert.IsTrue(attack.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, true, 101, out var released);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(released));
      playback.TrySample(game, (PlayerRef)1, settings, true, 1000, out released);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(released));
      settings.Button = CommandButton.RightKick;
      playback.TrySample(game, (PlayerRef)1, settings, true, 1001, out attack); Assert.IsTrue(attack.RightKick);
      playback.TrySample(new object(), (PlayerRef)1, settings, true, 0, out attack); Assert.IsTrue(attack.RightKick);
      playback.Reset();
      playback.TrySample(game, (PlayerRef)1, settings, true, 2000, out attack); Assert.IsTrue(attack.RightKick);
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void TickSamplingIsStableAndInvalidDurationsStillLeaveAReleaseTick() {
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true; settings.InitialDelayTicks = -1; settings.IntervalTicks = -1; settings.PressTicks = -1;
      var playback = new LSDFPlayer2TestInputPlayback(); var game = new object();
      playback.TrySample(game, (PlayerRef)1, settings, true, 100, out var first); Assert.IsTrue(first.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, true, 101, out var release);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(release));
      playback.TrySample(game, (PlayerRef)1, settings, true, 102, out var next); Assert.IsTrue(next.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, true, 100, out var same); Assert.AreEqual(first, same);
      settings.PressTicks = 5; settings.IntervalTicks = 1;
      playback.TrySample(game, (PlayerRef)1, settings, true, 200, out _);
      playback.TrySample(game, (PlayerRef)1, settings, true, 204, out var held); Assert.IsTrue(held.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, true, 205, out release);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(release));
      settings.Direction = (CommandDirection)99;
      playback.TrySample(game, (PlayerRef)1, settings, true, 206, out var invalid);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(invalid));
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void InputComponentLifecycleCallbacksStartAFreshSchedule() {
    var go = new GameObject("2p-input-lifetime", typeof(Quantum.Asteroids.LSDF_PlayerInput));
    var settings = ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    var component = go.GetComponent<Quantum.Asteroids.LSDF_PlayerInput>();
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var onEnable = typeof(Quantum.Asteroids.LSDF_PlayerInput).GetMethod("OnEnable", flags);
    var onDisable = typeof(Quantum.Asteroids.LSDF_PlayerInput).GetMethod("OnDisable", flags);
    try {
      // This is an EditMode fixture; invoke Unity lifecycle entry points explicitly.
      onEnable.Invoke(component, null);
      var playback = (LSDFPlayer2TestInputPlayback)typeof(Quantum.Asteroids.LSDF_PlayerInput)
        .GetField("_testPlayback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(component);
      settings.Enabled = true; settings.InitialDelayTicks = 0; settings.Repeat = false;
      var game = new object();
      playback.TrySample(game, (PlayerRef)1, settings, true, 100, out var first); Assert.IsTrue(first.LeftPunch);
      playback.TrySample(game, (PlayerRef)1, settings, true, 101, out var released);
      Assert.AreEqual(0, LSDFInputBuffer.AttackButtons(released));
      onDisable.Invoke(component, null); onEnable.Invoke(component, null);
      playback.TrySample(game, (PlayerRef)1, settings, true, 102, out var restarted); Assert.IsTrue(restarted.LeftPunch);
    } finally { onDisable.Invoke(component, null); Object.DestroyImmediate(go); Object.DestroyImmediate(settings); }
  }

  [Test]
  public void GameplayInputUsesTheDisabledSettingsAssetAndTheSameUpdaterAsTheCamera() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      Quantum.Asteroids.LSDF_PlayerInput input = null; LSDFSceneBindings bindings = null;
      foreach (var root in scene.GetRootGameObjects()) {
        foreach (var found in root.GetComponentsInChildren<Quantum.Asteroids.LSDF_PlayerInput>(true)) input = found;
        foreach (var found in root.GetComponentsInChildren<LSDFSceneBindings>(true)) bindings = found;
      }
      Assert.IsNotNull(input); Assert.IsNotNull(bindings);
      var settings = AssetDatabase.LoadAssetAtPath<LSDFPlayer2TestInputSettings>("Assets/QuantumUser/Resources/LSDF_Player2TestInput.asset");
      Assert.IsNotNull(settings); Assert.IsFalse(settings.Enabled);
      Assert.AreSame(settings, input.Player2TestInput); Assert.AreSame(bindings.Updater, input.ViewUpdater);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }
}
