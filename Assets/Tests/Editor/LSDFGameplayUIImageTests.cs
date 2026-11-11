using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Quantum;
using Quantum.LSDF;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class LSDFGameplayUIImageTests {
  private GameObject _root;
  private Texture2D _texture;
  private Sprite[] _sprites;
  private LSDFSceneBindings.Hud _hud;
  private LSDFGameplayUIImages _images;

  [SetUp] public void SetUp() {
    _root = new GameObject("HUD image tests");
    _texture = new Texture2D(8, 4);
    _sprites = new Sprite[11];
    for (int i = 0; i < _sprites.Length; ++i) _sprites[i] = Sprite.Create(_texture, new Rect(0, 0, 8, 4), new Vector2(.5f, .5f));
    _images = new LSDFGameplayUIImages {
      HealthBackground = _sprites[0], HealthFill = _sprites[1], WinBackground = _sprites[2], WinMark = _sprites[3],
      Round1 = _sprites[4], Round2 = _sprites[5], Round3 = _sprites[6], Round4 = _sprites[7], Round5 = _sprites[8],
      Fight = _sprites[9], RoundWin = _sprites[10], RoundLose = _sprites[0]
    };
    _hud = Hud("first");
    LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
  }

  [TearDown] public void TearDown() {
    Object.DestroyImmediate(_root);
    foreach (var sprite in _sprites) Object.DestroyImmediate(sprite);
    Object.DestroyImmediate(_texture);
  }

  [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
  public void ConfirmedIntroSelectsEachRoundThenFightAndHidesAtUnlock(int round) {
    var mine = new LSDF_Player { playerHp = 170, isRoundEnd = true, loseRound = (round - 1) / 2, tickToEnableMove = 300 };
    var other = mine; other.loseRound = round - 1 - mine.loseRound;
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, mine, other, 100);
    Assert.AreSame(_sprites[round + 3], _hud.Announcement.sprite);
    Assert.IsTrue(_hud.Announcement.enabled); Assert.IsTrue(_hud.RoundRoot.activeSelf);
    Assert.IsFalse(_hud.Round.gameObject.activeSelf); Assert.IsFalse(_hud.Fight.gameObject.activeSelf);
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, mine, other, 239);
    Assert.AreSame(_sprites[round + 3], _hud.Announcement.sprite);
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, mine, other, 240);
    Assert.AreSame(_images.Fight, _hud.Announcement.sprite);
    mine.isRoundEnd = other.isRoundEnd = false;
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, mine, other, 300);
    Assert.IsFalse(_hud.RoundRoot.activeSelf); Assert.IsFalse(_hud.Announcement.enabled);
    Assert.IsNull(_hud.Announcement.sprite);
  }

  [TestCase(true, false)] [TestCase(false, false)] [TestCase(true, true)] [TestCase(false, true)]
  public void ResultsAreLocalToEitherPlayerAndAlsoUseImagesAtMatchEnd(bool localWins, bool matchOver) {
    var otherHud = Hud("second");
    LSDFGameplayUIRenderer.ApplyImages(otherHud, _images);
    int losses = matchOver ? 3 : 1;
    var winner = new LSDF_Player { playerHp = 20, isRoundEnd = true };
    var loser = new LSDF_Player { playerHp = 0, isRoundEnd = true, loseRound = losses };
    var mine = localWins ? winner : loser; var other = localWins ? loser : winner;
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, mine, other, 400);
    LSDFGameplayUIRenderer.RenderRound(otherHud, _images, other, mine, 400);
    Assert.AreSame(localWins ? _images.RoundWin : _images.RoundLose, _hud.Announcement.sprite);
    Assert.AreSame(localWins ? _images.RoundLose : _images.RoundWin, otherHud.Announcement.sprite);
    LSDFGameplayUIRenderer.SetWins(_hud, other.loseRound, mine.loseRound);
    Assert.AreEqual(localWins, _hud.LeftWins[0].gameObject.activeSelf);
    Assert.AreEqual(!localWins, _hud.RightWins[0].gameObject.activeSelf);
    Assert.IsTrue(_hud.LeftWinBackgrounds[2].gameObject.activeSelf);
    Assert.IsTrue(_hud.RightWinBackgrounds[2].gameObject.activeSelf);
    Assert.AreNotSame(_hud.Announcement, otherHud.Announcement);
  }

  [Test] public void PartialImageSetsFallBackPerMessageWithoutLeavingAStaleSprite() {
    _images.Round5 = null;
    LSDFGameplayUIRenderer.ShowRound(_hud, _images, 1);
    LSDFGameplayUIRenderer.ShowRound(_hud, _images, 5);
    Assert.IsFalse(_hud.Announcement.enabled); Assert.IsNull(_hud.Announcement.sprite);
    Assert.IsTrue(_hud.Round.gameObject.activeSelf); Assert.AreEqual("Round 5", _hud.Round.text);
    LSDFGameplayUIRenderer.ShowFight(_hud, _images);
    Assert.AreSame(_images.Fight, _hud.Announcement.sprite); Assert.IsFalse(_hud.Round.gameObject.activeSelf);
    _images.Fight = null;
    LSDFGameplayUIRenderer.ShowFight(_hud, _images);
    Assert.IsFalse(_hud.Announcement.enabled); Assert.IsTrue(_hud.Fight.gameObject.activeSelf);
    Assert.AreEqual("FIGHT", _hud.Fight.text);
    LSDFGameplayUIRenderer.ShowResult(_hud, null, LSDFRoundOutcome.FirstWins, true);
    Assert.AreEqual("YOU WIN", _hud.Fight.text);
    LSDFGameplayUIRenderer.ShowResult(_hud, null, LSDFRoundOutcome.SecondWins, false);
    Assert.AreEqual("ROUND LOSE", _hud.Fight.text);
  }

  [Test] public void DrawNeverUsesWinOrLoseAndNoOutcomeHidesTheMessage() {
    var a = new LSDF_Player { playerHp = 0, isRoundEnd = true };
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, a, a, 10);
    Assert.AreEqual("DRAW", _hud.Fight.text); Assert.IsFalse(_hud.Announcement.enabled);
    _images.Draw = _sprites[1];
    LSDFGameplayUIRenderer.RenderRound(_hud, _images, a, a, 11);
    Assert.AreSame(_images.Draw, _hud.Announcement.sprite);
    LSDFGameplayUIRenderer.ShowResult(_hud, _images, LSDFRoundOutcome.None, false);
    Assert.IsFalse(_hud.RoundRoot.activeSelf); Assert.IsNull(_hud.Announcement.sprite);
  }

  [Test] public void BothHudLayoutsShareArtWithoutChangingHealthRatioOrFillDirection() {
    var binding = _root.AddComponent<LSDFSceneBindings>();
    binding.Player1Hud = _hud; binding.Player2Hud = Hud("second"); binding.GameplayUIImages = _images;
    _hud.LeftHealth.fillAmount = .25f; _hud.RightHealth.fillAmount = .75f;
    _hud.RightHealth.fillOrigin = (int)Image.OriginHorizontal.Right;
    binding.ApplyGameplayUIImages();
    foreach (var hud in new[] { binding.Player1Hud, binding.Player2Hud }) {
      Assert.AreSame(_images.HealthBackground, hud.LeftHealthBackground.sprite);
      Assert.AreSame(_images.HealthBackground, hud.RightHealthBackground.sprite);
      Assert.AreSame(_images.HealthFill, hud.LeftHealth.sprite);
      Assert.AreSame(_images.WinBackground, hud.LeftWinBackgrounds[0].sprite);
      Assert.AreSame(_images.WinMark, hud.LeftWins[0].sprite);
      Assert.AreEqual(Color.white, hud.LeftWins[0].color);
      Assert.AreEqual(Image.Type.Filled, hud.LeftHealth.type);
      Assert.AreEqual(Image.FillMethod.Horizontal, hud.LeftHealth.fillMethod);
      Assert.IsTrue(hud.LeftHealth.GetComponent<LSDFUIImageHorizontalFlip>().FlipX);
      Assert.IsNull(hud.RightHealth.GetComponent<LSDFUIImageHorizontalFlip>());
    }
    Assert.AreEqual(.25f, _hud.LeftHealth.fillAmount); Assert.AreEqual(.75f, _hud.RightHealth.fillAmount);
    Assert.AreEqual((int)Image.OriginHorizontal.Right, _hud.RightHealth.fillOrigin);
    var serialized = new SerializedObject(binding);
    Assert.AreSame(_images.Round5, serialized.FindProperty("GameplayUIImages.Round5").objectReferenceValue);
    _images.MirrorPlayer1HealthFill = false;
    binding.ApplyGameplayUIImages();
    Assert.IsFalse(_hud.LeftHealth.GetComponent<LSDFUIImageHorizontalFlip>().FlipX);
  }

  [Test] public void EmptyArtSlotsPreserveTheAuthoredHealthAndIconGraphics() {
    var color = new Color(.2f, .6f, .3f, .9f);
    _hud.LeftHealthBackground.color = color;
    LSDFGameplayUIRenderer.ApplyImages(_hud, new LSDFGameplayUIImages());
    Assert.AreSame(_images.HealthBackground, _hud.LeftHealthBackground.sprite);
    Assert.AreEqual(color, _hud.LeftHealthBackground.color);
    Assert.AreSame(_images.WinMark, _hud.LeftWins[0].sprite);
  }

  [Test] public void WinsKeepAllThreeBackgroundsAndResetOnlyTheColoredOverlays() {
    LSDFGameplayUIRenderer.SetWins(_hud, 3, 1);
    Assert.IsTrue(_hud.LeftWins[2].gameObject.activeSelf); Assert.IsFalse(_hud.RightWins[1].gameObject.activeSelf);
    LSDFGameplayUIRenderer.SetWins(_hud, 0, 0);
    foreach (var image in _hud.LeftWins) Assert.IsFalse(image.gameObject.activeSelf);
    foreach (var image in _hud.RightWins) Assert.IsFalse(image.gameObject.activeSelf);
    foreach (var image in _hud.LeftWinBackgrounds) Assert.IsTrue(image.gameObject.activeSelf);
    foreach (var image in _hud.RightWinBackgrounds) Assert.IsTrue(image.gameObject.activeSelf);
  }

  [Test] public void ReentryReusesOneImageAndRespectsAspectSizingAndMissingReferences() {
    var image = _hud.Announcement;
    int children = _hud.RoundRoot.transform.childCount;
    _images.AnnouncementSize = new Vector2(500, 140);
    LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
    LSDFGameplayUIRenderer.ShowFight(_hud, _images);
    LSDFGameplayUIRenderer.Hide(_hud);
    LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
    LSDFGameplayUIRenderer.ShowRound(_hud, _images, 2);
    Assert.AreSame(image, _hud.Announcement); Assert.AreEqual(children, _hud.RoundRoot.transform.childCount);
    Assert.AreEqual(new Vector2(500, 140), image.rectTransform.sizeDelta);
    Assert.IsTrue(image.preserveAspect); Assert.IsFalse(image.raycastTarget);
    _hud.Announcement = null;
    LSDFGameplayUIRenderer.Prepare(_hud);
    Assert.AreSame(image, _hud.Announcement); Assert.AreEqual(children, _hud.RoundRoot.transform.childCount);
    Assert.DoesNotThrow(() => LSDFGameplayUIRenderer.ApplyImages(null, null));
    Assert.DoesNotThrow(() => LSDFGameplayUIRenderer.ShowFight(new LSDFSceneBindings.Hud(), _images));
  }

  [Test] public void GameplaySceneConnectsBothHudLayersAndOneAnnouncementEach() {
    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/LSDFGameplay.unity");
    try {
      LSDFSceneBindings binding = null;
      foreach (var root in scene.GetRootGameObjects()) {
        var found = root.GetComponentInChildren<LSDFSceneBindings>(true);
        if (found != null) binding = found;
      }
      Assert.IsNotNull(binding); Assert.IsNotNull(binding.GameplayUIImages);
      foreach (var hud in new[] { binding.Player1Hud, binding.Player2Hud }) {
        Assert.IsNotNull(hud.LeftHealthBackground); Assert.IsNotNull(hud.RightHealthBackground);
        Assert.AreEqual(3, hud.LeftWinBackgrounds.Length); Assert.AreEqual(3, hud.RightWinBackgrounds.Length);
        for (int i = 0; i < 3; ++i) {
          Assert.AreSame(hud.LeftWins[i].transform.parent, hud.LeftWinBackgrounds[i].transform);
          Assert.AreSame(hud.RightWins[i].transform.parent, hud.RightWinBackgrounds[i].transform);
        }
        Assert.IsNotNull(hud.Announcement); Assert.AreSame(hud.RoundRoot.transform, hud.Announcement.transform.parent);
        Assert.IsTrue(hud.Announcement.preserveAspect); Assert.IsFalse(hud.Announcement.enabled);
        Assert.IsFalse(hud.Announcement.raycastTarget);
      }
      Assert.AreNotSame(binding.Player1Hud.Announcement, binding.Player2Hud.Announcement);
    } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }

  [TestCase(false)] [TestCase(true)]
  public void DifferentSpriteSizesFitInsideTheFrameWithoutChangingHealthOrShakeParents(bool mirrored) {
    var outer = Sprite.Create(_texture, new Rect(0, 0, 8, 4), new Vector2(.2f, .8f));
    var inner = Sprite.Create(_texture, new Rect(0, 0, 6, 1), new Vector2(.5f, .5f));
    try {
      _images.HealthBackground = outer; _images.HealthFill = inner;
      _images.HealthBarWidth = 800;
      var frame = _hud.LeftHealthBackground.rectTransform;
      var fill = _hud.LeftHealth.rectTransform;
      var parent = fill.parent;
      parent.localScale = new Vector3(mirrored ? -1 : 1, 1, 1);
      frame.pivot = new Vector2(.2f, .8f);
      frame.anchoredPosition = new Vector2(30, 50);
      _hud.LeftHealth.fillAmount = .25f;
      _hud.LeftHealth.fillOrigin = (int)Image.OriginHorizontal.Right;
      LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
      Assert.AreEqual(new Vector2(800, 400), frame.rect.size);
      Assert.AreEqual(new Vector2(600, 100), fill.rect.size);
      Assert.Less(Vector3.Distance(frame.TransformPoint(frame.rect.center), fill.TransformPoint(fill.rect.center)), .001f);
      Assert.AreSame(parent, fill.parent); Assert.AreSame(parent, frame.parent);
      Assert.AreEqual(.25f, _hud.LeftHealth.fillAmount);
      Assert.AreEqual((int)Image.OriginHorizontal.Right, _hud.LeftHealth.fillOrigin);
      // Reapplying cannot multiply the shrink or drift the original layout.
      LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
      Assert.AreEqual(new Vector2(600, 100), fill.rect.size);
      _images.HealthFillScale = new Vector2(.5f, 2);
      _images.HealthFillOffset = new Vector2(8, -3);
      LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
      Assert.AreEqual(new Vector2(300, 200), fill.rect.size);
      Assert.Less(Vector3.Distance(frame.TransformPoint(frame.rect.center + _images.HealthFillOffset), fill.position), .001f);
      _images.FitHealthBar = false;
      var manualSize = fill.sizeDelta;
      _images.HealthBarWidth = 500;
      LSDFGameplayUIRenderer.ApplyImages(_hud, _images);
      Assert.AreEqual(manualSize, fill.sizeDelta); Assert.AreEqual(800, frame.rect.width);
    } finally { Object.DestroyImmediate(outer); Object.DestroyImmediate(inner); }
  }

  private LSDFSceneBindings.Hud Hud(string name) {
    var parent = Child(_root.transform, name);
    var left = Child(parent.transform, "left"); var right = Child(parent.transform, "right");
    CreateImage(left.transform, "Left Background Health"); CreateImage(right.transform, "Right Background Health");
    var hud = new LSDFSceneBindings.Hud {
      LeftHealth = CreateImage(left.transform, "Left Current Health"), RightHealth = CreateImage(right.transform, "Right Current Health"),
      RoundRoot = Child(parent.transform, "Round Root")
    };
    hud.Round = Child(hud.RoundRoot.transform, "Round").AddComponent<TextMeshProUGUI>();
    hud.Fight = Child(hud.RoundRoot.transform, "Fight").AddComponent<TextMeshProUGUI>();
    for (int i = 0; i < 3; ++i) {
      var a = CreateImage(left.transform, "Slot " + i); var b = CreateImage(right.transform, "Slot " + i);
      hud.LeftWins[i] = CreateImage(a.transform, "Win"); hud.RightWins[i] = CreateImage(b.transform, "Win");
      hud.LeftWins[i].color = hud.RightWins[i].color = Color.red;
    }
    return hud;
  }
  private static GameObject Child(Transform parent, string name) {
    var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
  }
  private static Image CreateImage(Transform parent, string name) => Child(parent, name).AddComponent<Image>();
}
