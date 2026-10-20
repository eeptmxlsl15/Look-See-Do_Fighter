using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.LSDF;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using Object = UnityEngine.Object;

public class LSDFHitEffectTests {
  private const string PrefabPath = "Assets/Assets Resources/Effect Prefabs/Red.prefab";

  [Test]
  public void GameplayWiresOneScenePresenterToTheRequestedPrefab() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      var presenter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LSDF_HitEffectView>(true)).Single();
      Assert.IsTrue(presenter.enabled);
      Assert.IsNotNull(presenter.Updater);
      Assert.IsNotNull(presenter.Updater.GetComponent<LSDFSceneBindings>());
      Assert.IsNull(presenter.GetComponent<QuantumEntityView>(), "Scene effects must activate without a simulated entity.");
      Assert.IsFalse(presenter.UseFindUpdater);
      AssertPrefabs(presenter);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }

  [Test]
  public void MenuRetainsTheNewReferencesWithoutAnotherActivePresenter() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Photon/QuantumMenu/LSDF_Menu.unity");
    try {
      var presenter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LSDF_HitEffectView>(true)).Single();
      Assert.IsFalse(presenter.enabled);
      AssertPrefabs(presenter);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }

  [Test]
  public void SpritePrefabKeepsItsAnimationAndRendersAboveBothFighters() {
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    Assert.IsNotNull(prefab);
    var sprite = prefab.GetComponent<SpriteRenderer>();
    Assert.IsNotNull(sprite.sprite);
    Assert.IsNotNull(sprite.sharedMaterial);
    Assert.AreEqual("Universal Render Pipeline/2D/Sprite-Unlit-Default", sprite.sharedMaterial.shader.name);
    Assert.Greater(sprite.sortingOrder, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuantumUser/Resources/LSDF_Player.prefab").GetComponent<SpriteRenderer>().sortingOrder);
    Assert.Greater(sprite.sortingOrder, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuantumUser/Resources/LSDF_Enemy.prefab").GetComponent<SpriteRenderer>().sortingOrder);
    Assert.Greater(Mathf.Abs(prefab.transform.localScale.x), 0);
    Assert.Greater(Mathf.Abs(prefab.transform.localScale.y), 0);
    Assert.Greater(Mathf.Abs(prefab.transform.localScale.z), 0);
    var animator = prefab.GetComponent<Animator>();
    Assert.IsNotNull(animator.runtimeAnimatorController);
    Assert.IsTrue(animator.runtimeAnimatorController.animationClips.Any(clip => clip.length > 0));
  }

  private static void AssertPrefabs(LSDF_HitEffectView presenter) {
    foreach (var prefab in new[] { presenter.HitParticle, presenter.GuardParticle, presenter.ParringParticle, presenter.CounterParticle }) {
      Assert.IsNotNull(prefab);
      StringAssert.StartsWith("Assets/Assets Resources/Effect Prefabs/", AssetDatabase.GetAssetPath(prefab));
    }
  }

  [UnityTest]
  public IEnumerator SceneActivationPresentsAndCleansPredictedEventsAcrossGames() {
    yield return new EnterPlayMode();
    string error = null;
    var task = RunPresentation();
    while (!task.IsCompleted) yield return null;
    if (task.IsFaulted) error = task.Exception.ToString();
    yield return new ExitPlayMode();
    Assert.IsNull(error, error);
  }

  private static async Task RunPresentation() {
    QuantumRunnerUnityFactory.Init();
    var menu = AssetDatabase.LoadAssetAtPath<Quantum.Menu.QuantumMenuConfig>("Assets/QuantumUser/Resources/LSDF_MenuConfig.asset");
    var config = new QuantumUnityJsonSerializer().CloneConfig(menu.AvailableScenes[0].RuntimeConfig);
    config.Seed = 12345;
    var simulation = QuantumUnityDB.GetGlobalAsset<SimulationConfig>(config.SimulationConfig);
    var sceneMode = simulation.AutoLoadSceneFromMap;
    QuantumRunner first = null, other = null;
    GameObject scope = null, root = null, duplicateRoot = null;
    bool gameplayLoaded = false;
    try {
      simulation.AutoLoadSceneFromMap = SimulationConfig.AutoLoadSceneFromMapMode.Disabled;
      first = Start(config, "LSDF-hit-effects-first");
      other = Start(config, "LSDF-hit-effects-other");
      // The actual updater synchronizes map-linked views too; provide the real map scene for that scope.
      var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/LSDFGameplay.unity", UnityEngine.SceneManagement.LoadSceneMode.Additive);
      while (!load.isDone) await Task.Yield();
      gameplayLoaded = true;
      CheckFlashMetadata(first.Game.Frames.Verified);
      CheckFlashFrame(first.Game);
      scope = new GameObject("effect-scope");
      var updater = scope.AddComponent<QuantumEntityViewUpdater>();
      updater.SetCurrentGame(first.Game);
      var bindings = scope.AddComponent<LSDFSceneBindings>();
      bindings.Updater = updater;
      bindings.Player1Camera = new GameObject("effect-first-camera", typeof(Camera));
      bindings.Player1Camera.transform.SetParent(scope.transform);
      bindings.Player1Camera.transform.position = new Vector3(0, 0, -1);
      bindings.Player2Camera = new GameObject("effect-second-camera", typeof(Camera));
      bindings.Player2Camera.transform.SetParent(scope.transform);
      bindings.Player2Camera.transform.position = new Vector3(0, 0, 1);
      bindings.Player2Camera.transform.rotation = Quaternion.Euler(0, 180, 0);
      bindings.Player2Camera.SetActive(false);
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
      root = Presenter("effects", updater, prefab);
      var view = root.GetComponent<LSDF_HitEffectView>();
      view.HitOpacity = .5f; view.GuardOpacity = .25f;
      view.ParringOpacity = .6f; view.CounterOpacity = .8f;
      PublishView(first.Game);
      Assert.IsTrue(view.IsActive, "The actual updater callback must activate this scene component without EntityView.");

      var hit = new EventOnHitEffect { Game = first.Game, position = new FPVector2(FP._0_25, FP._0_50) };
      QuantumEvent.Dispatcher.Publish(hit);
      QuantumEvent.Dispatcher.Publish(hit);
      QuantumEvent.Dispatcher.Publish(new EventOnHitEffect { Game = other.Game, position = FPVector2.Zero });
      Assert.AreEqual(1, root.transform.childCount, "Duplicate and foreign-game events must not create more effects.");
      var effect = root.transform.GetChild(0).gameObject;
      var animator = effect.GetComponent<Animator>();
      animator.speed = 0;
      Assert.AreEqual(new Vector3(.25f, .5f, 0), effect.transform.position);
      Assert.AreEqual(prefab.transform.localScale, effect.transform.localScale);
      float prefabAlpha = prefab.GetComponent<SpriteRenderer>().color.a;
      Assert.AreEqual(prefabAlpha * .5f, effect.GetComponent<SpriteRenderer>().color.a, .001f);
      view.HitOpacity = .2f;
      view.SendMessage("LateUpdate"); view.SendMessage("LateUpdate");
      Assert.AreEqual(prefabAlpha * .2f, effect.GetComponent<SpriteRenderer>().color.a, .001f, "Opacity must not multiply itself on repeated updates.");
      Assert.AreEqual(prefabAlpha, prefab.GetComponent<SpriteRenderer>().color.a, .001f);
      Assert.Less(Quaternion.Angle(bindings.Player1Camera.transform.rotation, effect.transform.rotation), .01f);
      bindings.Player1Camera.SetActive(false);
      bindings.Player2Camera.SetActive(true);
      view.SendMessage("LateUpdate");
      Assert.Less(Quaternion.Angle(bindings.Player2Camera.transform.rotation, effect.transform.rotation), .01f);

      var guard = new EventOnGuardEffect { Game = first.Game, position = FPVector2.Zero };
      QuantumEvent.Dispatcher.Publish(guard);
      var guarded = root.transform.GetChild(1).gameObject;
      guarded.GetComponent<Animator>().speed = 0;
      Assert.AreEqual(prefabAlpha * .25f, guarded.GetComponent<SpriteRenderer>().color.a, .001f);
      var wrongCancel = Callback<CallbackEventCanceled>(other.Game);
      wrongCancel.EventKey = LSDFPresentationLedger<GameObject>.Key(guard);
      QuantumCallback.Dispatcher.Publish(wrongCancel);
      Assert.IsTrue(guarded != null);
      var cancel = Callback<CallbackEventCanceled>(first.Game);
      cancel.EventKey = wrongCancel.EventKey;
      QuantumCallback.Dispatcher.Publish(cancel);
      await Task.Yield();
      await Task.Yield();
      Assert.IsTrue(guarded == null, "A canceled prediction must remove its visible instance.");
      var confirm = Callback<CallbackEventConfirmed>(first.Game);
      confirm.EventKey = LSDFPresentationLedger<GameObject>.Key(hit);
      QuantumCallback.Dispatcher.Publish(confirm);
      Assert.IsTrue(effect != null, "Confirmation must keep playback visible.");

      duplicateRoot = Presenter("duplicate-effects", updater, prefab);
      PublishView(first.Game);
      var parry = new EventOnParringEffect { Game = first.Game, position = new FPVector2(FP._1, FP._0) };
      QuantumEvent.Dispatcher.Publish(parry);
      Assert.AreEqual(0, duplicateRoot.transform.childCount, "Only one presenter may own this game's effects.");
      Assert.AreEqual(2, root.transform.childCount);
      Assert.AreEqual(prefabAlpha * .6f, root.transform.GetChild(1).GetComponent<SpriteRenderer>().color.a, .001f);
      duplicateRoot.SetActive(false);
      root.SetActive(false);
      await Task.Yield();
      await Task.Yield();
      Assert.IsTrue(effect == null, "Disabling must also clean confirmed effects.");
      Assert.AreEqual(0, root.transform.childCount);
      root.SetActive(true);
      PublishView(first.Game);
      var counter = new EventOnCounterEffect { Game = first.Game, position = FPVector2.Zero };
      QuantumEvent.Dispatcher.Publish(counter);
      Assert.AreEqual(1, root.transform.childCount, "Reactivation must have exactly one subscription.");
      var counterEffect = root.transform.GetChild(0).gameObject;
      Assert.AreEqual(prefabAlpha * .8f, counterEffect.GetComponent<SpriteRenderer>().color.a, .001f);
      var counterAnimator = counterEffect.GetComponent<Animator>();
      var firstSprite = counterEffect.GetComponent<SpriteRenderer>().sprite;
      int animationState = counterAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
      counterAnimator.Play(animationState, 0, .8f); counterAnimator.Update(0);
      view.SendMessage("LateUpdate");
      Assert.Less(counterAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);
      Assert.AreNotSame(firstSprite, counterEffect.GetComponent<SpriteRenderer>().sprite);
      counterAnimator.Play(animationState, 0, 1.1f); counterAnimator.Update(0);
      view.SendMessage("LateUpdate");
      await Task.Yield();
      await Task.Yield();
      Assert.IsTrue(counterEffect == null, "The supplied looping animation must play only one cycle.");

      QuantumEvent.Dispatcher.Publish(new EventOnHitEffect { Game = first.Game, position = new FPVector2(-FP._1, FP._0) });
      Assert.AreEqual(1, root.transform.childCount);
      updater.SetCurrentGame(other.Game);
      PublishView(other.Game);
      await Task.Yield();
      await Task.Yield();
      Assert.AreEqual(0, root.transform.childCount, "Changing games must clear all old playback.");
      QuantumEvent.Dispatcher.Publish(new EventOnGuardEffect { Game = first.Game, position = new FPVector2(-FP._1, FP._0) });
      Assert.AreEqual(0, root.transform.childCount);
      QuantumEvent.Dispatcher.Publish(new EventOnGuardEffect { Game = other.Game, position = new FPVector2(-FP._1, FP._0) });
      Assert.AreEqual(1, root.transform.childCount);
    } finally {
      if (root != null) Object.Destroy(root);
      if (duplicateRoot != null) Object.Destroy(duplicateRoot);
      if (scope != null) Object.Destroy(scope);
      if (other != null) await other.ShutdownAsync();
      if (first != null) await first.ShutdownAsync();
      if (gameplayLoaded) {
        var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Assets/Scenes/LSDFGameplay.unity");
        while (unload != null && !unload.isDone) await Task.Yield();
      }
      simulation.AutoLoadSceneFromMap = sceneMode;
    }
  }

  private static QuantumRunner Start(RuntimeConfig config, string id) {
    var runner = QuantumRunner.StartGame(new SessionRunner.Arguments {
      RuntimeConfig = config,
      SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
      GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
      GameMode = DeterministicGameMode.Local,
      PlayerCount = 2,
      HeapExtraCount = 1,
      RunnerId = id
    });
    runner.IsSessionUpdateDisabled = true;
    for (int i = 0; i < 16 && runner.Game.Frames.Verified == null; ++i) runner.Session.Update(1.0 / 60);
    Assert.IsNotNull(runner.Game.Frames.Verified);
    return runner;
  }

  private static GameObject Presenter(string name, QuantumEntityViewUpdater updater, GameObject prefab) {
    var root = new GameObject(name);
    root.SetActive(false);
    var view = root.AddComponent<LSDF_HitEffectView>();
    view.Updater = updater;
    view.MaximumLifetime = 10;
    view.HitParticle = view.GuardParticle = view.ParringParticle = view.CounterParticle = prefab;
    root.SetActive(true);
    return root;
  }

  private static void CheckFlashMetadata(Frame frame) {
    foreach (var graphName in new[] { "LSDF_PlayerAnimatorGraph", "LSDF_EnemyAnimatorGraph" }) {
      var source = AssetDatabase.LoadAssetAtPath<AnimatorGraph>("Assets/QuantumUser/Animators/" + graphName + ".asset");
      var graph = frame.FindAsset<AnimatorGraph>(source.Guid);
      var states = graph.Layers[0].States;
      Assert.AreEqual(LSDFActionFlashKind.Homing, LSDFActionFlash.Classify(frame, states.Single(state => state.Name == "0_LeftUpper")));
      Assert.AreEqual(LSDFActionFlashKind.None, LSDFActionFlash.Classify(frame, states.Single(state => state.Name == "0_Jap")));
      Assert.AreEqual(LSDFActionFlashKind.Parry, LSDFActionFlash.Classify(frame, states.Single(state => state.Name == "ParringStart")));
      Assert.AreEqual(LSDFActionFlashKind.Parry, LSDFActionFlash.Classify(frame, states.Single(state => state.Name == "Parring")));
    }
  }

  private static unsafe void CheckFlashFrame(QuantumGame game) {
    var frame = game.CreateFrame();
    using var flash = new LSDFActionFlash();
    try {
      frame.Deserialize(game.Frames.Verified.Serialize(DeterministicFrameSerializeMode.Blit));
      var source = AssetDatabase.LoadAssetAtPath<AnimatorGraph>("Assets/QuantumUser/Animators/LSDF_PlayerAnimatorGraph.asset");
      var graph = frame.FindAsset<AnimatorGraph>(source.Guid);
      var entity = frame.Create();
      frame.Add(entity, new LSDF_Player());
      frame.Add(entity, new PlayerLink { PlayerRef = (PlayerRef)0 });
      frame.Add(entity, new AnimatorComponent { AnimatorGraph = graph.Guid });
      var animator = frame.Unsafe.GetPointer<AnimatorComponent>(entity);
      animator->Self = entity;
      if (animator->Layers.Equals(default(Quantum.Collections.QListPtr<LayerData>))) graph.Initialise(frame, animator);
      var layer = frame.ResolveList(animator->Layers).GetPointer(0);
      layer->CurrentStateId = graph.Layers[0].States.Single(state => state.Name == "0_LeftUpper").Id;
      layer->ToStateId = 0; layer->Time = FP._0;
      flash.Observe(frame, entity); flash.Advance(.035f, .22f, .035f, .12f);
      Assert.AreEqual(.22f, flash.Advance(0, .22f, .035f, .12f), .0001f);
      layer->CurrentStateId = graph.Layers[0].States.Single(state => state.Name == "Idle").Id;
      flash.Observe(frame, entity);
      Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
      layer->CurrentStateId = graph.Layers[0].States.Single(state => state.Name == "ParringStart").Id;
      flash.Observe(frame, entity); flash.Advance(.035f, .22f, .035f, .12f);
      Assert.AreEqual(.22f, flash.Advance(0, .22f, .035f, .12f), .0001f);
      var player = frame.Get<LSDF_Player>(entity); player.isRoundEnd = true; frame.Set(entity, player);
      flash.Observe(frame, entity);
      Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
    } finally { frame.Free(); }
  }

  private static T Callback<T>(QuantumGame game) where T : Quantum.CallbackBase {
    return (T)Activator.CreateInstance(typeof(T), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { game }, null);
  }

  private static void PublishView(QuantumGame game) {
    QuantumCallback.Dispatcher.Publish(Callback<CallbackUpdateView>(game));
  }
}
