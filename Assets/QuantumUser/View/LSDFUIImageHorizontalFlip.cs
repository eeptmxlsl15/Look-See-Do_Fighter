using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Mirror sprite sampling only. Filled geometry and RectTransform remain unchanged.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class LSDFUIImageHorizontalFlip : BaseMeshEffect {
  [SerializeField] private bool _flipX;
  public bool FlipX {
    get => _flipX;
    set {
      if (_flipX == value) return;
      _flipX = value;
      if (graphic != null) graphic.SetVerticesDirty();
    }
  }

  public override void ModifyMesh(VertexHelper vertices) {
    if (!IsActive() || !_flipX || vertices.currentVertCount == 0) return;
    var image = graphic as Image;
    var sprite = image != null ? image.overrideSprite : null;
    if (sprite == null && image != null) sprite = image.sprite;
    if (sprite == null) return;
    var uv = DataUtility.GetOuterUV(sprite);
    var vertex = default(UIVertex);
    for (int i = 0; i < vertices.currentVertCount; ++i) {
      vertices.PopulateUIVertex(ref vertex, i);
      // Reflect around the full sprite's atlas UV range, even when HP crops the mesh.
      vertex.uv0.x = uv.x + uv.z - vertex.uv0.x;
      vertices.SetUIVertex(vertex, i);
    }
  }
}
