# 피격 체력바 흔들림 — 2026-10-17

확정된 피격에 맞은 플레이어의 체력바 그룹만 짧게 흔들리도록 추가했다. 로컬 HUD의 왼쪽은 자신, 오른쪽은 상대라는 기존 연결을 유지한다. 양쪽 HUD에 각 체력바의 독립된 RectTransform을 명시적으로 연결해 상대 체력바나 전체 HUD를 움직이지 않는다.

## 동작과 설정

- `LSDF_IngameUI.HealthShakeDuration`: 기본 0.22초.
- `LSDF_IngameUI.HealthShakeStrength`: 기본 6 Canvas 단위.
- 두 값 중 하나가 0이면 흔들림을 끈다. Unity Inspector의 기존 LSDF_IngameUI에서 조정한다.
- VerifiedFrame의 HP 감소 또는 새 피격 상태/피격 종료 tick 변화를 읽는다. 피해가 0으로 계산되는 피격도 기존 isHit/DelayFrame 변화로 감지한다. 가드만으로는 흔들리지 않는다.
- 연속 피격은 기존 원점을 유지한 채 흔들림 시간을 다시 시작한다. 같은 확정 tick을 여러 렌더 프레임에서 읽어도 반복 발동하지 않는다.
- 감쇠하는 좌우·상하 진동을 unscaledDeltaTime으로 재생하고 종료 시 정확히 원위치로 돌린다. 회복·다음 라운드, HUD 해제·재연결, 과거 tick으로 이동할 때도 원위치를 복원한다.
- 시뮬레이션 상태·공격 판정·QTN·시스템 순서는 변경하지 않았다. VerifiedFrame 감지이므로 예측 취소에 따른 잘못된 체력바 흔들림을 피한다.

## 파일

- `Assets/QuantumUser/View/LSDF_IngameUI.cs`: HUD 연결과 View 전용 LSDFHealthBarShake.
- `Assets/QuantumUser/View/LSDFSceneBindings.cs`: 체력바 그룹 참조.
- `Assets/Scenes/LSDFGameplay.unity`: 두 HUD의 네 체력바 그룹을 연결.
- `Assets/Tests/Editor/LSDFStructuralTests.cs`: 양쪽 피격 대상 구분, 중복 tick, 가드/회복/피해 0/원점 복원·재바인딩, 씬 참조 검사.

## 검증

이번 UI 작업을 대상으로 Unity 컴파일, 관련 4개 사례, Quantum 정적 정책, 변경 diff를 구현 후 한 차례 실행한다. 이전 접속 종료 조사에서 보류한 온라인 회귀 검사는 이번 UI 검증에 포함하지 않는다. 실제 온라인 화면 육안 확인은 별도 미실행 범위다.

### 한 차례 실행 결과

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS: 오류 0, 테스트/기존 에셋 경고 5건. 제외 폴더 소스는 열지 않음 |
| Quantum 정적 정책 | PASS: 91개 파일, 기존·신규 적중 0. Simulation 변경 없음 |
| 변경 diff | PASS |
| 피격 대상 구분 | PASS: 왼쪽/오른쪽 각 1개 사례, 같은 verified tick 중복 발동 방지·원점 복원 포함 |
| 수명·경계 | PASS: 피해 0 피격, 가드, 연속 피격, 회복, Reset, HUD 교체, 과거 tick 복원 |
| 실제 씬 연결 검사 | FAIL: RectTransform 두 개를 Assert.AreNotEqual로 비교해 NUnit이 Transform의 자식 컬렉션으로 비교함 |
| 검사 코드 수정 | AreSame/AreNotSame 참조 비교로 수정 후 미검증. 게임 코드·씬 참조는 검사 뒤 변경하지 않음 |
| 온라인 화면 육안·실제 Photon 대전 | NOT RUN: 이번 검사 범위 아님 |

job `8ab22a0a9ee44326931de89fda5bd112`, 총 4개 중 **3 PASS / 1 FAIL / 0 SKIP**, 0.60초. 실패 검사 수정 후 재컴파일·재실행은 하지 않았다. 전체 통과로 기록하지 않는다. 원본 상태와 수정 범위는 [검증 기록](evidence/health-bar-shake-validation.json)에 남겼다.

### 후속 게시 요청의 검증

사용자가 프리팹의 `HealthShakeStrength`를 **20**으로 조정하고 현재 결과물 Push를 요청했다. 해당 프리팹을 포함한 새 게시 후보를 한 차례 검증해 **235 PASS / 0 FAIL / 0 SKIP**를 확인했다. 수정한 씬 참조 비교와 흔들림 검사도 이 묶음에서 통과했다. 시간은 0.22초로 유지한다.

이전 실패 기록은 위에 보존한다. 이번 통과는 자동 재실행이 아니라 새 게시 요청의 검증 결과다. 실제 화면 육안 확인·독립 빌드는 미실행이다. [게시 후보 검증](evidence/health-shake-publish-validation.json).
