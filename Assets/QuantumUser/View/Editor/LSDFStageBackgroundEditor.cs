using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LSDFStageBackground))]
public sealed class LSDFStageBackgroundEditor : Editor {
  public override void OnInspectorGUI() {
    EditorGUI.BeginChangeCheck();
    DrawDefaultInspector();
    if (EditorGUI.EndChangeCheck()) {
      ((LSDFStageBackground)target).RefitReferenceView();
      SceneView.RepaintAll();
    }
    EditorGUILayout.HelpBox("Background Sprite에 PNG 스프라이트 하나를 연결하면 양쪽 카메라에 표시됩니다. " +
      "PNG Import Settings: Sprite (2D and UI), Sprite Mode Single, Filter Mode Point, Compression None. " +
      "Offset Y와 Minimum Height는 기준 거리에서의 위치·크기입니다. " +
      "카메라가 멀어지면 캐릭터와 배경이 같은 비율로 작아집니다. Fit Reference View는 기준 구도만 맞춥니다.", MessageType.Info);
  }
}
