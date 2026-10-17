#if UNITY_EDITOR
using System.Linq;
using Quantum.Addons.Animator;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LSDFCounterHitZoomSettings))]
public sealed class LSDFCounterHitZoomSettingsEditor : Editor {
  private string _search = "";
  public override void OnInspectorGUI() {
    serializedObject.Update();
    EditorGUILayout.HelpBox("카운터 히트 줌을 사용할 기술만 체크하세요. 펼치면 기술별 배율과 시간을 조절할 수 있습니다.\n일반 히트·가드에는 적용되지 않습니다.", MessageType.Info);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("Enabled"), new GUIContent("전체 사용"));
    _search = EditorGUILayout.TextField("기술 검색", _search);
    var attacks = serializedObject.FindProperty("Attacks");
    for (int i = 0; i < attacks.arraySize; i++) {
      var entry = attacks.GetArrayElementAtIndex(i);
      var attack = entry.FindPropertyRelative("Attack").objectReferenceValue;
      string label = attack != null ? attack.name.Replace(" Window Event", "") : "기술 미지정";
      if (!string.IsNullOrEmpty(_search) && label.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
      EditorGUILayout.BeginHorizontal();
      var enabled = entry.FindPropertyRelative("Enabled");
      enabled.boolValue = EditorGUILayout.Toggle(enabled.boolValue, GUILayout.Width(20));
      entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, label, true);
      EditorGUILayout.EndHorizontal();
      if (!entry.isExpanded) continue;
      EditorGUI.indentLevel++;
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("Attack"), new GUIContent("기술 에셋"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("Magnification"), new GUIContent("줌 배율"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("ZoomInSeconds"), new GUIContent("줌인 시간 (초)"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("HoldSeconds"), new GUIContent("유지 시간 (초)"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("ReturnSeconds"), new GUIContent("복귀 시간 (초)"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("FocusStrength"), new GUIContent("타격점 중심 이동"));
      EditorGUILayout.PropertyField(entry.FindPropertyRelative("FocusOffset"), new GUIContent("타격점 위치 보정"));
      EditorGUI.indentLevel--;
    }
    serializedObject.ApplyModifiedProperties();
    if (GUILayout.Button("새 기술 목록 추가 / 기존 설정 유지")) AddMissingAttacks((LSDFCounterHitZoomSettings)target);
  }

  private static void AddMissingAttacks(LSDFCounterHitZoomSettings settings) {
    var attacks = AssetDatabase.FindAssets("t:AnimatorTimeWindowEventAsset", new[] { "Assets/QuantumUser/Resources/LSDF_Animator Assets" })
      .Select(guid => AssetDatabase.LoadAssetAtPath<AnimatorTimeWindowEventAsset>(AssetDatabase.GUIDToAssetPath(guid)))
      .Where(attack => attack != null && (attack.GetType().GetField("Hit") != null ||
        attack.GetType().GetField("FirstHit") != null))
      .OrderBy(attack => attack.name);
    var entries = (settings.Attacks ?? System.Array.Empty<LSDFCounterHitZoomSettings.Entry>()).ToList();
    Undo.RecordObject(settings, "Add missing counter-hit zoom moves");
    foreach (var attack in attacks) if (!entries.Any(entry => entry != null && entry.Attack == attack))
      entries.Add(new LSDFCounterHitZoomSettings.Entry { Attack = attack });
    settings.Attacks = entries.ToArray();
    EditorUtility.SetDirty(settings);
  }
}
#endif
