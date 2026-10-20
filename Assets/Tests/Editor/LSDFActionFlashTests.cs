using System.Linq;
using NUnit.Framework;
using Photon.Deterministic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assert = NUnit.Framework.Assert;
using Object = UnityEngine.Object;

public class LSDFActionFlashTests {
  [Test]
  public void PulseHasAQuickSmoothPeakAndReturnsToZero() {
    Assert.AreEqual(0, LSDFActionFlash.Envelope(0, .22f, .035f, .12f));
    float rising = LSDFActionFlash.Envelope(.0175f, .22f, .035f, .12f);
    Assert.Greater(rising, 0); Assert.Less(rising, .22f);
    Assert.AreEqual(.22f, LSDFActionFlash.Envelope(.035f, .22f, .035f, .12f), .0001f);
    float falling = LSDFActionFlash.Envelope(.095f, .22f, .035f, .12f);
    Assert.Greater(falling, 0); Assert.Less(falling, .22f);
    Assert.AreEqual(0, LSDFActionFlash.Envelope(.16f, .22f, .035f, .12f));
    Assert.AreEqual(0, LSDFActionFlash.Envelope(0, .22f, 0, 0));
    Assert.AreEqual(0, LSDFActionFlash.Envelope(.01f, 0, .035f, .12f));
  }

  [Test]
  public void RepeatedFramesDoNotRestartThePulse() {
    using var flash = new LSDFActionFlash();
    flash.Observe(10, 7, LSDFActionFlashKind.Homing, FP._0);
    flash.Advance(.035f, .22f, .035f, .12f);
    flash.Observe(10, 7, LSDFActionFlashKind.Homing, FP._0);
    Assert.AreEqual(.22f, flash.Advance(0, .22f, .035f, .12f), .0001f);
    flash.Advance(.13f, .22f, .035f, .12f);
    flash.Observe(11, 7, LSDFActionFlashKind.Homing, FP._0_10);
    Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
  }

  [Test]
  public void ParryStartFlashesOnceAndASecondActivationCanFlashAgain() {
    using var flash = new LSDFActionFlash();
    flash.Observe(10, 1, LSDFActionFlashKind.Parry, FP._0);
    flash.Advance(.2f, .22f, .035f, .12f);
    flash.Observe(11, 2, LSDFActionFlashKind.Parry, FP._0, true);
    Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
    flash.Observe(12, 1, LSDFActionFlashKind.Parry, FP._0);
    flash.Advance(.035f, .22f, .035f, .12f);
    Assert.AreEqual(.22f, flash.Advance(0, .22f, .035f, .12f), .0001f);
  }

  [Test]
  public void InterruptionResetAndRewindDoNotLeaveAWhiteCharacter() {
    using var flash = new LSDFActionFlash();
    flash.Observe(100, 7, LSDFActionFlashKind.Homing, FP._0);
    flash.Advance(.035f, .22f, .035f, .12f);
    flash.Observe(101, 8, LSDFActionFlashKind.None, FP._0);
    Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
    flash.Observe(102, 7, LSDFActionFlashKind.Homing, FP._0);
    flash.Advance(.035f, .22f, .035f, .12f);
    flash.Observe(90, 7, LSDFActionFlashKind.Homing, FP._1);
    Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
    flash.Reset();
    Assert.AreEqual(0, flash.Advance(0, .22f, .035f, .12f));
  }

  [Test]
  public void TwoFightersShareAMaterialAndKeepIndependentOpacityAndOriginalColors() {
    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuantumUser/View/Rendering/LSDFWhiteFlash.mat");
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets Resources/Effect Prefabs/Red.prefab");
    var first = new GameObject("flash-first", typeof(SpriteRenderer));
    var second = new GameObject("flash-second", typeof(SpriteRenderer));
    using var a = new LSDFActionFlash(); using var b = new LSDFActionFlash();
    try {
      var source = first.GetComponent<SpriteRenderer>(); var other = second.GetComponent<SpriteRenderer>();
      source.sprite = other.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
      source.color = new Color(.3f, .4f, .5f, .8f); var original = source.color;
      source.flipX = true; source.sortingOrder = 3;
      a.Apply(source, material, .2f); b.Apply(other, material, .1f);
      var overlay = first.transform.GetChild(0).GetComponent<SpriteRenderer>();
      var otherOverlay = second.transform.GetChild(0).GetComponent<SpriteRenderer>();
      Assert.AreSame(material, overlay.sharedMaterial); Assert.AreSame(material, otherOverlay.sharedMaterial);
      var block = new MaterialPropertyBlock(); overlay.GetPropertyBlock(block);
      Assert.AreEqual(.16f, block.GetFloat("_FlashOpacity"), .0001f);
      otherOverlay.GetPropertyBlock(block); Assert.AreEqual(.1f, block.GetFloat("_FlashOpacity"), .0001f);
      Assert.AreEqual(original, source.color);
      Assert.AreEqual(-1, overlay.transform.localScale.x);
      Assert.AreEqual(source.sortingOrder + 1, overlay.sortingOrder);
      a.Apply(source, material, 0); Assert.IsFalse(overlay.enabled);
      a.Dispose(); Assert.AreEqual(0, first.transform.childCount);
      Assert.AreEqual(original, source.color);
    } finally { a.Dispose(); b.Dispose(); Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
  }

  [Test]
  public void SceneWiresSharedFlashMaterialAndTuningWithoutChangingChosenEffects() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      var binding = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LSDFSceneBindings>(true)).Single();
      Assert.IsNotNull(binding.ActionFlashMaterial);
      Assert.AreEqual("LSDF/Sprite White Flash", binding.ActionFlashMaterial.shader.name);
      Assert.IsTrue(binding.ActionFlashMaterial.HasProperty("_FlashOpacity"));
      Assert.Greater(binding.ActionFlashStrength, 0); Assert.Less(binding.ActionFlashStrength, .5f);
      Assert.Less(binding.ActionFlashFadeIn + binding.ActionFlashFadeOut, .3f);
      var effect = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Quantum.LSDF.LSDF_HitEffectView>(true)).Single();
      Assert.AreEqual("Assets/Assets Resources/Effect Prefabs/Red.prefab", AssetDatabase.GetAssetPath(effect.HitParticle));
      Assert.AreEqual("Assets/Assets Resources/Effect Prefabs/White.prefab", AssetDatabase.GetAssetPath(effect.GuardParticle));
      Assert.AreEqual("Assets/Assets Resources/Effect Prefabs/Red.prefab", AssetDatabase.GetAssetPath(effect.ParringParticle));
      Assert.AreEqual("Assets/Assets Resources/Effect Prefabs/DarkRed.prefab", AssetDatabase.GetAssetPath(effect.CounterParticle));
      Assert.AreEqual(1, effect.HitOpacity); Assert.AreEqual(1, effect.GuardOpacity);
      Assert.AreEqual(1, effect.ParringOpacity); Assert.AreEqual(1, effect.CounterOpacity);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }

  [Test]
  public void WhiteFlashShaderImportsWithoutErrors() {
    var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/QuantumUser/View/Rendering/LSDFWhiteFlash.shader");
    Assert.IsNotNull(shader);
    var errors = ShaderUtil.GetShaderMessages(shader).Where(message => message.severity.ToString() == "Error").ToArray();
    Assert.IsEmpty(errors, string.Join("\n", errors.Select(message => message.message)));
    Assert.IsTrue(shader.isSupported);
  }

  [Test]
  public void WhiteOverlayBrightensOpaquePixelsAndKeepsTransparentPixelsClear() {
    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuantumUser/View/Rendering/LSDFWhiteFlash.mat");
    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    texture.filterMode = FilterMode.Point;
    texture.SetPixels(new[] { Color.clear, new Color(.1f, .1f, .1f, 1), new Color(.1f, .1f, .1f, 1), new Color(.1f, .1f, .1f, 1) });
    texture.Apply();
    var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2, 0, SpriteMeshType.FullRect);
    var root = new GameObject("flash-render", typeof(SpriteRenderer));
    var target = new RenderTexture(32, 32, 0, RenderTextureFormat.ARGB32);
    var pixels = new Texture2D(32, 32, TextureFormat.RGBA32, false);
    var previous = RenderTexture.active;
    using var flash = new LSDFActionFlash();
    using var command = new UnityEngine.Rendering.CommandBuffer();
    try {
      var source = root.GetComponent<SpriteRenderer>(); source.sprite = sprite;
      flash.Apply(source, material, .22f);
      var overlay = root.transform.GetChild(0).GetComponent<SpriteRenderer>();
      target.Create();
      command.SetRenderTarget(target);
      command.ClearRenderTarget(false, true, new Color(.1f, .1f, .1f, 1));
      command.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(-.5f, .5f, -.5f, .5f, -1, 1));
      command.DrawRenderer(overlay, material);
      Graphics.ExecuteCommandBuffer(command);
      RenderTexture.active = target;
      pixels.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); pixels.Apply();
      float[] samples = { pixels.GetPixel(8, 8).r, pixels.GetPixel(24, 8).r, pixels.GetPixel(8, 24).r, pixels.GetPixel(24, 24).r };
      Assert.Greater(samples.Max() - samples.Min(), .05f, "The sprite alpha mask must leave a clear quadrant instead of whitening a rectangle.");
      Assert.Greater(samples.Max(), .15f, "Opaque dark pixels must brighten under the white overlay.");
    } finally {
      RenderTexture.active = previous;
      flash.Dispose(); target.Release();
      Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
      Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
    }
  }
}
