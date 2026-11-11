using Quantum.LSDF;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(LSDFSceneBindings)), CanEditMultipleObjects]
public sealed class LSDFSceneBindingsEditor : Editor {
  private int _preview;
  private static readonly string[] Previews = {
    "숨김", "Round 1", "Round 2", "Round 3", "Round 4", "Round 5", "Fight", "Round Win", "Round Lose", "Draw"
  };

  private void OnEnable() { serializedObject.FindProperty("GameplayUIImages").isExpanded = true; }

  public override void OnInspectorGUI() {
    serializedObject.Update();
    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("GameplayUIImages"), true);
    bool changed = EditorGUI.EndChangeCheck();
    serializedObject.ApplyModifiedProperties();
    EditorGUILayout.HelpBox("Sprite (2D and UI)로 임포트한 이미지를 위 슬롯에 넣으세요. 양쪽 플레이어 HUD에 공통 적용됩니다. " +
      "비어 있는 체력/아이콘 슬롯은 기존 그림을 유지하고, 중앙 알림은 해당 이미지가 없으면 텍스트를 표시합니다. " +
      "Round Win/Lose는 최종 매치 결과에도 사용합니다.", MessageType.Info);
    using (new EditorGUI.DisabledScope(Application.isPlaying)) {
      _preview = EditorGUILayout.Popup("중앙 알림 미리보기", _preview, Previews);
      if (GUILayout.Button("이미지 적용 / UI 연결 / 미리보기") || changed) Apply();
    }
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("기존 씬 연결 · 카메라 · 연출", EditorStyles.boldLabel);
    serializedObject.Update();
    DrawPropertiesExcluding(serializedObject, "m_Script", "GameplayUIImages");
    serializedObject.ApplyModifiedProperties();
  }

  private void Apply() {
    foreach (var selected in targets) {
      var binding = (LSDFSceneBindings)selected;
      Undo.RecordObject(binding, "Apply gameplay UI images");
      foreach (var hud in new[] { binding.Player1Hud, binding.Player2Hud }) {
        if (hud == null) continue;
        var canvas = hud.RoundRoot != null ? hud.RoundRoot.GetComponentInParent<Canvas>(true) : null;
        if (canvas != null) Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply gameplay UI images");
        var previous = hud.Announcement;
        var previousFlip = hud.LeftHealth != null ? hud.LeftHealth.GetComponent<LSDFUIImageHorizontalFlip>() : null;
        LSDFGameplayUIRenderer.ApplyImages(hud, binding.GameplayUIImages);
        if (previousFlip == null && hud.LeftHealth != null) {
          var createdFlip = hud.LeftHealth.GetComponent<LSDFUIImageHorizontalFlip>();
          if (createdFlip != null) Undo.RegisterCreatedObjectUndo(createdFlip, "Create health sprite flip");
        }
        if (previous == null && hud.Announcement != null)
          Undo.RegisterCreatedObjectUndo(hud.Announcement.gameObject, "Create announcement image");
        if (_preview >= 1 && _preview <= 5) LSDFGameplayUIRenderer.ShowRound(hud, binding.GameplayUIImages, _preview);
        else if (_preview == 6) LSDFGameplayUIRenderer.ShowFight(hud, binding.GameplayUIImages);
        else if (_preview >= 7) LSDFGameplayUIRenderer.ShowResult(hud, binding.GameplayUIImages,
          _preview == 7 ? LSDFRoundOutcome.FirstWins : _preview == 8 ? LSDFRoundOutcome.SecondWins : LSDFRoundOutcome.Draw, false);
        else LSDFGameplayUIRenderer.Hide(hud);
      }
      EditorUtility.SetDirty(binding);
      if (binding.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
    }
    SceneView.RepaintAll();
  }
}
