#if UNITY_EDITOR
using Quantum.LSDF;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LSDFPlayer2TestInputSettings))]
public sealed class LSDFPlayer2TestInputSettingsEditor : Editor {
  private static readonly string[] Directions = { "중립", "뒤 (←)", "뒤+아래 (↙)", "아래 (↓)", "앞+아래 (↘)", "앞 (→)", "위 (↑)" };
  private static readonly string[] Buttons = { "LP · 왼손 (U)", "RP · 오른손 (I)", "LK · 왼발 (J)", "RK · 오른발 (K)" };
  public override void OnInspectorGUI() {
    serializedObject.Update();
    EditorGUILayout.HelpBox("이 클라이언트가 2P일 때 지정한 커맨드를 자동 입력합니다.\nEditor / Development Build에서 적용되며, 방향은 캐릭터 기준 앞·뒤입니다.\n실행되는 기술은 2P의 기존 커맨드별 스킬 설정을 따릅니다.", MessageType.Info);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("Enabled"), new GUIContent("2P 테스트 입력 사용"));
    var direction = serializedObject.FindProperty("Direction");
    var button = serializedObject.FindProperty("Button");
    direction.intValue = EditorGUILayout.Popup("방향", direction.intValue, Directions);
    button.intValue = EditorGUILayout.Popup("공격 버튼", button.intValue, Buttons);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("Repeat"), new GUIContent("반복 입력"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("InitialDelayTicks"), new GUIContent("시작 대기 (틱)"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("IntervalTicks"), new GUIContent("반복 간격 (틱)"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("PressTicks"), new GUIContent("누르고 있는 시간 (틱)"));
    EditorGUILayout.HelpBox("기본 60틱 = 1초. 시작 대기는 라운드 조작 잠금이 풀린 뒤부터 계산합니다.\n반복 사이에는 버튼을 놓는 틱이 최소 1틱 들어갑니다. 끄면 키보드 입력으로 돌아옵니다.", MessageType.None);
    serializedObject.ApplyModifiedProperties();
  }
}
#endif
