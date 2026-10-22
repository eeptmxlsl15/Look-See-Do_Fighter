using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public sealed class LSDFStageBackgroundTests {
  private sealed class Fixture : IDisposable {
    public readonly GameObject Root = new GameObject("background-test");
    public readonly LSDFStageBackground Stage;
    public readonly Texture2D Texture;
    public readonly Sprite Sprite;
    public readonly BoxCollider2D Collider;
    public Fixture(Vector2? pivot = null) {
      Texture = new Texture2D(120, 40, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
      var pixels = new Color[120 * 40];
      for (int y = 0; y < 40; ++y) for (int x = 0; x < 120; ++x)
        pixels[y * 120 + x] = x < 40 ? Color.red : x < 80 ? Color.green : Color.blue;
      Texture.SetPixels(pixels); Texture.Apply();
      Sprite = Sprite.Create(Texture, new Rect(0, 0, 120, 40), pivot ?? new Vector2(.5f, .5f), 20, 0, SpriteMeshType.FullRect);
      Stage = Child("stage").AddComponent<LSDFStageBackground>();
      Stage.RepeatHorizontally = false; // Keep the original single-image projection contracts separate.
      Stage.Player1Camera = CreateCamera("first", -.8f, 0, 6);
      Stage.Player2Camera = CreateCamera("second", .8f, 180, 7);
      Stage.Player1Backdrop = Backdrop("first-backdrop", 6);
      Stage.Player2Backdrop = Backdrop("second-backdrop", 7);
      Stage.GroundVisual = Child("ground").AddComponent<SpriteRenderer>();
      Collider = Stage.GroundVisual.gameObject.AddComponent<BoxCollider2D>();
      Stage.BackgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
      Stage.BackgroundSprite = Sprite;
    }
    private GameObject Child(string name) {
      var child = new GameObject(name); child.transform.SetParent(Root.transform, false); return child;
    }
    private Camera CreateCamera(string name, float z, float yaw, int layer) {
      var camera = Child(name).AddComponent<Camera>();
      camera.enabled = false; camera.nearClipPlane = .01f; camera.farClipPlane = 100;
      camera.aspect = 16f / 9; camera.fieldOfView = 60;
      camera.transform.SetPositionAndRotation(new Vector3(0, 0, z), Quaternion.Euler(0, yaw, 0));
      camera.cullingMask = (1 << layer) | 1; return camera;
    }
    private SpriteRenderer Backdrop(string name, int layer) {
      var child = Child(name); child.transform.SetParent(Stage.transform, false); child.layer = layer;
      return child.AddComponent<SpriteRenderer>();
    }
    public void Dispose() { Object.DestroyImmediate(Root); Object.DestroyImmediate(Sprite); Object.DestroyImmediate(Texture); }
  }

  private static void AssertCovered(Camera camera, SpriteRenderer renderer) {
    Assert.IsTrue(renderer.enabled);
    var bounds = renderer.bounds;
    var low = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
    var high = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    for (int corner = 0; corner < 4; ++corner) {
      var p = camera.WorldToViewportPoint(new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
        (corner & 2) == 0 ? bounds.min.y : bounds.max.y, bounds.center.z));
      low = Vector2.Min(low, p); high = Vector2.Max(high, p);
      Assert.Greater(p.z, camera.nearClipPlane);
    }
    Assert.LessOrEqual(low.x, .001f); Assert.LessOrEqual(low.y, .001f);
    Assert.GreaterOrEqual(high.x, .999f); Assert.GreaterOrEqual(high.y, .999f);
  }

  [TestCase(4f / 3)] [TestCase(16f / 9)] [TestCase(21f / 9)]
  public void ReferenceFramingCoversBothOpposingCamerasWithoutStretching(float aspect) {
    using var f = new Fixture();
    f.Stage.transform.position = new Vector3(.4f, .2f, 0);
    f.Stage.Offset = new Vector2(-.15f, .1f);
    f.Stage.Player1Camera.aspect = f.Stage.Player2Camera.aspect = aspect;
    f.Stage.Player1Camera.transform.position = f.Stage.transform.TransformPoint(new Vector3(0, 0, -f.Stage.ReferenceCameraDistance));
    f.Stage.Player2Camera.transform.position = f.Stage.transform.TransformPoint(new Vector3(0, 0, f.Stage.ReferenceCameraDistance));
    f.Stage.Refresh();
    AssertCovered(f.Stage.Player1Camera, f.Stage.Player1Backdrop);
    AssertCovered(f.Stage.Player2Camera, f.Stage.Player2Backdrop);
    foreach (var renderer in new[] { f.Stage.Player1Backdrop, f.Stage.Player2Backdrop }) {
      Assert.AreEqual(3, renderer.bounds.size.x / renderer.bounds.size.y, .001f);
      Assert.AreSame(f.Stage.BackgroundMaterial, renderer.sharedMaterial);
      Assert.AreEqual(-100, renderer.sortingOrder);
    }
    Assert.AreEqual(0, f.Stage.Player1Camera.cullingMask & (1 << 7));
    Assert.AreEqual(0, f.Stage.Player2Camera.cullingMask & (1 << 6));
  }

  [Test]
  public void FollowZoomShakeAndViewSwitchDoNotChangeCameraOwnershipOrPose() {
    using var f = new Fixture();
    for (int i = 0; i < 4; ++i) {
      var camera = i % 2 == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
      int sign = i % 2 == 0 ? -1 : 1;
      camera.gameObject.SetActive(true);
      (i % 2 == 0 ? f.Stage.Player2Camera : f.Stage.Player1Camera).gameObject.SetActive(false);
      var position = new Vector3((i - 2) * .6f, .015f, sign * (.7f + i));
      camera.transform.position = position;
      var rotation = camera.transform.rotation; int mask = camera.cullingMask;
      f.Stage.Refresh();
      Assert.IsTrue((sign < 0 ? f.Stage.Player1Backdrop : f.Stage.Player2Backdrop).enabled);
      Assert.AreEqual(position, camera.transform.position); Assert.AreEqual(rotation, camera.transform.rotation);
      Assert.AreEqual(mask, camera.cullingMask); Assert.AreEqual(60, camera.fieldOfView);
      Assert.IsTrue(camera.gameObject.activeSelf); Assert.IsFalse(camera.enabled);
    }
    f.Stage.Player1Camera.orthographic = f.Stage.Player2Camera.orthographic = true;
    f.Stage.Player1Camera.orthographicSize = f.Stage.Player2Camera.orthographicSize = 2;
    f.Stage.Refresh(); Assert.IsTrue(f.Stage.Player1Backdrop.enabled);
    Assert.IsTrue(f.Stage.Player2Backdrop.enabled);
  }

  private static Vector3[] Landmarks(SpriteRenderer renderer) {
    var bounds = renderer.bounds;
    // SpriteRenderer bounds include a Z thickness (0.2 for the current sprites), but the drawn vertices are XY.
    // Compare points on the actual image plane instead of the front/back faces of that bounding volume.
    return new[] { new Vector3(bounds.min.x, bounds.min.y, bounds.center.z),
      new Vector3(bounds.max.x, bounds.max.y, bounds.center.z), bounds.center };
  }

  [TestCase(0, false)] [TestCase(1, false)] [TestCase(0, true)] [TestCase(1, true)]
  public void BackgroundLandmarksAndFightersShareTheSameZoomPanAndShake(int view, bool orthographic) {
    using var f = new Fixture();
    var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
    var renderer = view == 0 ? f.Stage.Player1Backdrop : f.Stage.Player2Backdrop;
    int side = view == 0 ? -1 : 1;
    f.Stage.Player1Camera.orthographic = f.Stage.Player2Camera.orthographic = orthographic;
    f.Stage.Player1Camera.orthographicSize = f.Stage.Player2Camera.orthographicSize = 2;
    f.Stage.Offset = new Vector2(.2f, -.1f);
    f.Stage.Refresh();
    var plane = new Plane(Vector3.forward, Vector3.zero);
    var virtualPoints = Landmarks(renderer);
    for (int i = 0; i < virtualPoints.Length; ++i) {
      var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(virtualPoints[i]));
      Assert.IsTrue(plane.Raycast(ray, out float hit));
      virtualPoints[i] = ray.GetPoint(hit);
    }
    float initialMapWidth = Mathf.Abs(camera.WorldToViewportPoint(virtualPoints[1]).x - camera.WorldToViewportPoint(virtualPoints[0]).x);
    float initialFighterWidth = Mathf.Abs(camera.WorldToViewportPoint(Vector3.right * .1f).x - camera.WorldToViewportPoint(Vector3.left * .1f).x);
    for (int step = 0; step < 4; ++step) {
      camera.transform.position = new Vector3(step == 3 ? 0 : step * .17f, step == 3 ? 0 : .015f * step,
        side * (step == 3 ? .8f : .8f + step));
      camera.fieldOfView = step == 1 ? 50 : 60;
      camera.orthographicSize = step == 1 ? 3 : 2;
      f.Stage.Refresh();
      var actual = Landmarks(renderer);
      for (int i = 0; i < actual.Length; ++i) {
        var expectedScreen = camera.WorldToViewportPoint(virtualPoints[i]);
        var actualScreen = camera.WorldToViewportPoint(actual[i]);
        Assert.AreEqual(expectedScreen.x, actualScreen.x, .001f, "Background X must stay locked to the fighter plane.");
        Assert.AreEqual(expectedScreen.y, actualScreen.y, .001f, "Background Y must share the fighters' camera shake and zoom.");
      }
      float mapWidth = Mathf.Abs(camera.WorldToViewportPoint(actual[1]).x - camera.WorldToViewportPoint(actual[0]).x);
      float fighterWidth = Mathf.Abs(camera.WorldToViewportPoint(Vector3.right * .1f).x - camera.WorldToViewportPoint(Vector3.left * .1f).x);
      Assert.AreEqual(fighterWidth / initialFighterWidth, mapWidth / initialMapWidth, .001f);
      if (step == 1) Assert.Less(mapWidth, initialMapWidth, "Zooming out must shrink the map instead of refilling the viewport.");
    }
  }

  [Test]
  public void CustomPivotAndOptionalMirrorKeepTheSameWorldLandmarks() {
    using var f = new Fixture(new Vector2(.1f, .8f));
    f.Stage.Refresh();
    Assert.Less(Vector3.Distance(new Vector3(0, 0, f.Stage.Depth), f.Stage.Player1Backdrop.bounds.center), .001f);
    Assert.Less(Vector3.Distance(new Vector3(0, 0, -f.Stage.Depth), f.Stage.Player2Backdrop.bounds.center), .001f);
    Assert.IsTrue(f.Stage.Player2Backdrop.flipX);
    f.Stage.MirrorPlayer2 = false; f.Stage.Refresh();
    Assert.IsFalse(f.Stage.Player2Backdrop.flipX);
    Assert.Less(Vector3.Distance(new Vector3(0, 0, -f.Stage.Depth), f.Stage.Player2Backdrop.bounds.center), .001f);
    Assert.AreEqual(FilterMode.Point, f.Texture.filterMode);
  }

  [Test]
  public void ImageRemovalDisableAndReentryRestoreOnlyTheOriginalGroundVisual() {
    using var f = new Fixture();
    f.Stage.Refresh(); Assert.IsFalse(f.Stage.GroundVisual.enabled);
    Assert.IsTrue(f.Collider.enabled); Assert.IsTrue(f.Collider.gameObject.activeInHierarchy);
    f.Stage.BackgroundSprite = null; f.Stage.Refresh();
    Assert.IsTrue(f.Stage.GroundVisual.enabled); Assert.IsFalse(f.Stage.Player1Backdrop.enabled);
    f.Stage.BackgroundSprite = f.Sprite; f.Stage.Refresh();
    f.Stage.enabled = false; Assert.IsTrue(f.Stage.GroundVisual.enabled);
    f.Stage.enabled = true; Assert.IsFalse(f.Stage.GroundVisual.enabled);
    f.Stage.HideGroundVisual = false; f.Stage.Refresh(); Assert.IsTrue(f.Stage.GroundVisual.enabled);
    f.Stage.GroundVisual.enabled = false; f.Stage.HideGroundVisual = true; f.Stage.Refresh();
    f.Stage.BackgroundSprite = null; f.Stage.Refresh(); Assert.IsFalse(f.Stage.GroundVisual.enabled);
    Assert.AreEqual(2, f.Stage.transform.childCount);
  }

  [Test]
  public void MissingCamerasAndSeparateStagesCannotLeakPresentationState() {
    using var a = new Fixture(); using var b = new Fixture();
    a.Stage.Refresh(); b.Stage.Refresh();
    a.Stage.Player1Camera = null; a.Stage.Player2Camera = null; a.Stage.Refresh();
    Assert.IsFalse(a.Stage.Player1Backdrop.enabled); Assert.IsFalse(a.Stage.Player2Backdrop.enabled);
    Assert.IsTrue(a.Stage.GroundVisual.enabled);
    Assert.IsTrue(b.Stage.Player1Backdrop.enabled); Assert.IsFalse(b.Stage.GroundVisual.enabled);
    Assert.AreSame(b.Sprite, b.Stage.Player2Backdrop.sprite);
    b.Stage.BackgroundMaterial = null; b.Stage.Refresh();
    Assert.IsTrue(b.Stage.GroundVisual.enabled); Assert.IsFalse(b.Stage.Player2Backdrop.enabled);
  }

  [Test]
  public void SceneWiresOneSharedImageSlotAndSeparateCameraLayers() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      var stage = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LSDFStageBackground>(true)).Single();
      var bindings = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LSDFSceneBindings>(true)).Single();
      Assert.AreSame(bindings.Player1Camera.GetComponent<Camera>(), stage.Player1Camera);
      Assert.AreSame(bindings.Player2Camera.GetComponent<Camera>(), stage.Player2Camera);
      Assert.AreEqual(6, stage.Player1Backdrop.gameObject.layer); Assert.AreEqual(7, stage.Player2Backdrop.gameObject.layer);
      Assert.AreNotEqual(0, stage.Player1Camera.cullingMask & (1 << 6)); Assert.AreEqual(0, stage.Player1Camera.cullingMask & (1 << 7));
      Assert.AreNotEqual(0, stage.Player2Camera.cullingMask & (1 << 7)); Assert.AreEqual(0, stage.Player2Camera.cullingMask & (1 << 6));
      Assert.AreEqual(stage.transform, stage.Player1Backdrop.transform.parent);
      Assert.AreEqual(stage.transform, stage.Player2Backdrop.transform.parent);
      Assert.IsTrue(stage.RepeatHorizontally);
      foreach (var tile in Tiles(stage, 0)) {
        Assert.IsNotNull(tile); Assert.AreEqual(6, tile.gameObject.layer);
        Assert.AreEqual(stage.transform, tile.transform.parent);
      }
      foreach (var tile in Tiles(stage, 1)) {
        Assert.IsNotNull(tile); Assert.AreEqual(7, tile.gameObject.layer);
        Assert.AreEqual(stage.transform, tile.transform.parent);
      }
      Assert.AreEqual(6, stage.GetComponentsInChildren<SpriteRenderer>(true).Length);
      Assert.IsNotNull(stage.GroundVisual); Assert.IsTrue(stage.GroundVisual.gameObject.activeSelf);
      Assert.AreEqual("Universal Render Pipeline/2D/Sprite-Unlit-Default", stage.BackgroundMaterial.shader.name);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }

  private static SpriteRenderer[] Tiles(LSDFStageBackground stage, int view) => view == 0
    ? new[] { stage.Player1Backdrop, stage.Player1LeftBackdrop, stage.Player1RightBackdrop }
    : new[] { stage.Player2Backdrop, stage.Player2LeftBackdrop, stage.Player2RightBackdrop };

  private static Vector3 OnStage(Camera camera, Vector3 point) {
    var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(point));
    Assert.IsTrue(new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float hit));
    return ray.GetPoint(hit);
  }

  private static void AssertHorizontalCoverage(Camera camera, SpriteRenderer[] tiles) {
    var ranges = tiles.Select(tile => {
      Assert.IsTrue(tile.enabled);
      var bounds = tile.bounds;
      float a = camera.WorldToViewportPoint(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z)).x;
      float b = camera.WorldToViewportPoint(new Vector3(bounds.max.x, bounds.center.y, bounds.center.z)).x;
      return new Vector2(Mathf.Min(a, b), Mathf.Max(a, b));
    }).OrderBy(range => range.x).ToArray();
    Assert.LessOrEqual(ranges[0].x, .001f, "Left viewport edge must remain covered.");
    Assert.GreaterOrEqual(ranges[2].y, .999f, "Right viewport edge must remain covered.");
    for (int i = 1; i < 3; ++i)
      Assert.AreEqual(ranges[i - 1].y, ranges[i].x, .001f, "Adjacent tiles must meet without gaps or overlaps.");
  }

  [TestCase(0, false)] [TestCase(1, false)] [TestCase(0, true)] [TestCase(1, true)]
  public void ThreeTilesCoverContinuousPanReverseAndLargeJumpsWithoutGrowingThePool(int view, bool orthographic) {
    using var f = new Fixture();
    f.Stage.RepeatHorizontally = true; f.Stage.FillCameraView = false; f.Stage.MinimumHeight = 2;
    f.Stage.Player1Camera.orthographic = f.Stage.Player2Camera.orthographic = orthographic;
    f.Stage.Player1Camera.orthographicSize = f.Stage.Player2Camera.orthographicSize = 2;
    f.Stage.Offset = new Vector2(.2f, -.1f); f.Stage.Refresh();
    var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
    var tiles = Tiles(f.Stage, view);
    var ids = f.Stage.GetComponentsInChildren<SpriteRenderer>().Select(tile => tile.GetInstanceID()).OrderBy(id => id).ToArray();
    float width = OnStage(camera, new Vector3(tiles[0].bounds.max.x, 0, tiles[0].bounds.center.z)).x -
      OnStage(camera, new Vector3(tiles[0].bounds.min.x, 0, tiles[0].bounds.center.z)).x;
    foreach (float step in new[] { 0, .49f, .51f, 1.01f, 7.1f, -.51f, -7.1f, 1000.2f, -1000.2f, 0 }) {
      var position = camera.transform.position; position.x = step * width; camera.transform.position = position;
      f.Stage.Refresh(); AssertHorizontalCoverage(camera, tiles);
      Assert.AreEqual(position, camera.transform.position);
      foreach (var tile in tiles) {
        Assert.AreEqual(view == 0 ? 6 : 7, tile.gameObject.layer);
        Assert.AreEqual(view == 1, tile.flipX);
        Assert.AreSame(f.Sprite, tile.sprite); Assert.AreSame(f.Stage.BackgroundMaterial, tile.sharedMaterial);
      }
    }
    CollectionAssert.AreEqual(ids, f.Stage.GetComponentsInChildren<SpriteRenderer>().Select(tile => tile.GetInstanceID()).OrderBy(id => id).ToArray());
    Assert.AreEqual(6, ids.Length);
  }

  [TestCase(0)] [TestCase(1)]
  public void CrossingOneTileBoundaryMovesOnlyTheOffscreenTile(int view) {
    using var f = new Fixture();
    f.Stage.RepeatHorizontally = true; f.Stage.FillCameraView = false; f.Stage.MinimumHeight = 2; f.Stage.Refresh();
    var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
    var tiles = Tiles(f.Stage, view);
    float width = OnStage(camera, new Vector3(tiles[0].bounds.max.x, 0, tiles[0].bounds.center.z)).x -
      OnStage(camera, new Vector3(tiles[0].bounds.min.x, 0, tiles[0].bounds.center.z)).x;
    camera.transform.position += Vector3.right * (.49f * width); f.Stage.Refresh();
    var before = tiles.Select(tile => OnStage(camera, tile.bounds.center).x).ToArray();
    camera.transform.position += Vector3.right * (.02f * width); f.Stage.Refresh();
    var deltas = tiles.Select((tile, i) => Mathf.Abs(OnStage(camera, tile.bounds.center).x - before[i])).OrderBy(delta => delta).ToArray();
    Assert.AreEqual(0, deltas[0], .001f); Assert.AreEqual(0, deltas[1], .001f);
    Assert.AreEqual(3 * width, deltas[2], .001f);
    AssertHorizontalCoverage(camera, tiles);
  }

  [TestCase(0, false)] [TestCase(1, false)] [TestCase(0, true)] [TestCase(1, true)]
  public void WideZoomAddsWholeRepeatsInsideThreeStripsWithoutStretchingTheImage(int view, bool orthographic) {
    using var f = new Fixture(new Vector2(.1f, .8f));
    f.Stage.RepeatHorizontally = true; f.Stage.FillCameraView = false; f.Stage.MinimumHeight = 2;
    f.Stage.Player1Camera.orthographic = f.Stage.Player2Camera.orthographic = orthographic;
    f.Stage.Player1Camera.orthographicSize = f.Stage.Player2Camera.orthographicSize = 2;
    f.Stage.Refresh();
    var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
    var tiles = Tiles(f.Stage, view);
    float originalWidth = Mathf.Abs(camera.WorldToViewportPoint(tiles[0].bounds.max).x - camera.WorldToViewportPoint(tiles[0].bounds.min).x);
    foreach (float zoom in new[] { 5, 20, 1 }) {
      camera.transform.position = new Vector3(37, 0, (view == 0 ? -1 : 1) * (.8f + zoom));
      camera.orthographicSize = 2 * (1 + zoom); f.Stage.Refresh();
      AssertHorizontalCoverage(camera, tiles);
      foreach (var tile in tiles) {
        int columns = Mathf.RoundToInt(tile.size.x / f.Sprite.bounds.size.x);
        Assert.Greater(columns, 1); Assert.AreEqual(1, columns % 2);
        Assert.AreEqual(SpriteDrawMode.Tiled, tile.drawMode);
        Assert.AreEqual(SpriteTileMode.Continuous, tile.tileMode);
        Assert.AreEqual(f.Sprite.bounds.size.y, tile.size.y, .001f);
        float unitWidth = Mathf.Abs(OnStage(camera, new Vector3(tile.bounds.max.x, 0, tile.bounds.center.z)).x -
          OnStage(camera, new Vector3(tile.bounds.min.x, 0, tile.bounds.center.z)).x) / columns;
        Assert.AreEqual(orthographic ? 6 : 1, unitWidth, .001f, "World-space map scale must not change with zoom.");
        float projectedWidth = Mathf.Abs(camera.WorldToViewportPoint(new Vector3(tile.bounds.max.x, 0, tile.bounds.center.z)).x -
          camera.WorldToViewportPoint(new Vector3(tile.bounds.min.x, 0, tile.bounds.center.z)).x) / columns;
        Assert.Less(projectedWidth, originalWidth);
      }
    }
    Assert.AreEqual(6, f.Stage.GetComponentsInChildren<SpriteRenderer>().Length);
  }

  [Test]
  public void AuthoredDuplicatesAreAdoptedWithoutCreatingOrDeletingSceneObjects() {
    using var f = new Fixture();
    foreach (var source in new[] { f.Stage.Player1Backdrop, f.Stage.Player2Backdrop }) {
      for (int i = 1; i <= 2; ++i) {
        var child = new GameObject(source.name + " (" + i + ")") { layer = source.gameObject.layer };
        child.transform.SetParent(f.Stage.transform, false);
        child.AddComponent<SpriteRenderer>().sprite = f.Sprite;
      }
    }
    var ids = f.Stage.GetComponentsInChildren<SpriteRenderer>().Select(tile => tile.GetInstanceID()).OrderBy(id => id).ToArray();
    f.Stage.RepeatHorizontally = true; f.Stage.Refresh();
    CollectionAssert.AreEqual(ids, Tiles(f.Stage, 0).Concat(Tiles(f.Stage, 1)).Select(tile => tile.GetInstanceID()).OrderBy(id => id).ToArray());
    var stageObject = f.Stage.gameObject;
    Object.DestroyImmediate(f.Stage);
    Assert.AreEqual(6, stageObject.transform.childCount);
  }

  [Test]
  public void RepeatingTilesRestoreGroundDisableAndReenterWithoutLeakingOwnedObjects() {
    using var f = new Fixture();
    f.Stage.RepeatHorizontally = true; f.Stage.Refresh();
    var all = Tiles(f.Stage, 0).Concat(Tiles(f.Stage, 1)).ToArray();
    f.Stage.BackgroundSprite = null; f.Stage.Refresh();
    Assert.IsTrue(all.All(tile => !tile.enabled)); Assert.IsTrue(f.Stage.GroundVisual.enabled);
    f.Stage.BackgroundSprite = f.Sprite; f.Stage.Refresh();
    f.Stage.enabled = false; Assert.IsTrue(all.All(tile => !tile.enabled));
    Assert.IsTrue(f.Stage.GroundVisual.enabled);
    f.Stage.enabled = true; f.Stage.Refresh(); Assert.IsTrue(all.All(tile => tile.enabled));
    f.Stage.Player1Camera = null; f.Stage.Refresh();
    Assert.IsTrue(Tiles(f.Stage, 0).All(tile => !tile.enabled));
    Assert.IsTrue(Tiles(f.Stage, 1).All(tile => tile.enabled));
    f.Stage.RepeatHorizontally = false; f.Stage.Refresh();
    Assert.IsFalse(f.Stage.Player2LeftBackdrop.enabled); Assert.IsFalse(f.Stage.Player2RightBackdrop.enabled);
    var stageObject = f.Stage.gameObject;
    Object.DestroyImmediate(f.Stage);
    Assert.AreEqual(2, stageObject.transform.childCount, "Only generated side tiles are owned and destroyed by the component.");
  }

  [TestCase(0, .8f)] [TestCase(1, .8f)] [TestCase(0, 8.8f)] [TestCase(1, 8.8f)]
  public void RepeatingSeamsRenderInTheCorrectOrderFromBothCameras(int view, float distance) {
    using var f = new Fixture();
    f.Stage.RepeatHorizontally = true; f.Stage.FillCameraView = false; f.Stage.MinimumHeight = 2;
    var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
    camera.transform.position = new Vector3(.5f, 0, (view == 0 ? -1 : 1) * distance); f.Stage.Refresh();
    var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
    var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false);
    var previous = RenderTexture.active;
    using var commands = new CommandBuffer();
    try {
      target.Create(); commands.SetRenderTarget(target); commands.ClearRenderTarget(true, true, Color.black);
      commands.SetViewProjectionMatrices(camera.worldToCameraMatrix, GL.GetGPUProjectionMatrix(camera.projectionMatrix, true));
      foreach (var tile in Tiles(f.Stage, view)) commands.DrawRenderer(tile, f.Stage.BackgroundMaterial);
      Graphics.ExecuteCommandBuffer(commands);
      RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
      var left = pixels.GetPixel(62, 64); var right = pixels.GetPixel(65, 64);
      TestContext.WriteLine($"View {view}, distance {distance}: left={left}, right={right}");
      Assert.Greater(view == 0 ? left.b : left.r, .8f);
      Assert.Greater(view == 0 ? right.r : right.b, .8f);
    } finally {
      RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
    }
  }

  [Test]
  public void UnlitImageRendersFromBothSidesWithTheExpectedHorizontalMirror() {
    using var f = new Fixture();
    f.Stage.FillCameraView = false; f.Stage.MinimumHeight = 2; f.Stage.Refresh();
    var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
    var pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false);
    var previous = RenderTexture.active;
    using var commands = new CommandBuffer();
    try {
      target.Create();
      for (int view = 0; view < 2; ++view) {
        var camera = view == 0 ? f.Stage.Player1Camera : f.Stage.Player2Camera;
        var renderer = view == 0 ? f.Stage.Player1Backdrop : f.Stage.Player2Backdrop;
        commands.Clear(); commands.SetRenderTarget(target); commands.ClearRenderTarget(true, true, Color.black);
        commands.SetViewProjectionMatrices(camera.worldToCameraMatrix, GL.GetGPUProjectionMatrix(camera.projectionMatrix, true));
        commands.DrawRenderer(renderer, f.Stage.BackgroundMaterial);
        Graphics.ExecuteCommandBuffer(commands);
        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
        var left = pixels.GetPixel(24, 32); var right = pixels.GetPixel(40, 32);
        TestContext.WriteLine($"View {view}: left={left}, right={right}");
        Assert.Greater(view == 0 ? left.r : left.b, .8f, "The left side must show the expected world landmark.");
        Assert.Greater(view == 0 ? right.b : right.r, .8f, "The opposite camera must mirror the stage instead of showing its back face.");
      }
    } finally {
      RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
    }
  }
}
