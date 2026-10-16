# 3차 리팩토링 — 잽 계열 타격 정의 분리

2026-10-16. 이전 1·2차 변경이 있는 작업 트리를 보존하고 R04를 잽 4종으로 확장했다.

## 적용 범위

- `JapWindowEvent`, `JapJapWindowEvent`, `JapTwoWindowEvent`, `JapJapTwoWindowEvent`의
  히트박스 생성/피해/경직 정의를 `LSDFSingleHitDefinition` 및 기존 `LSDFHitboxFactory`로 연결했다.
- 4개 기존 이벤트 에셋에 `Hit` 데이터를 직렬화했다. 파일/타입/GUID/Quantum asset ID/Animator 참조는 유지했다.
- 공통 정의에 `ForceBack`을 추가해 섬광 마지막 타의 기존 밀어내기 1을 보존했다.
  기존 왼어퍼와 다른 잽의 기본 밀어내기는 0이다.
- Frame의 `BufferedNextAttack`, 기존 입력 예약 구간, `NextAttack` 전이/초기화,
  전진 속도, 피격 중단과 종료 처리, 시스템 실행 순서는 유지했다.
- CP949 파일의 필요한 구간만 `tools/ai/refactor_phase3.ps1`로 일회성 바이트 보존 치환했다.
  이미 이관된 파일에 해당 스크립트를 다시 실행하지 않는다.

## 보존한 타격 데이터

| 이벤트 | 발동/생성 프레임 | 피해 | 가드/히트/카운터 경직 | 높이 | 가드/패링 반응 | 밀어내기 |
|---|---|---|---|---|---|---|
| Jap | 10 / 9 | 7 | 21 / 28 / 28 | High | Normal / Stun | 0 |
| JapJap | 10 / 9 | 7 | 21 / 28 / 28 | High | Normal / Stun | 0 |
| JapTwo | 8 / 7 | 12 | 19 / 22 / 28 | High | Normal / Stun | 0 |
| JapJapTwo | 5 / 4 | 7 | 21 / 29 / 29 | Mid | Combo / Combo | 1 |

상단 3종의 offset/half extents는 기존 FP 식 `(0.25+0.05, 0.25)` 및
`(FP._0_20/2, (FP._0_10-FP._0_02)/2)`를 유지한다.
마지막 타는 기존 중단 offset `(0.25,0)` 및 왼어퍼와 같은 반크기다.
모든 타격은 생성 시뮬레이션 tick + 1에 파괴한다.
연타 전체를 하나의 범용 상태 기계로 바꾸거나 다단히트 기술까지 이관하지 않았다.

## 온라인 실행기 보완

2차 실행에서 확인한 사실은 세션 시작 타임아웃과 5초 이상 네트워크 Service 공백이다.
기존 실행기는 첫 클라이언트의 matchmaking이 완료돼도 두 번째 접속이 끝나야 첫 Quantum 세션을 시작했다.
설치 SDK의 matchmaking Service task와 QuantumRunnerBehaviour.Update 경로를 확인한 뒤,
각 클라이언트 접속 직후 해당 세션 시작을 예약하도록 바꿨다.
백그라운드 실행과 원래 값 복원은 2차 보완을 유지했다. 시작 제한은 60초, 전체 취소 제한은 90초다.

두 플레이어의 중립 스킬을 서로 다르게 설정하고 잽/후속타/왼어퍼를 입력한다.
같은 verified tick 체크섬, 서로 다른 로드아웃, 중복 스폰 방지, 피해와 타격 생성 관측을 확인한다.
실패 시 접속/세션 시작/전투/결과 비교 중 어느 단계인지 기록한다.
실제 온라인 실행 결과가 나오기 전에는 타임아웃 문제가 해결됐다고 판단하지 않는다.

## 검증 결과

최종 검사 묶음을 한 차례 실행한다. 새 에셋 계약 테스트는 기존 수치·위치·속성·판정 틱을 실제 에셋에서 확인한다.

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS — 오류 0건, 기존 obsolete/SDK/제외 에셋 경고는 남음 |
| 기존 및 잽 에셋 계약 검사 | PASS — 27건: 기존 23 + 잽 4종 에셋 계약 4 |
| 온라인 2클라이언트/체크섬 | FAIL — FPMath.SqrtRaw에서 NullReferenceException, 전투·체크섬 판정 미도달 |
| 정적 정책/diff/문서 링크 | PASS — Simulation 83개, 기존 패턴 5건, 신규 0건. 후속 LUT 수정 전 결과 |
| 강제 지연·롤백 재생·교차 플랫폼·독립 빌드·UI | NOT RUN |

실제 Unity 테스트 작업 ID: `b8a734f4fdf2472694af5673b381379e`.
총 28개 중 27 PASS / 1 FAIL / 0 SKIP. [실제 테스트 결과](evidence/phase3-test-results.json)를 보존했다.

### 온라인 실패 원인과 후속 수정

2차의 단순 타임아웃과 달리 이번에는 서버의 SimulationStart 메시지를 처리하다
`FPMath.SqrtRaw → TriangleMesh → PhysicsEngine3D → FrameContext` 초기화에서 실패했다.
SDK의 초기화/테이블 로딩 코드를 추적했고, Unity MCP 읽기 전용 진단에서
`FPLut.IsLoaded=False`, `QuantumLookupTables.TryGetGlobal=False`를 확인했다.
기존 2D/3D 시스템 구성은 그대로이며, Physics3D를 임의 제거해 오류를 감추지 않았다.

프로젝트 기본 경로 `Assets/QuantumUser/Resources/QuantumLookupTables.asset`에 전역 설정이 없었다.
설치 SDK의 스크립트 importer가 가리키는 것과 같은 6개 LUT 파일 GUID를 연결한 에셋과 `.meta`를 추가했다.
테스트 실행기에도 세션 연결 전에 SDK 초기화 및 LUT 로드 여부 확인을 추가했다.
이는 **실패 후 수정이며 미재검증**이다. 온라인 테스트는 한 번만 실행했고 예외 로그를 무시하도록 바꾸지 않았다.
위 27개 통과는 잽 이관 코드/에셋에 대한 근거이며, LUT 에셋/후속 테스트 초기화 변경의 검증은 포함하지 않는다.
Editor는 테스트를 종료해 메뉴 씬으로 돌아왔으며 백그라운드 실행 설정도 원래 false로 복원됐다.

다음 검증에서는 LUT 수정 후 온라인 전투가 시작되는지 먼저 확인해야 한다.
후속 구조 작업은 NarakTwo/LarsCutKick의 다단히트 정의와 단계별 생성 상태를 분리하는 것이다.
로드아웃 비용 규칙과 실제 재접속/슬롯 교체 정책은 계속 별도 미완료 항목으로 남긴다.
관련 이력: [2차 결과](refactor-phase2-result.md), [백로그](refactoring-backlog.md).
