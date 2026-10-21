# 카메라 높이 인스펙터 설정 — 2026-10-22

기준: `dev/Prototype`, HEAD `debf379c006e40a9a4e00884db3d27bfd111a4d9`.
요청: 체력바와 캐릭터 머리 사이의 여유를 카메라 높이로 조절하고 싶다.

[LSDFSceneBindings](../../Assets/QuantumUser/View/LSDFSceneBindings.cs)에 `Camera Follow → Camera Height`를 추가했다.
`LSDFGameplay → EntityViewUpdater`의 해당 컴포넌트에서 월드 Y 높이를 설정한다.
기본값 0은 기존 구도이며, 0.1부터 올려 보면서 머리 위 공간을 맞출 수 있다. 음수도 허용한다.
양쪽 플레이어 카메라의 추적 목표에 같은 값을 적용하고 기존 보간·거리 줌·카운터 줌·피격 흔들림 경로를 유지한다.
플레이 중에도 변경할 수 있으나 영구 보존할 값은 편집 모드에서 설정하고 씬을 저장한다.

변경은 View의 직렬화 필드와 추적 목표 Y에 한정한다. Simulation/QTN/Generated/SDK/에셋은 바꾸지 않는다.
시작 Git 상태는 선행 기록과 같은 HEAD이나 사용자 씬·Map 내용은 이후 변경되어 있었다.
이번 시작 SHA-256: 씬 `96cfbf6fcf04d656b51fb04d1d88b66f746c6db2aec689c6c13dd19169dea38a`,
Map `b18bcc38fab7b51c5e59b82e56a6279cd657b8e3fb02514622d44ad95480d49f`.
기존 배경 구현/테스트/문서, PNG 추가·삭제, IDE 및 사용자 씬·Map 변경을 보존한다.
씬 전체 재저장, staging, 커밋·푸시는 하지 않는다.

최종 검증은 한 묶음으로 정적 정책·관련 diff 공백·문서 링크, Unity 컴파일/콘솔,
프리뷰 씬의 Inspector 직렬화 및 양쪽 카메라의 기존 줌/흔들림 사례 8개를 한 번 확인한다.
실제 대전의 화면 여유는 사용자 튜닝 대상이며 온라인/rollback 검사는 이번 View 설정의 검증 범위에 포함하지 않는다.
이전 배경 원근 테스트 2개의 보정 후 미검증 상태는 유지한다.

## 최종 한 차례 결과

| 검사 | 결과·근거 |
|---|---|
| Unity 컴파일 | PASS — 컴파일/도메인 재로드 완료 후 새 타입 필드로 실행 가능, 컴파일 중/실패 플래그 false 검사 통과. 콘솔 오류·경고 0 |
| Inspector 기본값/적용/직렬화 | 기본 0, `SerializedObject`로 0.1/-0.1/0.2 적용 및 `EditorJsonUtility` 왕복 확인까지 통과. 아래 부가 참조 확인에서 전체 프로브 FAIL |
| 프리뷰 참조 확인 | FAIL — 같은 프리뷰 컴포넌트 전체 JSON을 덮어쓴 뒤 `Player camera reference missing`. 원본 씬의 두 참조는 fileID 1535058701/953246092로 존재. 왕복 전 참조 상태는 따로 수집하지 않아 원인 미확정. 재실행/원본 씬 수정 없음 |
| 기존 줌/흔들림 | 8 PASS / 0 FAIL / 0 SKIP — job `b35629b0578d4975a1160e7fcea0776d`, 0.5923315초. `LSDFCameraHitShakeTests.EitherFighterShakesTheWholeCameraInScreenAxesAndRestoresItsPosition` 4개와 `LSDFCounterHitZoomTests.BothPerspectivesCenterTheHitAndRestoreTheMovingFollowCamera` 4개. 새 높이로 실제 대전하는 검사는 아님 |
| 정적 정책 | PASS — `python tools/ai/quantum_guard.py check`, Simulation 94개/신규 0. 결정론 증명은 아님 |
| diff 공백 | UNKNOWN — 공백 검사와 후속 diff 조회를 합친 명령에서 후속 Git이 `Not a git repository`로 종료 1. 공백 검사 개별 종료 코드 미확보, 검사 재실행 없음. 정상 권한의 읽기 전용 diff로 제품 변경이 필드 3줄/목표 Y 1줄에 한정됨을 확인 |
| 문서·사용자 파일 보존 | PASS — 결과 기록 전 상대 링크 34개 유효. 씬/Map 최종 SHA-256이 위 시작 해시와 일치 |
| 실제 게임 구도·온라인/rollback | NOT RUN |

검증 실패를 전체 PASS로 처리하지 않는다. 실패한 프로브·공백 검사를 다시 실행하지 않았고 제품 코드는 검증 후 변경하지 않았다.
현재 작업 상태와 결과 설명만 검사 후 기록했다. 이 문서는 미추적이며 자동 커밋하지 않는다.
