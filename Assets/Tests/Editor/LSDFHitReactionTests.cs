using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.Addons.Animator;
using Quantum.LSDF;
using UnityEditor;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

/// <summary>Real Frame/graph regression at the hit boundary, without Photon or Unity views.</summary>
public class LSDFHitReactionTests {
  [UnityTest]
  public IEnumerator AirHitInterruptsNativeGraphsAndSurvivesFrameRestore() {
    yield return new EnterPlayMode();
    string error = null;
    var task = RunCases();
    while (!task.IsCompleted) yield return null;
    if (task.IsFaulted) error = task.Exception.ToString();
    yield return new ExitPlayMode();
    Assert.IsNull(error, error);
  }

  private static async Task RunCases() {
    QuantumRunnerUnityFactory.Init();
    var menu = AssetDatabase.LoadAssetAtPath<Quantum.Menu.QuantumMenuConfig>(
      "Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
    var config = new QuantumUnityJsonSerializer().CloneConfig(menu.AvailableScenes[0].RuntimeConfig);
    config.Seed = 12345;
    var simulation = QuantumUnityDB.GetGlobalAsset<SimulationConfig>(config.SimulationConfig);
    var previousSceneMode = simulation.AutoLoadSceneFromMap;
    QuantumRunner runner = null;
    Frame frame = null;
    var failures = new List<string>();
    int passed = 0;
    try {
      // Fixture-only bootstrap setting, restored after shutdown and never saved.
      simulation.AutoLoadSceneFromMap = SimulationConfig.AutoLoadSceneFromMapMode.Disabled;
      runner = QuantumRunner.StartGame(new SessionRunner.Arguments {
        RuntimeConfig = config,
        SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
        GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
        GameMode = DeterministicGameMode.Local,
        PlayerCount = 2,
        RunnerId = "LSDF-air-interruption-regression"
      });
      runner.IsSessionUpdateDisabled = true;
      for (int i = 0; i < 16 && runner.Game.Frames.Verified == null; ++i)
        runner.Session.Update(1.0 / 60);
      Assert.IsNotNull(runner.Game.Frames.Verified, "Local Quantum runner did not initialize.");
      byte[] initial = runner.Game.Frames.Verified.Serialize(DeterministicFrameSerializeMode.Blit);
      frame = runner.Game.CreateFrame();
      foreach (string graphName in new[] { "LSDF_PlayerAnimatorGraph", "LSDF_EnemyAnimatorGraph" }) {
        foreach (string attack in new[] { "0_Jap", "1_Jap", "1_JapJap" }) {
          foreach (bool counter in new[] { true, false }) {
            // None, a manual repeated press, and the actual 2P test input producer.
            for (int inputMode = 0; inputMode < 3; ++inputMode) {
              string label = $"{graphName}/{attack}/counter={counter}/input={inputMode}";
              try {
                frame.Deserialize(initial);
                RunCase(frame, graphName, attack, counter, inputMode, false);
                ++passed;
                TestContext.Progress.WriteLine("AIR INTERRUPT PASS: " + label);
              } catch (Exception ex) { failures.Add(label + ": " + ex.Message); }
            }
          }
        }
        try {
          frame.Deserialize(initial);
          RunCase(frame, graphName, "0_LeftUpper", true, 1, true);
          ++passed;
          TestContext.Progress.WriteLine("AIR INTERRUPT PASS: " + graphName + "/active fade");
        } catch (Exception ex) { failures.Add(graphName + "/active fade: " + ex.Message); }
      }
    } finally {
      frame?.Free();
      if (runner != null) await runner.ShutdownAsync();
      simulation.AutoLoadSceneFromMap = previousSceneMode;
    }
    TestContext.Progress.WriteLine($"AIR INTERRUPT CASES: {passed} passed, {failures.Count} failed.");
    Assert.IsEmpty(failures, string.Join("\n", failures));
  }

  private static unsafe void RunCase(Frame frame, string graphName, string attackName,
      bool counter, int inputMode, bool pendingFade) {
    var loaded = AssetDatabase.LoadAssetAtPath<AnimatorGraph>(
      "Assets/QuantumUser/Animators/" + graphName + ".asset");
    var graph = frame.FindAsset<AnimatorGraph>(loaded.Guid);
    EntityRef defender = Fighter(frame, graph, (PlayerRef)0, FP._0);
    EntityRef attacker = Fighter(frame, graph, (PlayerRef)1, FP._2);
    frame.Unsafe.GetPointer<LSDF_Player>(attacker)->BufferedNextAttack = true;
    frame.Unsafe.GetPointer<LSDF_CommandBuffer>(attacker)->HasPending = true;
    var animator = frame.Unsafe.GetPointer<AnimatorComponent>(defender);
    var player = frame.Unsafe.GetPointer<LSDF_Player>(defender);
    var layer = frame.ResolveList(animator->Layers).GetPointer(0);
    var attack = State(graph, attackName);
    layer->CurrentStateId = attack.Id;
    layer->Length = attack.GetLength(frame, layer);
    layer->Time = layer->Length;
    layer->LastTime = layer->Time - frame.DeltaTime;
    layer->NormalizedTime = FP._1;
    if (pendingFade) {
      // An ordinary fade has already won before the hit arrives.
      attack.FadeTo(frame, animator, layer, State(graph, "Go To Idle"), frame.DeltaTime, FP._0_25);
      Assert.AreNotEqual(0, layer->ToStateId);
    }
    player->isAttack = player->canCounter = true;
    player->BufferedNextAttack = inputMode != 0;
    AnimatorComponent.SetBoolean(frame, animator, "NextAttack", inputMode != 0);
    AnimatorComponent.SetBoolean(frame, animator, "YZero", true); // stale landing request
    AnimatorComponent.SetTrigger(frame, animator, "InputCommand");
    AnimatorComponent.SetTrigger(frame, animator, "Rp");
    var commands = frame.Unsafe.GetPointer<LSDF_CommandBuffer>(defender);
    var input = inputMode == 2 ? AutoInput() : new Quantum.Input { RightPunch = inputMode == 1 };
    LSDFInputBuffer.Capture(ref *commands, input, *player, frame.Number, 10);
    commands->Dispatching = true;
    var heldButtons = commands->PreviousAttackButtons;
    EntityRef ownHitbox = Hitbox(frame, defender);
    EntityRef otherHitbox = Hitbox(frame, attacker);
    byte[] before = frame.Serialize(DeterministicFrameSerializeMode.Blit);

    HitAndStep(frame, defender, attacker, counter);
    AssertReaction(frame, defender, attacker, graph, ownHitbox, otherHitbox, heldButtons);
    ulong expected = frame.CalculateChecksum();
    // Restore every mutable component/Animator list, then repeat the same boundary operation.
    frame.Deserialize(before);
    HitAndStep(frame, defender, attacker, counter);
    Assert.AreEqual(expected, frame.CalculateChecksum(), "Frame restore/resimulation checksum differs.");

    animator = frame.Unsafe.GetPointer<AnimatorComponent>(defender);
    player = frame.Unsafe.GetPointer<LSDF_Player>(defender);
    layer = frame.ResolveList(animator->Layers).GetPointer(0);
    FP previousTime = layer->Time;
    int entryCount = player->hitCount;
    Hit(frame, defender, attacker, false);
    Assert.AreEqual(previousTime, layer->Time, "Air rehit must not restart its entry window.");
    Step(frame, defender, graph);
    Assert.AreEqual(entryCount, player->hitCount, "Air entry was replayed on rehit.");
    Assert.IsTrue(frame.Get<LSDF_AirCombo>(defender).ComboActive);
    Assert.AreEqual(1, frame.Get<LSDF_AirCombo>(defender).HitCount);

    // Existing landing condition still routes Air -> Rising.
    AnimatorComponent.SetBoolean(frame, animator, "YZero", true);
    Step(frame, defender, graph);
    Assert.AreEqual("Rising", graph.GetState(layer->CurrentStateId).Name);
    var destroy = new LSDF_CollisionDestroySystem();
    var filter = new LSDF_CollisionDestroySystem.Filter {
      Entity = ownHitbox, TickToDestroy = frame.Unsafe.GetPointer<TickToDestroy>(ownHitbox)
    };
    destroy.Update(frame, ref filter);
    Assert.IsFalse(frame.Exists(ownHitbox), "Interrupted hitbox survived the normal cleanup step.");
    Assert.IsTrue(frame.Exists(otherHitbox), "Other fighter's hitbox was removed.");
  }

  private static unsafe EntityRef Fighter(Frame frame, AnimatorGraph graph, PlayerRef playerRef, FP x) {
    var entity = frame.Create();
    frame.Add(entity, new Transform2D { Position = new FPVector2(x, FP._0) });
    frame.Add(entity, new PhysicsCollider2D { Shape = Shape2D.CreateBox(new FPVector2(FP._0_50, FP._1)) });
    frame.Add(entity, new PhysicsBody2D { GravityScale = FP._1 });
    frame.Add(entity, new LSDF_Player { playerHp = 170 });
    frame.Add(entity, new PlayerLink { PlayerRef = playerRef });
    frame.Add(entity, new LSDF_CommandBuffer());
    frame.Add(entity, new AnimatorComponent { AnimatorGraph = graph.Guid });
    return entity;
  }

  private static EntityRef Hitbox(Frame frame, EntityRef owner) {
    var entity = frame.Create();
    frame.Add(entity, new LSDF_HitboxInfo { AttackerEntity = owner });
    frame.Add(entity, new TickToDestroy { TickToDestroyAt = frame.Number + 1 });
    return entity;
  }

  private static unsafe void Hit(Frame frame, EntityRef defender, EntityRef attacker, bool counter) {
    var hit = new LSDF_HitboxInfo {
      AttackerEntity = attacker, CountType = CountAttackType.Air,
      launcher = true, AttackType = HitboxAttackType.Mid,
      attackDamage = 10, enemyCountTime = 30, enemyHitTime = 30
    };
    var player = frame.Unsafe.GetPointer<LSDF_Player>(defender);
    var animator = frame.Unsafe.GetPointer<AnimatorComponent>(defender);
    // TriggerInfo2D is an engine-owned, read-only view into a physics contact.
    // Exercise the production launch/reaction operations at that boundary instead
    // of fabricating its private native pointers. Contact dispatch is outside this fixture.
    Assert.IsTrue(LSDFJuggle.OnHit(frame, defender, player, animator, &hit, counter));
    Assert.IsTrue(LSDFHitReactions.EnterAir(frame, defender, animator));
    player->isHit = true;
  }

  private static unsafe void HitAndStep(Frame frame, EntityRef defender, EntityRef attacker, bool counter) {
    Hit(frame, defender, attacker, counter);
    var animator = frame.Unsafe.GetPointer<AnimatorComponent>(defender);
    var graph = frame.FindAsset<AnimatorGraph>(animator->AnimatorGraph);
    Assert.AreEqual(State(graph, "Air").Id, frame.ResolveList(animator->Layers).GetPointer(0)->CurrentStateId,
      "The hit must commit Air before ordinary Animator transitions can be selected.");
    Step(frame, defender, graph);
  }

  private static unsafe void Step(Frame frame, EntityRef entity, AnimatorGraph graph) {
    var animator = frame.Unsafe.GetPointer<AnimatorComponent>(entity);
    graph.UpdateGraphState(frame, animator, frame.ResolveList(animator->Layers).GetPointer(0), 0, frame.DeltaTime);
  }

  private static unsafe void AssertReaction(Frame frame, EntityRef defender, EntityRef attacker,
      AnimatorGraph graph, EntityRef ownHitbox, EntityRef otherHitbox, int heldButtons) {
    var animator = frame.Unsafe.GetPointer<AnimatorComponent>(defender);
    var layer = frame.ResolveList(animator->Layers).GetPointer(0);
    var player = frame.Get<LSDF_Player>(defender);
    var commands = frame.Get<LSDF_CommandBuffer>(defender);
    Assert.AreEqual(State(graph, "Air").Id, layer->CurrentStateId);
    Assert.AreEqual(0, layer->ToStateId);
    Assert.IsTrue(player.isAir && player.isHit);
    Assert.IsFalse(LSDFInputBuffer.CanExecute(player, frame.Number));
    Assert.IsFalse(player.isAttack || player.canCounter || player.BufferedNextAttack);
    Assert.AreEqual(1, player.hitCount, "Air's time-zero event must run exactly once.");
    Assert.IsTrue(frame.Get<PhysicsBody2D>(defender).Velocity.Y > FP._0);
    Assert.IsFalse(commands.HasPending || commands.Dispatching);
    Assert.AreEqual(heldButtons, commands.PreviousAttackButtons, "Held-button history must survive cancellation.");
    Assert.IsFalse(AnimatorComponent.GetBoolean(frame, animator, "NextAttack"));
    Assert.IsFalse(AnimatorComponent.IsTriggerActive(frame, animator, "Rp"));
    Assert.AreEqual(frame.Number, frame.Get<TickToDestroy>(ownHitbox).TickToDestroyAt);
    Assert.IsTrue(frame.Exists(ownHitbox), "Do not destroy inside Physics callbacks; preserve same-tick trades.");
    Assert.AreEqual(frame.Number + 1, frame.Get<TickToDestroy>(otherHitbox).TickToDestroyAt);
    Assert.IsFalse(frame.Get<LSDF_Player>(attacker).isAir, "Shared graph leaked state between fighters.");
    Assert.IsTrue(frame.Get<LSDF_Player>(attacker).BufferedNextAttack);
    Assert.IsTrue(frame.Get<LSDF_CommandBuffer>(attacker).HasPending);
    int owned = 0;
    foreach (var pair in frame.GetComponentIterator<LSDF_HitboxInfo>())
      if (frame.Get<LSDF_HitboxInfo>(pair.Entity).AttackerEntity == defender) ++owned;
    Assert.AreEqual(1, owned, "An interrupted attack event spawned another hitbox.");
  }

  private static AnimatorState State(AnimatorGraph graph, string name) {
    foreach (var state in graph.Layers[0].States) if (state.Name == name) return state;
    throw new InvalidOperationException("Missing fixture graph state: " + name);
  }

  private static Quantum.Input AutoInput() {
    var settings = UnityEngine.ScriptableObject.CreateInstance<LSDFPlayer2TestInputSettings>();
    try {
      settings.Enabled = true;
      settings.Direction = CommandDirection.Neutral;
      settings.Button = CommandButton.RightPunch;
      settings.InitialDelayTicks = 0;
      settings.IntervalTicks = settings.PressTicks = 1;
      var producer = new LSDFPlayer2TestInputPlayback();
      Assert.IsTrue(producer.TrySample(new object(), (PlayerRef)1, settings, true, 100, out var input));
      Assert.IsTrue(input.RightPunch);
      return input;
    } finally { UnityEngine.Object.DestroyImmediate(settings); }
  }
}
