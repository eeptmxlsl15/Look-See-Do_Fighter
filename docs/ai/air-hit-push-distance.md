# 공중 피격마다 뒤로 밀리는 거리와 Inspector 조절

작성: 2026-10-23T00:00:59+09:00. 기준 `dev/Prototype`, HEAD `b571a8f15e617788a0c86fe6507e992146f1800c`.

## 동작과 설정

사용자 요청: 공중 콤보를 맞을 때마다 뒤로 조금 밀리고, Inspector에서 그 값을 조절한다.
기존에는 속도 0.1·6틱으로 약 0.01 월드 단위만 이동했다. 밀림량을 직관적으로 조절하도록 거리 기준으로 바꿨다.

[LSDF_GameConfig.asset](../../Assets/QuantumUser/Resources/LSDF_GameConfig.asset)를 선택한 뒤 `Air Combo → 공중 피격 밀림`에서 조절한다.

| Inspector 항목 | 기본값 | 의미 |
|---|---|---|
| 공중 피격 1회 밀림 거리 | 0.05 | 공격자의 현재 위치 반대 방향으로 이동할 월드 거리. 0이면 밀림 해제 |
| 공중 피격 밀림 시간 (틱) | 6 | 설정한 거리를 이동하는 시간. 60Hz에서 약 0.1초. 0이면 밀림 해제 |

시동기와 일반 공중 후속타에 적용한다. 좌표를 즉시 바꾸지 않고 매 물리 틱의 가로 속도로 부드럽게 이동한다.
후속타마다 새 방향과 거리로 다시 시작한다. 기존 밀림 도중 재타격되면 이전의 남은 이동량을 누적하지 않는다.
일반 밀림 시간은 0~600틱으로 보정하고 음수 거리는 0으로 처리한다.
5번째 후속타 또는 지정 마무리 기술은 기존 `마무리 날리기 가로 속도`·위쪽 속도를 사용한다.
콤보 높이·낙하·70/50/30% 피해·5후속타 제한·착지/벽/KO/라운드 초기화 규칙은 유지한다.

## 구현

- [설정](../../Assets/QuantumUser/Simulation/LSDFJuggleSettings.cs)에 FP `HitPushDistance`를 추가했다.
  이전 `HitPushSpeed` 직렬화 필드는 기존 값과 함께 보존하고 Inspector에서 숨겼다. 새 밀림 계산에서는 사용하지 않는다.
- [피격 이동](../../Assets/QuantumUser/Simulation/LSDFJuggle.cs)은 거리 / 시간으로 속도를 구하고 설치된 물리 Drag를 보정한다.
  가로 속도·종료 틱은 기존 Frame 컴포넌트 `LSDF_AirCombo.HorizontalVelocity / PushUntilTick`에 저장한다.
  공유 설정·이벤트 에셋에 플레이어별 상태를 기록하지 않는다. `player.forceBack`도 실제 예약된 속도와 맞춘다.
- 공격자와 피격자의 실제 X를 비교하는 기존 방향 판정을 사용한다. 플레이어 번호만으로 방향을 고정하지 않는다.
- [매칭 계약](../../Assets/QuantumUser/Simulation/LSDFLoadout.cs)을 `lsdf-loadout-v15-air-hit-push-distance`로 올렸다.
  다른 밀림 규칙의 실행 버전과 같은 방에서 매칭되지 않도록 한다.
- 게임 설정 에셋에는 `HitPushDistance.RawValue=3277`만 추가하고 기존 속도 6554·시간 6·기타 설정과 asset ID를 보존했다.
  QTN/Generated/SDK/시스템 순서/카메라 코드는 수정하지 않았고 씬에 수동 패치하지 않았다.

## 한 차례 최종 검증

근거: [검증 JSON](evidence/air-hit-push-distance-validation.json).
job `9279bd97f6444372838e7e82c5ad23ad`: **99 PASS / 0 FAIL / 0 SKIP**.

| 검사 | 결과 |
|---|---|
| Unity 컴파일·콘솔 | PASS — 오류 0, 기존 SDK/테스트/외부 에셋 경고 10개 |
| Simulation 정적 정책 | PASS — 94개, 신규 적중 0. 정적 패턴 검사이며 결정론 증명은 아님 |
| 관련 변경 공백 | PASS — 한 차례 실행 |
| 공중 궤적 | 36 PASS |
| 밀림·마무리·방향·0 설정 | 30 PASS — 3/6/12틱과 Drag 유무에서 FP 이동량 적분, 반복 타격, 역방향, 타수와 마무리 보존 |
| 체력·공중 피해 | 32 PASS — 70/50/30%와 정수 버림·지상 피해·회복 보존 |
| 실제 Frame 저장/복원 | 1 PASS — 내부 8개: 두 Animator 그래프 × 두 피격 플레이어 × 일반/카운터. 밀림 도중 저장 후 복원·동일 가로 이동 재계산·전체 Frame 체크섬 일치 |
| Inspector 연결 | 위 궤적 검사 안에서 실제 에셋 거리값·SerializedProperty 두 항목 확인 |
| Photon 두 클라이언트·전체 입력 재생·강제 네트워크 rollback | NOT RUN |
| 실제 타격감 육안 평가 | NOT RUN |

Frame 검사는 실제 Quantum Frame에서 생산 피격 경로를 호출하고 가로 Drag 적분을 제어해 비교한다.
전체 물리 시스템 재생이나 네트워크 지연 rollback을 통과했다고 표현하지 않는다.
온라인 마무리 fixture는 밀림 비활성 조건을 새 거리 필드로 이관했으나 이번 job에서 실행하지 않았다.
이전 `AirHitInterruptsNativeGraphsAndSurvivesFrameRestore`의 보정 후 미검증 이력과 KazumiHoming 빈 공격 이벤트는 유지한다.
제품 코드·설정은 위 검사 후 변경하지 않았다. 검사 반복·커밋·게시도 하지 않았다.

## 기존 변경 보존

기존 무한 배경 View/Inspector/테스트·경기 씬·이미지 메타 변경, IDE 9개, 미추적 PNG/메타·csproj와 포트폴리오/빌드/문서를 보존한다.
종료 시 경기 씬 SHA256은 `d7203ebfe935a3ef85df73cc5ff05f7e6de70329d0052013e5346f78fbee9744`다.
앞선 배경 작업의 종료 해시와 다르며 현재 필드 직렬화 순서도 관측했다. 이번 작업 시작 파일 해시를 확보하지 않아
사용자 저장과 테스트의 씬 직렬화를 구분하지 못한다. 씬이 바이트 단위로 불변이라고 주장하지 않고 현재 상태를 보존한다.
이번에는 제품 C# 3개, 설정 에셋 1개, 기존 테스트 C# 4개와 이 작업 기록을 수정·추가했다.
다음은 사용자가 Inspector에서 거리·시간을 조절하며 타격감을 확인하는 작업이며 새 검증·커밋·게시 승인으로 간주하지 않는다.
