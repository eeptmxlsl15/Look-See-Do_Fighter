using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Quantum;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

public class LSDFCameraHitShakeTests {
  private static LSDF_Player Healthy => new LSDF_Player { playerHp = 170 };
  private static LSDF_Player Hit => new LSDF_Player { playerHp = 158, isHit = true, DelayFrame = 40 };
  private static void Start(LSDFCameraHitShake shake, bool hitSecond = false) {
    shake.Observe(default, Healthy, default, Healthy, 10);
    shake.Observe(default, hitSecond ? Healthy : Hit, default, hitSecond ? Hit : Healthy, 11);
  }

  [TestCase(false, 1)] [TestCase(false, -1)]
  [TestCase(true, 1)] [TestCase(true, -1)]
  public void EitherFighterShakesTheWholeCameraInScreenAxesAndRestoresItsPosition(bool hitSecond, int side) {
    var go = new GameObject("hit-camera", typeof(Camera));
    var camera = go.GetComponent<Camera>(); var shake = new LSDFCameraHitShake();
    var origin = new Vector3(.3f, .1f, -.8f * side);
    var rotation = Quaternion.Euler(0, side == 1 ? 0 : 180, 0);
    go.transform.SetPositionAndRotation(origin, rotation);
    try {
      Start(shake, hitSecond);
      shake.Advance(go.transform, .025f, .18f, .015f);
      var offset = go.transform.position - origin;
      Assert.IsTrue(shake.IsShaking);
      Assert.Greater(offset.magnitude, 0);
      Assert.LessOrEqual(offset.magnitude, .015f);
      Assert.That(Vector3.Dot(offset, go.transform.forward), Is.EqualTo(0).Within(.000001));
      Assert.AreEqual(rotation, go.transform.rotation);
      Assert.AreEqual(60, camera.fieldOfView);
      shake.Advance(go.transform, 1, .18f, .015f);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(origin, go.transform.position);
      shake.Observe(default, hitSecond ? Healthy : Hit, default, hitSecond ? Hit : Healthy, 11);
      Assert.IsFalse(shake.IsShaking, "Reading the same verified hit must not replay it.");
    } finally { shake.Reset(); Object.DestroyImmediate(go); }
  }

  [Test]
  public void ZeroDamageHitsRetriggerWithoutDriftAndGuardRecoveryOrRewindDoNotShake() {
    var go = new GameObject("hit-repeat"); var shake = new LSDFCameraHitShake();
    var origin = new Vector3(1, 2, -1); go.transform.position = origin;
    var player = Healthy;
    try {
      shake.Observe(default, player, default, Healthy, 10);
      player.isGuard = true; player.DelayFrame = 30;
      shake.Observe(default, player, default, Healthy, 11); Assert.IsFalse(shake.IsShaking);
      player.isGuard = false; player.isHit = true; player.DelayFrame = 40;
      shake.Observe(default, player, default, Healthy, 12);
      shake.Advance(go.transform, .03f, .18f, .015f);
      var firstOffset = go.transform.position - origin;
      player.DelayFrame = 45;
      shake.Observe(default, player, default, Healthy, 13);
      shake.Advance(go.transform, .03f, .18f, .015f);
      Assert.AreEqual(firstOffset, go.transform.position - origin);
      player.isHealing = true;
      shake.Observe(default, player, default, Healthy, 14);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(origin, go.transform.position);
      player = Hit;
      shake.Observe(default, Healthy, default, Healthy, 15);
      shake.Observe(default, player, default, Healthy, 16);
      shake.Advance(go.transform, .03f, .18f, .015f);
      shake.Observe(default, player, default, Healthy, 9);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(origin, go.transform.position);
    } finally { shake.Reset(); Object.DestroyImmediate(go); }
  }

  [TestCase(0, .015f)] [TestCase(.18f, 0)]
  public void ZeroSettingsStopTheShakeAndRestoreTheCamera(float duration, float strength) {
    var go = new GameObject("hit-disabled"); var shake = new LSDFCameraHitShake();
    go.transform.position = Vector3.back;
    try {
      Start(shake); shake.Advance(go.transform, .03f, .18f, .015f);
      shake.Advance(go.transform, .03f, duration, strength);
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(Vector3.back, go.transform.position);
    } finally { shake.Reset(); Object.DestroyImmediate(go); }
  }

  [Test]
  public void HitShakeComposesWithMovingCounterZoomAndIndependentHealthBarShake() {
    var go = new GameObject("hit-and-zoom", typeof(Camera));
    var bar = new GameObject("hit-health", typeof(RectTransform)).GetComponent<RectTransform>();
    var camera = go.GetComponent<Camera>(); var shake = new LSDFCameraHitShake();
    var zoom = new LSDFCounterHitZoomPlayer(); var healthShake = new LSDFHealthBarShake();
    var settings = new LSDFCounterHitZoomSettings.Entry { Enabled = true, Magnification = 2,
      ZoomInSeconds = 0, HoldSeconds = .5f, ReturnSeconds = .2f };
    var follow = Vector3.back;
    go.transform.position = follow;
    try {
      Start(shake);
      healthShake.Observe(bar, default, Healthy, 10); healthShake.Observe(bar, default, Hit, 11);
      zoom.Begin(camera, settings, new Vector3(.2f, .3f, 0));
      zoom.Advance(follow, .03f);
      var zoomPosition = go.transform.position; float zoomLens = camera.fieldOfView;
      shake.Advance(go.transform, .03f, .18f, .015f);
      healthShake.Advance(.03f, .22f, 20);
      Assert.AreNotEqual(zoomPosition, go.transform.position);
      Assert.IsTrue(healthShake.IsShaking); Assert.AreNotEqual(Vector2.zero, bar.anchoredPosition);
      Assert.AreEqual(zoomLens, camera.fieldOfView);
      shake.RemoveOffset(); Assert.AreEqual(zoomPosition, go.transform.position);
      var movingFollow = new Vector3(.5f, 0, -2);
      zoom.Advance(movingFollow, .03f); zoomPosition = go.transform.position;
      shake.Advance(go.transform, 1, .18f, .015f);
      Assert.IsFalse(shake.IsShaking); Assert.IsTrue(zoom.IsPlaying);
      Assert.AreEqual(zoomPosition, go.transform.position); Assert.AreEqual(zoomLens, camera.fieldOfView);
      Assert.IsTrue(healthShake.IsShaking, "The camera effect must not stop the health bar's independent effect.");
      zoom.Advance(movingFollow, 1);
      Assert.AreEqual(movingFollow, go.transform.position); Assert.AreEqual(60, camera.fieldOfView);
    } finally { shake.Reset(); zoom.Reset(); healthShake.Reset(); Object.DestroyImmediate(go); Object.DestroyImmediate(bar.gameObject); }
  }

  [Test]
  public void CameraSwitchAndSceneDisableRemoveOffsetsWithoutKeepingOldHits() {
    var first = new GameObject("hit-first", typeof(LSDFSceneBindings)); var second = new GameObject("hit-second");
    var binding = first.GetComponent<LSDFSceneBindings>();
    var shake = (LSDFCameraHitShake)typeof(LSDFSceneBindings).GetField("_cameraHitShake", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(binding);
    first.transform.position = Vector3.back; second.transform.position = Vector3.forward;
    try {
      Start(shake); shake.Advance(first.transform, .03f, .18f, .015f);
      shake.Advance(second.transform, .03f, .18f, .015f);
      Assert.AreEqual(Vector3.back, first.transform.position); Assert.AreEqual(Vector3.forward, second.transform.position);
      Assert.IsFalse(shake.IsShaking);
      Start(shake); shake.Advance(second.transform, .03f, .18f, .015f);
      binding.enabled = false;
      // EditMode fixtures invoke the runtime lifecycle explicitly.
      binding.SendMessage("OnDisable");
      Assert.IsFalse(shake.IsShaking); Assert.AreEqual(Vector3.forward, second.transform.position);
      binding.enabled = true; binding.SendMessage("OnEnable"); Assert.IsFalse(shake.IsShaking);
    } finally { binding.SendMessage("OnDisable"); shake.Reset(); Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
  }

  [Test]
  public void GameplaySceneEnablesHitShakeAlongsideTheExistingZoomProfileAndHudRoots() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      var binding = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<LSDFSceneBindings>(true)).Single();
      Assert.That(binding.HitShakeDuration, Is.EqualTo(.18f).Within(.000001));
      Assert.That(binding.HitShakeStrength, Is.EqualTo(.015f).Within(.000001));
      Assert.IsNotNull(binding.CounterHitZoom);
      Assert.IsNotNull(binding.Player1Camera.GetComponent<Camera>()); Assert.IsNotNull(binding.Player2Camera.GetComponent<Camera>());
      Assert.IsNotNull(binding.Player1Hud.LeftHealthShakeRoot); Assert.IsNotNull(binding.Player2Hud.RightHealthShakeRoot);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }
}
