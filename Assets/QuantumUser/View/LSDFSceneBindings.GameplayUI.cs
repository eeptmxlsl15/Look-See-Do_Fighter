using System;
using Quantum.LSDF;
using UnityEngine;
using UnityEngine.UI;

public partial class LSDFSceneBindings {
  [Header("게임플레이 UI 이미지")]
  public LSDFGameplayUIImages GameplayUIImages = new LSDFGameplayUIImages();

  [ContextMenu("게임 UI 이미지 적용 / 참조 연결")]
  public void ApplyGameplayUIImages() {
    LSDFGameplayUIRenderer.ApplyImages(Player1Hud, GameplayUIImages);
    LSDFGameplayUIRenderer.ApplyImages(Player2Hud, GameplayUIImages);
  }
}

/// <summary>Shared presentation settings for both local-player HUD layouts. No match state.</summary>
[Serializable]
public sealed class LSDFGameplayUIImages {
  [Header("체력바")]
  [InspectorName("체력바 배경"), Tooltip("체력과 무관하게 전체가 보이는 배경/프레임 이미지입니다.")]
  public Sprite HealthBackground;
  [InspectorName("체력 채움 (선택)"), Tooltip("비워 두면 기존 채움 이미지를 유지합니다. HP 비율만큼 가로로 줄어듭니다.")]
  public Sprite HealthFill;
  [InspectorName("1P (왼쪽) 체력 이미지 좌우 반전"), Tooltip("채움 Sprite의 색상 방향만 반전합니다. 체력이 줄어드는 방향, 위치, 크기는 바꾸지 않습니다.")]
  public bool MirrorPlayer1HealthFill = true;
  [InspectorName("체력바 원본 비율 자동 맞춤"), Tooltip("배경의 너비를 기준으로 원본 비율을 유지하고 채움을 배경 안에 맞춥니다. 끄면 RectTransform을 직접 배치할 수 있습니다.")]
  public bool FitHealthBar = true;
  [InspectorName("체력바 가로 크기"), Min(0), Tooltip("양쪽 HUD의 체력바 너비입니다. 0이면 기존 배경 RectTransform의 너비를 유지합니다.")]
  public float HealthBarWidth;
  [InspectorName("체력 채움 크기 배율"), Tooltip("원본 배경 대비 채움 크기에 곱합니다. 기본 (1, 1). 채움은 배경 크기를 넘지 않습니다.")]
  public Vector2 HealthFillScale = Vector2.one;
  [InspectorName("체력 채움 위치 보정"), Tooltip("배경 중심 기준 Canvas 단위입니다. 기본 (0, 0).")]
  public Vector2 HealthFillOffset;
  [Header("라운드 승리 아이콘")]
  [InspectorName("승리 아이콘 배경"), Tooltip("승리 수와 관계없이 세 칸 모두 표시됩니다.")]
  public Sprite WinBackground;
  [InspectorName("승리 시 빨간 이미지"), Tooltip("획득한 라운드 수만큼 배경 위에 겹쳐 표시합니다. 원본 색을 그대로 사용합니다.")]
  public Sprite WinMark;
  [Header("중앙 알림")]
  public Sprite Fight;
  public Sprite Round1, Round2, Round3, Round4, Round5;
  public Sprite RoundWin, RoundLose;
  [Tooltip("선택 항목입니다. 비워 두면 무승부에는 DRAW 텍스트를 표시합니다.")]
  public Sprite Draw;
  [InspectorName("중앙 이미지 표시 크기"), Tooltip("Canvas 단위의 최대 표시 영역입니다. 이미지 원본 비율을 유지합니다.")]
  public Vector2 AnnouncementSize = new Vector2(640, 180);

  public Sprite RoundSprite(int round) {
    switch (round) {
      case 1: return Round1;
      case 2: return Round2;
      case 3: return Round3;
      case 4: return Round4;
      case 5: return Round5;
      default: return null;
    }
  }
}

public static class LSDFGameplayUIRenderer {
  // Existing authored backgrounds and the new announcement image are reused across updates/re-entry.
  public static void Prepare(LSDFSceneBindings.Hud hud) {
    if (hud == null) return;
    if (hud.LeftHealthBackground == null) hud.LeftHealthBackground = HealthBackground(hud.LeftHealth);
    if (hud.RightHealthBackground == null) hud.RightHealthBackground = HealthBackground(hud.RightHealth);
    hud.LeftWinBackgrounds = WinBackgrounds(hud.LeftWins, hud.LeftWinBackgrounds);
    hud.RightWinBackgrounds = WinBackgrounds(hud.RightWins, hud.RightWinBackgrounds);
    if (hud.Announcement == null && hud.RoundRoot != null) {
      var existing = hud.RoundRoot.transform.Find("Announcement Image");
      var go = existing != null ? existing.gameObject : new GameObject("Announcement Image", typeof(RectTransform));
      if (existing == null) {
        go.layer = hud.RoundRoot.layer;
        go.transform.SetParent(hud.RoundRoot.transform, false);
      }
      hud.Announcement = go.GetComponent<Image>();
      if (hud.Announcement == null) hud.Announcement = go.AddComponent<Image>();
      hud.Announcement.enabled = false;
      var rect = hud.Announcement.rectTransform;
      rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
      rect.anchoredPosition = Vector2.zero;
    }
  }

  private static Image HealthBackground(Image fill) {
    if (fill == null || fill.transform.parent == null) return null;
    var sibling = fill.transform.parent.Find(fill.name.Replace("Current", "Background"));
    return sibling != null && sibling != fill.transform ? sibling.GetComponent<Image>() : null;
  }

  private static Image[] WinBackgrounds(Image[] marks, Image[] backgrounds) {
    if (marks == null) return backgrounds;
    if (backgrounds == null || backgrounds.Length != marks.Length) Array.Resize(ref backgrounds, marks.Length);
    for (int i = 0; i < marks.Length; ++i) {
      if (backgrounds[i] == null && marks[i] != null && marks[i].transform.parent != null)
        backgrounds[i] = marks[i].transform.parent.GetComponent<Image>();
    }
    return backgrounds;
  }

  public static void ApplyImages(LSDFSceneBindings.Hud hud, LSDFGameplayUIImages images) {
    if (hud == null) return;
    Prepare(hud);
    if (images == null) return;
    ApplySprite(hud.LeftHealthBackground, images.HealthBackground);
    ApplySprite(hud.RightHealthBackground, images.HealthBackground);
    ApplyFill(hud.LeftHealth, images.HealthFill);
    ApplyFill(hud.RightHealth, images.HealthFill);
    ApplyFillFlip(hud.LeftHealth, images.MirrorPlayer1HealthFill);
    FitHealthBar(hud.LeftHealthBackground, hud.LeftHealth, images);
    FitHealthBar(hud.RightHealthBackground, hud.RightHealth, images);
    ApplySprites(hud.LeftWinBackgrounds, images.WinBackground);
    ApplySprites(hud.RightWinBackgrounds, images.WinBackground);
    ApplySprites(hud.LeftWins, images.WinMark);
    ApplySprites(hud.RightWins, images.WinMark);
    if (hud.Announcement != null) {
      hud.Announcement.raycastTarget = false;
      hud.Announcement.preserveAspect = true;
      hud.Announcement.type = Image.Type.Simple;
      hud.Announcement.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, images.AnnouncementSize.x), Mathf.Max(1, images.AnnouncementSize.y));
    }
  }

  private static void ApplyFill(Image image, Sprite sprite) {
    if (image == null) return;
    ApplySprite(image, sprite);
    image.type = Image.Type.Filled;
    image.fillMethod = Image.FillMethod.Horizontal;
    // Preserve the authored fill origin; the opponent's layout may be mirrored.
  }

  private static void ApplyFillFlip(Image image, bool flip) {
    if (image == null) return;
    var effect = image.GetComponent<LSDFUIImageHorizontalFlip>();
    if (effect == null && flip) effect = image.gameObject.AddComponent<LSDFUIImageHorizontalFlip>();
    if (effect != null) effect.FlipX = flip;
  }

  private static void FitHealthBar(Image background, Image fill, LSDFGameplayUIImages images) {
    if (!images.FitHealthBar || background == null || fill == null || background == fill ||
        background.sprite == null || fill.sprite == null) return;
    var frame = background.rectTransform;
    var bar = fill.rectTransform;
    // The authored HUD keeps both images under the same shake root; never reparent them.
    if (frame.parent != bar.parent || frame.parent == null) return;
    var outer = background.sprite.rect.size;
    var inner = fill.sprite.rect.size;
    if (outer.x <= 0 || outer.y <= 0 || inner.x <= 0 || inner.y <= 0) return;
    if (images.HealthBarWidth > 0) frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, images.HealthBarWidth);
    float width = frame.rect.width;
    if (width <= 0) return;
    frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width * outer.y / outer.x);
    background.type = Image.Type.Simple;
    background.preserveAspect = false; // The RectTransform itself now has the source aspect.
    var ratio = new Vector2(Mathf.Clamp01(inner.x / outer.x * Mathf.Max(0, images.HealthFillScale.x)),
      Mathf.Clamp01(inner.y / outer.y * Mathf.Max(0, images.HealthFillScale.y)));
    bar.anchorMin = frame.anchorMin;
    bar.anchorMax = frame.anchorMax;
    bar.pivot = new Vector2(.5f, .5f);
    bar.localScale = frame.localScale;
    bar.localRotation = frame.localRotation;
    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frame.rect.width * ratio.x);
    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frame.rect.height * ratio.y);
    bar.localPosition = bar.parent.InverseTransformPoint(frame.TransformPoint(frame.rect.center + images.HealthFillOffset));
    fill.preserveAspect = false; // Filled uses the full-width rect; fillAmount alone controls current HP.
  }

  private static void ApplySprites(Image[] targets, Sprite sprite) {
    if (targets == null) return;
    foreach (var image in targets) ApplySprite(image, sprite);
  }

  private static void ApplySprite(Image image, Sprite sprite) {
    if (image == null || sprite == null) return; // Empty slots retain the authored graphic.
    image.overrideSprite = null;
    image.sprite = sprite;
    image.color = Color.white;
    image.raycastTarget = false;
  }

  public static void SetWins(LSDFSceneBindings.Hud hud, int left, int right) {
    if (hud == null) return;
    SetWins(hud.LeftWins, hud.LeftWinBackgrounds, left);
    SetWins(hud.RightWins, hud.RightWinBackgrounds, right);
  }

  private static void SetWins(Image[] marks, Image[] backgrounds, int count) {
    if (backgrounds != null) foreach (var background in backgrounds)
      if (background != null) background.gameObject.SetActive(true);
    if (marks != null) for (int i = 0; i < marks.Length; ++i)
      if (marks[i] != null) marks[i].gameObject.SetActive(i < count);
  }

  public static void RenderRound(LSDFSceneBindings.Hud hud, LSDFGameplayUIImages images,
      in Quantum.LSDF_Player mine, in Quantum.LSDF_Player other, int verifiedTick) {
    var phase = LSDFRoundPolicy.Phase(true, mine, other, verifiedTick);
    if (phase == LSDFRoundPhase.RoundEnd || phase == LSDFRoundPhase.MatchEnd) {
      ShowResult(hud, images, LSDFRoundPolicy.Outcome(mine, other), phase == LSDFRoundPhase.MatchEnd);
    } else if (phase == LSDFRoundPhase.Intro) {
      int remaining = Mathf.Max(mine.tickToEnableMove, other.tickToEnableMove) - verifiedTick;
      if (remaining <= 60) ShowFight(hud, images);
      else ShowRound(hud, images, LSDFRoundPolicy.RoundNumber(mine, other));
    } else Hide(hud);
  }

  public static void ShowRound(LSDFSceneBindings.Hud hud, LSDFGameplayUIImages images, int round) =>
    Show(hud, images != null ? images.RoundSprite(round) : null, $"Round {round}", true);

  public static void ShowFight(LSDFSceneBindings.Hud hud, LSDFGameplayUIImages images) =>
    Show(hud, images != null ? images.Fight : null, "FIGHT", false);

  public static void ShowResult(LSDFSceneBindings.Hud hud, LSDFGameplayUIImages images, LSDFRoundOutcome outcome, bool matchOver) {
    if (outcome == LSDFRoundOutcome.None) { Hide(hud); return; }
    Sprite sprite = images == null ? null : outcome == LSDFRoundOutcome.Draw ? images.Draw :
      outcome == LSDFRoundOutcome.FirstWins ? images.RoundWin : images.RoundLose;
    Show(hud, sprite, LSDF_IngameUI.ResultMessage(outcome, matchOver), false);
  }

  private static void Show(LSDFSceneBindings.Hud hud, Sprite sprite, string fallback, bool roundText) {
    if (hud == null || hud.RoundRoot == null) return;
    bool useImage = sprite != null && hud.Announcement != null;
    if (hud.Announcement != null) {
      hud.Announcement.sprite = useImage ? sprite : null;
      hud.Announcement.color = Color.white;
      hud.Announcement.enabled = useImage;
      hud.Announcement.gameObject.SetActive(true);
    }
    var label = roundText ? (hud.Round != null ? hud.Round : hud.Fight) : (hud.Fight != null ? hud.Fight : hud.Round);
    if (hud.Round != null) hud.Round.gameObject.SetActive(!useImage && hud.Round == label);
    if (hud.Fight != null) hud.Fight.gameObject.SetActive(!useImage && hud.Fight == label);
    if (!useImage && label != null) label.text = fallback;
    hud.RoundRoot.SetActive(useImage || label != null);
  }

  public static void Hide(LSDFSceneBindings.Hud hud) {
    if (hud == null) return;
    if (hud.Announcement != null) { hud.Announcement.enabled = false; hud.Announcement.sprite = null; }
    if (hud.RoundRoot != null) hud.RoundRoot.SetActive(false);
  }
}
