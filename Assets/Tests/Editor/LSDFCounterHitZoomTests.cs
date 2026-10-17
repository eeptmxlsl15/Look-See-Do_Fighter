using System.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LSDFCounterHitZoomTests {
  [TestCase(false, 1)] [TestCase(false, -1)]
  [TestCase(true, 1)] [TestCase(true, -1)]
  public void BothPerspectivesCenterTheHitAndRestoreTheMovingFollowCamera(bool orthographic, int side) {
    var go = new GameObject("zoom-test", typeof(Camera));
    var camera = go.GetComponent<Camera>();
    var zoom = new LSDFCounterHitZoomPlayer();
    try {
      camera.orthographic = orthographic; camera.fieldOfView = 60; camera.orthographicSize = 2;
      camera.transform.SetPositionAndRotation(new Vector3(0, 0, -2 * side), Quaternion.Euler(0, side == 1 ? 0 : 180, 0));
      var settings = new LSDFCounterHitZoomSettings.Entry { Enabled = true, Magnification = 2, ZoomInSeconds = .05f, HoldSeconds = .1f, ReturnSeconds = .2f };
      var hit = new Vector3(.25f, .3f, 0);
      zoom.Begin(camera, settings, hit);
      var follow = new Vector3(.1f, 0, -2 * side);
      zoom.Advance(follow, .05f);
      Assert.IsTrue(zoom.IsPlaying);
      var screen = camera.WorldToViewportPoint(hit);
      Assert.That(screen.x, Is.EqualTo(.5f).Within(.0001));
      Assert.That(screen.y, Is.EqualTo(.5f).Within(.0001));
      Assert.That(camera.orthographicSize, Is.EqualTo(1).Within(.0001));
      Assert.That(Mathf.Tan(30 * Mathf.Deg2Rad) / Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad), Is.EqualTo(2).Within(.0001));
      var movedFollow = new Vector3(.8f, 0, -3 * side);
      zoom.Advance(movedFollow, 1);
      Assert.IsFalse(zoom.IsPlaying);
      Assert.AreEqual(movedFollow, camera.transform.position);
      Assert.AreEqual(60, camera.fieldOfView); Assert.AreEqual(2, camera.orthographicSize);
      Assert.AreEqual(Quaternion.Euler(0, side == 1 ? 0 : 180, 0), camera.transform.rotation);
    } finally { zoom.Reset(); Object.DestroyImmediate(go); }
  }

  [Test]
  public void RepeatedHitsDoNotStackZoomAndResetRestoresOriginalLens() {
    var go = new GameObject("zoom-repeat", typeof(Camera));
    var camera = go.GetComponent<Camera>(); var zoom = new LSDFCounterHitZoomPlayer();
    try {
      camera.fieldOfView = 70; camera.orthographicSize = 3;
      var settings = new LSDFCounterHitZoomSettings.Entry { Enabled = true, Magnification = 2, ZoomInSeconds = 0, HoldSeconds = 1 };
      var follow = new Vector3(0, 0, -1);
      zoom.Begin(camera, settings, Vector3.right); zoom.Advance(follow, .1f);
      float firstLens = camera.fieldOfView;
      zoom.Begin(camera, settings, Vector3.up); zoom.Advance(follow, .1f);
      Assert.AreEqual(firstLens, camera.fieldOfView);
      Assert.AreEqual(new Vector3(0, 1, -1), camera.transform.position);
      zoom.Reset(); zoom.Reset();
      Assert.AreEqual(70, camera.fieldOfView); Assert.AreEqual(3, camera.orthographicSize);
      Assert.AreEqual(follow, camera.transform.position);
    } finally { zoom.Reset(); Object.DestroyImmediate(go); }
  }

  [Test]
  public void SwitchingCamerasAndDisablingTheEffectRestoresBothLenses() {
    var a = new GameObject("zoom-a", typeof(Camera)); var b = new GameObject("zoom-b", typeof(Camera));
    var first = a.GetComponent<Camera>(); var second = b.GetComponent<Camera>();
    var zoom = new LSDFCounterHitZoomPlayer();
    try {
      first.fieldOfView = 60; second.fieldOfView = 75;
      var settings = new LSDFCounterHitZoomSettings.Entry { Enabled = true, ZoomInSeconds = 0, HoldSeconds = 1 };
      zoom.Begin(first, settings, Vector3.right); zoom.Advance(new Vector3(0, 0, -1), .1f);
      zoom.Begin(second, settings, Vector3.up); zoom.Advance(new Vector3(0, 0, 1), .1f);
      Assert.AreEqual(60, first.fieldOfView); Assert.Less(second.fieldOfView, 75);
      settings.Enabled = false; zoom.Begin(second, settings, Vector3.zero);
      Assert.AreEqual(75, second.fieldOfView); Assert.IsFalse(zoom.IsPlaying);
      settings.Enabled = true; settings.ZoomInSeconds = settings.HoldSeconds = settings.ReturnSeconds = 0;
      zoom.Begin(first, settings, Vector3.zero); Assert.IsFalse(zoom.IsPlaying);
    } finally { zoom.Reset(); Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
  }

  [Test]
  public void SceneDisableAndReenableRestoresTheLensWithoutKeepingAnOldImpulse() {
    var go = new GameObject("zoom-lifetime", typeof(Camera), typeof(LSDFSceneBindings));
    var camera = go.GetComponent<Camera>(); var binding = go.GetComponent<LSDFSceneBindings>();
    var zoom = (LSDFCounterHitZoomPlayer)typeof(LSDFSceneBindings).GetField("_counterZoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(binding);
    try {
      camera.fieldOfView = 60;
      var settings = new LSDFCounterHitZoomSettings.Entry { Enabled = true, ZoomInSeconds = 0, HoldSeconds = 1 };
      zoom.Begin(camera, settings, Vector3.right); zoom.Advance(new Vector3(0, 0, -1), .1f);
      binding.enabled = false;
      Assert.IsFalse(zoom.IsPlaying); Assert.AreEqual(60, camera.fieldOfView);
      binding.enabled = true;
      Assert.IsFalse(zoom.IsPlaying);
      zoom.Begin(camera, settings, Vector3.up); zoom.Advance(new Vector3(0, 0, -1), .1f);
      binding.enabled = false;
      Assert.IsFalse(zoom.IsPlaying); Assert.AreEqual(60, camera.fieldOfView);
    } finally { zoom.Reset(); Object.DestroyImmediate(go); }
  }

  [Test]
  public void MoveChecklistHasAll37AssetsAndOnlySelectedMovesPlay() {
    var source = AssetDatabase.LoadAssetAtPath<LSDFCounterHitZoomSettings>("Assets/QuantumUser/Resources/LSDF_CounterHitZoom.asset");
    Assert.IsNotNull(source); Assert.AreEqual(37, source.Attacks.Length);
    Assert.AreEqual(37, source.Attacks.Select(entry => entry.Attack.Guid).Distinct().Count());
    Assert.IsTrue(source.Attacks.All(entry => !entry.Enabled && entry.Attack != null));
    var settings = Object.Instantiate(source);
    try {
      var a = settings.Attacks[0]; var b = settings.Attacks[1];
      Assert.IsFalse(settings.TryGet(a.Attack.Guid, out _));
      a.Enabled = true;
      Assert.IsTrue(settings.TryGet(a.Attack.Guid, out var selected)); Assert.AreSame(a, selected);
      Assert.IsFalse(settings.TryGet(b.Attack.Guid, out _));
      Assert.IsFalse(settings.TryGet(default, out _));
      settings.Enabled = false; Assert.IsFalse(settings.TryGet(a.Attack.Guid, out _));
      Assert.IsFalse(source.Attacks[0].Enabled, "Tuning the detached test profile must not change the project asset.");
    } finally { Object.DestroyImmediate(settings); }
  }

  [Test]
  public void GameplaySceneWiresTheSingleSettingsAssetAndCameraChildrenStayUntouched() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      var bindings = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<LSDFSceneBindings>(true)).Single();
      Assert.AreSame(AssetDatabase.LoadAssetAtPath<LSDFCounterHitZoomSettings>("Assets/QuantumUser/Resources/LSDF_CounterHitZoom.asset"), bindings.CounterHitZoom);
      Assert.IsNotNull(bindings.Player1Camera.GetComponent<Camera>());
      Assert.IsNotNull(bindings.Player2Camera.GetComponent<Camera>());
      Assert.Greater(bindings.Player1Camera.transform.childCount, 0);
      Assert.Greater(bindings.Player2Camera.transform.childCount, 0);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }
}
