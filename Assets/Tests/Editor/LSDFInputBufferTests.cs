using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;
using UnityEditor;

public class LSDFInputBufferTests {
  private static LSDF_Player Ready() => new LSDF_Player { playerHp = 170 };
  private static bool Route(ref LSDF_CommandBuffer buffer, in LSDF_Player player, int tick, int window = 10,
      bool accepts = true) => LSDFInputBuffer.TryRoute(ref buffer, player, tick, window, accepts, out _, out _);

  [Test]
  public void DefaultWindowIsEditableAndWiredToTheExistingAsset() {
    var config = AssetDatabase.LoadAssetAtPath<LSDF_GameConfig>("Assets/QuantumUser/Resources/LSDF_GameConfig.asset");
    Assert.AreEqual(10, LSDFInputBuffer.DefaultWindowFrames);
    Assert.IsNotNull(config);
    Assert.GreaterOrEqual(config.InputBufferFrames, 0, "The live asset is editable, not a fixed default fixture.");
    var defaults = UnityEngine.ScriptableObject.CreateInstance<LSDF_GameConfig>();
    try { Assert.AreEqual(10, defaults.InputBufferFrames); }
    finally { UnityEngine.Object.DestroyImmediate(defaults); }
  }

  [TestCase(0, true)] [TestCase(9, true)] [TestCase(10, true)] [TestCase(11, false)]
  public void ReleasedCommandExecutesOnFirstUnlockedTickWithinTheInclusiveWindow(int age, bool expected) {
    var player = Ready(); player.isAttack = true;
    var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { Down = true, Right = true, LeftPunch = true }, player, 100, 10);
    Assert.IsFalse(Route(ref buffer, player, 100));
    player.isAttack = false;
    Assert.AreEqual(expected, LSDFInputBuffer.TryRoute(ref buffer, player, 100 + age, 10, true, out var direction, out var button));
    if (expected) {
      Assert.AreEqual(CommandDirection.DownRight, direction); Assert.AreEqual(CommandButton.LeftPunch, button);
      Assert.AreEqual(100, buffer.ActiveInputTick); Assert.AreEqual(100 + age, buffer.DispatchTick);
      Assert.IsFalse(buffer.HasPending);
    }
  }

  [TestCase(3, 3, true)] [TestCase(3, 4, false)] [TestCase(0, 0, true)]
  [TestCase(0, 1, false)] [TestCase(-1, 1, false)] [TestCase(20, 15, true)]
  public void ConfiguredWindowChangesTheActualExpiry(int window, int age, bool expected) {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftKick = true }, player, 100, window);
    Assert.AreEqual(expected, Route(ref buffer, player, 100 + age, window));
  }

  [Test]
  public void HoldingDoesNotRenewExpiredInputOrRepeatAnAttack() {
    var player = Ready(); player.isAttack = true;
    var buffer = new LSDF_CommandBuffer(); var held = new Input { LeftPunch = true };
    for (int tick = 100; tick <= 120; tick++) LSDFInputBuffer.Capture(ref buffer, held, player, tick, 10);
    Assert.AreEqual(100, buffer.InputTick); Assert.IsFalse(buffer.HasPending);
    player.isAttack = false;
    LSDFInputBuffer.Capture(ref buffer, held, player, 121, 10); Assert.IsFalse(Route(ref buffer, player, 121));
    LSDFInputBuffer.Capture(ref buffer, default, player, 122, 10);
    LSDFInputBuffer.Capture(ref buffer, held, player, 123, 10); Assert.IsTrue(Route(ref buffer, player, 123));
    player.isAttack = true; Assert.IsFalse(Route(ref buffer, player, 124));
    player.isAttack = false;
    LSDFInputBuffer.Capture(ref buffer, held, player, 140, 10); Assert.IsFalse(Route(ref buffer, player, 140));
  }

  [Test]
  public void LatestNewPressWinsAndSnapshotsDirectionInsteadOfReadingItAtRelease() {
    var player = Ready(); player.isAttack = true;
    var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true, Left = true }, player, 100, 10);
    // Holding LP while newly pressing RK must not choose the old LP again.
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true, RightKick = true, Down = true, Right = true }, player, 105, 10);
    LSDFInputBuffer.Capture(ref buffer, default, player, 106, 10);
    player.isAttack = false;
    Assert.IsTrue(LSDFInputBuffer.TryRoute(ref buffer, player, 107, 10, true, out var direction, out var button));
    Assert.AreEqual(CommandDirection.DownRight, direction); Assert.AreEqual(CommandButton.RightKick, button);
    Assert.AreEqual(105, buffer.ActiveInputTick);
  }

  [Test]
  public void SimultaneousNewButtonsKeepLegacyPriority() {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true, RightPunch = true, LeftKick = true, RightKick = true }, player, 100, 10);
    Assert.IsTrue(LSDFInputBuffer.TryRoute(ref buffer, player, 100, 10, true, out _, out var button));
    Assert.AreEqual(CommandButton.LeftPunch, button);
  }

  [TestCase("attack")] [TestCase("hit")] [TestCase("guard")] [TestCase("stun")]
  [TestCase("combo")] [TestCase("air")] [TestCase("wall")] [TestCase("cantMove")] [TestCase("tickLock")]
  public void LockedActionsCaptureButDoNotExecute(string reason) {
    var player = Ready();
    switch (reason) {
      case "attack": player.isAttack = true; break;
      case "hit": player.isHit = true; break;
      case "guard": player.isGuard = true; break;
      case "stun": player.isStun = true; break;
      case "combo": player.isCombo = true; break;
      case "air": player.isAir = true; break;
      case "wall": player.isWallHit = true; break;
      case "cantMove": player.isCantMove = true; break;
      case "tickLock": player.tickToEnableMove = 105; break;
    }
    var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { RightPunch = true }, player, 100, 10);
    Assert.IsTrue(buffer.HasPending); Assert.IsFalse(Route(ref buffer, player, 100));
    player = Ready(); Assert.IsTrue(Route(ref buffer, player, 105));
  }

  [TestCase(0, false)] [TestCase(-1, false)] [TestCase(170, true)]
  public void KoAndRoundRecoveryDiscardCommandsInsteadOfLeakingToTheNextRound(int health, bool roundEnd) {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true }, player, 100, 10);
    player.playerHp = health; player.isRoundEnd = roundEnd; player.tickToEnableMove = 110;
    LSDFInputBuffer.Capture(ref buffer, new Input { RightPunch = true }, player, 101, 10);
    Assert.IsFalse(buffer.HasPending); Assert.IsFalse(buffer.Dispatching);
    player = Ready(); Assert.IsFalse(Route(ref buffer, player, 110));
  }

  [Test]
  public void AOneTickPressRemainsLatchedOnlyThroughAnimatorRouting() {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true }, player, 100, 10);
    Assert.IsTrue(Route(ref buffer, player, 100)); Assert.IsFalse(buffer.HasPending);
    LSDFInputBuffer.Capture(ref buffer, default, player, 101, 10);
    Assert.IsTrue(Route(ref buffer, player, 101, accepts: false));
    Assert.AreEqual(100, buffer.DispatchTick); Assert.AreEqual(100, buffer.ActiveInputTick);
    player.isAttack = true; Assert.IsFalse(Route(ref buffer, player, 104)); Assert.IsFalse(buffer.Dispatching);
    player.isAttack = false; Assert.IsFalse(Route(ref buffer, player, 105));
  }

  [Test]
  public void ANewPendingCommandCannotOverwriteTheInFlightRoute() {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true }, player, 100, 10);
    Assert.IsTrue(Route(ref buffer, player, 100));
    LSDFInputBuffer.Capture(ref buffer, new Input { RightPunch = true }, player, 101, 10);
    Assert.IsTrue(LSDFInputBuffer.TryRoute(ref buffer, player, 101, 10, false, out _, out var button));
    Assert.AreEqual(CommandButton.LeftPunch, button); Assert.IsTrue(buffer.HasPending);
    Assert.AreEqual(101, buffer.InputTick); Assert.AreEqual(100, buffer.ActiveInputTick);
  }

  [Test]
  public void RoutingTimeoutAndInvalidCommandsDoNotLeaveStaleTriggers() {
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftPunch = true }, player, 100, 10);
    Assert.IsTrue(Route(ref buffer, player, 100));
    Assert.IsFalse(Route(ref buffer, player, 100 + LSDFInputBuffer.MaximumRoutingFrames + 1));
    buffer.HasPending = true; buffer.InputTick = 200; buffer.PendingDirection = 99;
    Assert.IsFalse(Route(ref buffer, player, 200)); Assert.IsFalse(buffer.HasPending);
  }

  [Test]
  public void FighterBuffersAndValueSnapshotsAreIndependent() {
    var player = Ready(); var first = new LSDF_CommandBuffer(); var second = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref first, new Input { RightKick = true }, player, 100, 10);
    var snapshot = first;
    Assert.IsFalse(Route(ref second, player, 101)); Assert.IsTrue(Route(ref first, player, 101));
    first = snapshot;
    Assert.IsTrue(Route(ref first, player, 101)); Assert.AreEqual(100, first.ActiveInputTick);
  }

  [Test]
  public void CommandsWaitForARealAnimatorInputTransition() {
    var graph = AssetDatabase.LoadAssetAtPath<AnimatorGraph>("Assets/QuantumUser/Animators/LSDF_PlayerAnimatorGraph.asset");
    bool idle = false, map = false;
    foreach (var state in graph.Layers[0].States) {
      if (state.Name == "Idle") { idle = true; Assert.IsTrue(LSDFCommandProcessor.HasCommandTransition(state)); }
      if (state.Name == "Command Map") { map = true; Assert.IsFalse(LSDFCommandProcessor.HasCommandTransition(state)); }
    }
    Assert.IsTrue(idle && map);
    var player = Ready(); var buffer = new LSDF_CommandBuffer();
    LSDFInputBuffer.Capture(ref buffer, new Input { LeftKick = true }, player, 100, 10);
    Assert.IsFalse(Route(ref buffer, player, 100, accepts: false)); Assert.IsTrue(buffer.HasPending);
    Assert.IsTrue(Route(ref buffer, player, 101));
  }
}
