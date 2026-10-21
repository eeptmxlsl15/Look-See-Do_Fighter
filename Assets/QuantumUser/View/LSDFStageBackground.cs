using UnityEngine;

/// <summary>Opposing-camera backdrops projected as one flat stage with the fighters; presentation only.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
public sealed class LSDFStageBackground : MonoBehaviour {
  [Tooltip("Assign the panoramic PNG imported as Sprite (2D and UI). Both cameras share it.")]
  public Sprite BackgroundSprite;
  [Tooltip("Mirror the second view so landmarks retain their world left/right positions.")]
  public bool MirrorPlayer2 = true;
  [Min(.01f), Tooltip("Image height at the reference camera distance, before optional reference fit. Width keeps the sprite aspect ratio.")]
  public float MinimumHeight = 3;
  [Min(.1f), Tooltip("Distance behind the fighters on each side of the stage, along local Z.")]
  public float Depth = 4;
  [Min(.01f), Tooltip("Camera distance from the fighter plane used to author the initial framing. Zoom never refits that framing.")]
  public float ReferenceCameraDistance = .8f;
  public Vector2 Offset;
  [InspectorName("Fit Reference View"), Tooltip("Fit at the reference camera distance. Moving or zooming the camera does not enlarge the image to refill the screen.")]
  public bool FillCameraView = true;
  [Range(1, 1.2f)] public float Overscan = 1.02f;
  [Tooltip("Hide only the old ground's SpriteRenderer while a background is assigned. Collisions remain active.")]
  public bool HideGroundVisual = true;

  [HideInInspector] public Camera Player1Camera, Player2Camera;
  [HideInInspector] public SpriteRenderer Player1Backdrop, Player2Backdrop, GroundVisual;
  [HideInInspector] public Material BackgroundMaterial;
  // Persist the editor preview's restore information when a scene is saved with the ground hidden.
  [SerializeField, HideInInspector] private SpriteRenderer _hiddenGround;
  [SerializeField, HideInInspector] private bool _groundWasEnabled;
  private struct ReferenceView {
    public Camera Camera;
    public float Aspect, Depth, Distance;
    public bool Orthographic, Valid;
    public Matrix4x4 StageMatrix;
    public Vector2 Min, Max;
  }
  private ReferenceView _firstReference, _secondReference;

  private void OnEnable() => RefitReferenceView();
  private void LateUpdate() => Refresh();

  [ContextMenu("Refit Reference View")]
  public void RefitReferenceView() {
    _firstReference = default;
    _secondReference = default;
    Refresh();
  }

  [ContextMenu("Refresh Background")]
  public void Refresh() {
    bool ready = isActiveAndEnabled && BackgroundSprite != null && BackgroundMaterial != null;
    if (!ready) { Clear(); return; }
    var size = BackgroundSprite.bounds.size;
    if (size.x <= 0 || size.y <= 0) { Clear(); return; }
    float aspect = size.x / size.y;
    float depth = Mathf.Max(.1f, Depth);
    float height = Mathf.Max(.01f, MinimumHeight);
    if (FillCameraView) {
      float firstHeight = ReferenceHeight(Player1Camera, 1, depth, aspect, ref _firstReference);
      float secondHeight = ReferenceHeight(Player2Camera, -1, depth, aspect, ref _secondReference);
      height = Mathf.Max(height, Mathf.Max(firstHeight, secondHeight) * Mathf.Clamp(Overscan, 1, 1.2f));
    }
    bool firstVisible = ApplyFlatView(Player1Camera, Player1Backdrop, 1, depth, height / size.y, false);
    bool secondVisible = ApplyFlatView(Player2Camera, Player2Backdrop, -1, depth, height / size.y, MirrorPlayer2);
    UpdateGround(firstVisible || secondVisible);
  }

  // Capture lens/frustum at a fixed reference pose, never at the current follow/zoom position.
  // Aspect changes refit the viewport; lens changes keep the captured framing until an explicit refit.
  private float ReferenceHeight(Camera camera, int side, float depth, float aspect, ref ReferenceView view) {
    if (camera == null) { view = default; return 0; }
    float distance = Mathf.Max(.01f, ReferenceCameraDistance);
    if (view.Camera != camera || view.Aspect != camera.aspect || view.Orthographic != camera.orthographic ||
        view.Depth != depth || view.Distance != distance || view.StageMatrix != transform.localToWorldMatrix) {
      view = new ReferenceView { Camera = camera, Aspect = camera.aspect, Orthographic = camera.orthographic,
        Depth = depth, Distance = distance, StageMatrix = transform.localToWorldMatrix, Valid = true,
        Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity) };
      var referencePosition = transform.TransformPoint(new Vector3(0, 0, -side * distance));
      var plane = new Plane(transform.forward, transform.TransformPoint(new Vector3(0, 0, side * depth)));
      for (int corner = 0; corner < 4; ++corner) {
        var ray = camera.ViewportPointToRay(new Vector3(corner & 1, corner >> 1, 0));
        ray.origin += referencePosition - camera.transform.position;
        if (!plane.Raycast(ray, out float hit)) { view.Valid = false; break; }
        var point = (Vector2)transform.InverseTransformPoint(ray.GetPoint(hit));
        view.Min = Vector2.Min(view.Min, point); view.Max = Vector2.Max(view.Max, point);
      }
    }
    if (!view.Valid) return 0;
    var low = view.Min - Offset; var high = view.Max - Offset;
    return 2 * Mathf.Max(Mathf.Max(Mathf.Abs(low.y), Mathf.Abs(high.y)),
      Mathf.Max(Mathf.Abs(low.x), Mathf.Abs(high.x)) / aspect);
  }

  // Keep the render plane safely behind the fighters, but project every point as if it were on local Z=0.
  // The homothety about the camera removes depth parallax and gives the image the fighters' exact zoom ratio.
  private bool ApplyFlatView(Camera camera, SpriteRenderer backdrop, int side, float depth, float scale, bool mirror) {
    if (backdrop == null) return false;
    if (camera == null) { backdrop.enabled = false; return false; }
    var center = new Vector3(Offset.x, Offset.y, side * depth);
    if (!camera.orthographic) {
      var cameraLocal = transform.InverseTransformPoint(camera.transform.position);
      if (side * cameraLocal.z >= -.0001f) { backdrop.enabled = false; return false; }
      float reference = Mathf.Max(.01f, ReferenceCameraDistance);
      float toStage = reference / (reference + depth);
      float toBackdrop = (side * depth - cameraLocal.z) / -cameraLocal.z;
      center = cameraLocal + (new Vector3(Offset.x * toStage, Offset.y * toStage, 0) - cameraLocal) * toBackdrop;
      scale *= toStage * toBackdrop;
    }
    float cameraDepth = Vector3.Dot(transform.TransformPoint(center) - camera.transform.position, camera.transform.forward);
    bool visible = cameraDepth > camera.nearClipPlane && cameraDepth < camera.farClipPlane;
    Apply(backdrop, center, side > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0), mirror, scale, visible);
    return visible;
  }

  private void Apply(SpriteRenderer backdrop, Vector3 center, Quaternion rotation, bool mirror, float scale, bool visible) {
    if (backdrop == null) return;
    backdrop.sprite = BackgroundSprite;
    backdrop.sharedMaterial = BackgroundMaterial;
    backdrop.flipX = mirror;
    backdrop.flipY = false;
    backdrop.drawMode = SpriteDrawMode.Simple;
    backdrop.sortingOrder = -100;
    backdrop.transform.localRotation = rotation;
    backdrop.transform.localScale = new Vector3(scale, scale, 1);
    var pivot = BackgroundSprite.bounds.center;
    if (mirror) pivot.x = -pivot.x;
    backdrop.transform.localPosition = center - rotation * (pivot * scale);
    backdrop.enabled = visible;
  }

  private void UpdateGround(bool hasView) {
    bool hide = HideGroundVisual && hasView && GroundVisual != null;
    if (_hiddenGround != null && (!hide || _hiddenGround != GroundVisual)) RestoreGround();
    if (!hide) return;
    if (_hiddenGround == null) { _hiddenGround = GroundVisual; _groundWasEnabled = GroundVisual.enabled; }
    GroundVisual.enabled = false;
  }

  private void RestoreGround() {
    if (_hiddenGround != null) _hiddenGround.enabled = _groundWasEnabled;
    _hiddenGround = null;
  }

  private void Clear() {
    if (Player1Backdrop != null) Player1Backdrop.enabled = false;
    if (Player2Backdrop != null) Player2Backdrop.enabled = false;
    RestoreGround();
    _firstReference = default; _secondReference = default;
  }
  private void OnDisable() => Clear();
}
