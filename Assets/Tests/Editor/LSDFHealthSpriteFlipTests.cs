using System.Reflection;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class LSDFHealthSpriteFlipTests {
  [TestCase(0, 1f)] [TestCase(1, 1f)]
  [TestCase(0, .5f)] [TestCase(1, .5f)]
  [TestCase(0, .1f)] [TestCase(1, .1f)]
  public void SpriteUvMirrorsWithinItsAtlasRectWithoutChangingFilledGeometry(int origin, float amount) {
    var go = new GameObject("health sprite flip", typeof(RectTransform), typeof(CanvasRenderer));
    var texture = new Texture2D(8, 2);
    var sprite = Sprite.Create(texture, new Rect(2, 0, 4, 2), new Vector2(.5f, .5f));
    try {
      var image = go.AddComponent<Image>();
      image.sprite = sprite; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal;
      image.fillOrigin = origin; image.fillAmount = amount;
      var rect = image.rectTransform;
      rect.sizeDelta = new Vector2(400, 40); rect.anchoredPosition = new Vector2(17, 23);
      var effect = go.AddComponent<LSDFUIImageHorizontalFlip>(); effect.FlipX = true;
      using (var mesh = new VertexHelper()) {
        Populate(image, mesh);
        var before = new UIVertex[mesh.currentVertCount];
        for (int i = 0; i < before.Length; ++i) mesh.PopulateUIVertex(ref before[i], i);
        effect.ModifyMesh(mesh);
        var uv = DataUtility.GetOuterUV(sprite);
        for (int i = 0; i < before.Length; ++i) {
          var after = default(UIVertex); mesh.PopulateUIVertex(ref after, i);
          Assert.AreEqual(before[i].position, after.position);
          Assert.AreEqual(before[i].color, after.color);
          Assert.AreEqual(uv.x + uv.z - before[i].uv0.x, after.uv0.x, .00001f);
          Assert.AreEqual(before[i].uv0.y, after.uv0.y);
          // Right-origin 1P: low HP retains the original left/yellow part after mirroring.
          if (origin == 1) Assert.LessOrEqual(after.uv0.x, uv.x + (uv.z - uv.x) * amount + .00001f);
        }
        effect.FlipX = false;
        Populate(image, mesh); effect.ModifyMesh(mesh);
        for (int i = 0; i < before.Length; ++i) {
          var after = default(UIVertex); mesh.PopulateUIVertex(ref after, i);
          Assert.AreEqual(before[i].uv0, after.uv0);
        }
      }
      Assert.AreEqual(origin, image.fillOrigin); Assert.AreEqual(amount, image.fillAmount);
      Assert.AreEqual(new Vector2(400, 40), rect.sizeDelta);
      Assert.AreEqual(new Vector2(17, 23), rect.anchoredPosition);
      Assert.AreEqual(Vector3.one, rect.localScale);
      Assert.AreSame(sprite, image.sprite);
    } finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
  }

  private static void Populate(Image image, VertexHelper mesh) {
    typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
      new[] { typeof(VertexHelper) }, null).Invoke(image, new object[] { mesh });
  }
}
