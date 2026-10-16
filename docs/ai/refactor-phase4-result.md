# 4차 리팩토링 — 나락/Lars 다단히트 정의 분리

2026-10-16. 기존 변경을 보존하고 NarakTwo/LarsCutKick 두 기술의 생성 코드를 이관했다.

## 변경 사항

- `LSDFTimedHitDefinition`이 실제 생성 애니메이션 프레임과 읽기 전용 타격 정의를 가진다.
  `FirstHit`/`SecondHit`은 각 기존 Quantum 이벤트 에셋에 직렬화한다.
- 실제 생성은 `LSDFHitboxFactory`를 사용한다. 공통 정의에 jump/dodge/launcher 플래그를 추가했다.
- 애니메이션 프레임에서 생성 여부를 매번 계산하므로 공유 에셋에 실행 커서/타이머를 저장하지 않는다.
  중단된 기술의 후속타를 예약 큐로 강제로 실행하지 않는다. 기존 같은 프레임 재평가 동작도 유지한다.
- 이동·회피 구간·카운터 잠금·종료 처리·시스템 순서를 유지했다. DSL/CodeGen 변경은 없다.
- CP949 소스는 `tools/ai/refactor_phase4.ps1`로 필요한 생성 구간만 바이트 보존 치환했다.
  새 정의/기존 이벤트의 GUID 및 asset ID는 유지했다.

## 보존 계약

| 타격 | 실제 생성 애니메이션 프레임 | 피격 정보 startFrame | 피해 | 가드/히트/카운터 | 속성 |
|---|---|---|---|---|---|
| 나락 첫 타 | 17 | 18 | 15 | 20/20/20 | Low, dodgeHigh, Combo guard/parry |
| 나락 둘째 타 | 35 | 18 | 27 | 20/29/29 | Mid, Combo guard/parry |
| Lars 첫 타 | 15 | 16 | 12 | 21/28/28 | Mid, Air counter, Stun guard/parry, jump/dodge/launcher |
| Lars 둘째 타 | 25 | 16 | 14 | 21/28/28 | 첫 타와 같은 속성 |

생성 시각과 `startFrame`은 다른 계약이다. 둘째 타의 메타데이터를 36/26으로 바꾸지 않았다.
모든 타격은 생성 시뮬레이션 tick + 1에 파괴한다.
나락 첫 타는 기존 하단 offset/반크기, 나머지는 기존 중단 offset/반크기를 그대로 사용한다.

Lars 첫 타는 기존 코드에 `HitboxInfo.position` 할당이 없어 효과 위치가 원점이었다.
이관 중 효과 위치까지 바뀌지 않도록 공통 생성기의 명시적인 효과 위치 override로 원점을 유지했다.
다른 타격은 실제 히트박스 위치를 기록한다. 원점 효과를 수정하는 것은 별도 버그 작업으로 남긴다.

## 검증 범위

- 이전에 추가한 QuantumLookupTables 전역 설정과 6개 테이블 참조를 실제 에셋 검사에 포함한다.
- 새 단위/에셋 검사 5건: 4타의 생성/메타데이터/수치/속성/반전 계약 + LUT 참조.
- 온라인 테스트는 서로 다른 로드아웃의 두 연결로 잽/왼어퍼/나락/Lars 입력을 수행한다.
  입력은 세션 시작 후 공통 기준 tick을 정해 실행하고, 피해·타격별 관측·수명·효과 위치·같은 verified tick 체크섬을 확인한다.
- 온라인 세션 종료와 백그라운드 설정 복원은 기존 정리 경로를 사용한다.
- 한 번의 최종 검증 묶음만 수행하며 실패 후 자동 재실행하지 않는다.

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS — 검사 당시 오류 0건 |
| 단위/에셋/LUT | PASS — 32건. 기존 27 + 다단히트 4타 계약 + LUT 1건 |
| 온라인 2클라이언트 | FAIL — 자동 Unity 씬 전환의 ArgumentException: previousSceneName |
| 정적 정책/diff/문서 링크 | PASS — Simulation 84개, 기존 적중 5건, 신규 0건. 후속 실행기 수정 전 기준 |
| 강제 지연/입력 재생/실제 재접속/독립 빌드/UI | NOT RUN |

테스트 작업 ID: `90c79d0e28b240b1a64446990abe217a`. 총 33건, 32 PASS / 1 FAIL / 0 SKIP.
[실제 테스트 결과](evidence/phase4-test-results.json)에 원본 결과와 스택을 보존했다.
앞선 LUT 구성은 참조 검사에서 통과했고 이번 온라인 실패의 스택에는 기존 SqrtRaw 오류가 없다.

### 온라인 실패 분석과 수정 후 미검증 사항

실제 스택은 `QuantumCallbackHandler_UnityCallbacks.SwitchScene`의 `previousSceneName` 검사다.
Test Runner의 이름 없는 임시 씬을 이전 씬으로 취급하는 SDK 자동 로드 경로에서 실패했다.
시뮬레이션 전용 온라인 검사이므로 실행 중 `SimulationConfig.AutoLoadSceneFromMap`을 메모리에서만
Disabled로 두고 두 Runner 종료 후 원래 값으로 복원하도록 보완했다.
설정 에셋 파일은 저장/수정하지 않으며 전투 물리/시스템 구성은 바꾸지 않는다.
테스트 도중 외부 로그로 iterator가 중단돼도 비동기 접속/대기가 취소되도록 취소 토큰을 연결했다.

이 두 보완은 **테스트 실행기 수정 후 미검증**이다. 한 차례 실행 방침에 따라 재시도하지 않았고
예외 로그를 무시하거나 온라인 판정을 PASS로 바꾸지 않았다.
32건의 PASS는 이번 다단히트 코드/데이터와 LUT 참조에 대한 결과이며, 온라인 동기화 통과의 근거는 아니다.
다음 검증 때는 이 실행기 보완의 컴파일과 실제 전투/체크섬 관측까지 확인해야 한다.

관련: [3차 결과](refactor-phase3-result.md), [전체 백로그](refactoring-backlog.md).
후속 구조 작업 후보는 나머지 단타 이관 또는 PlayerSystem의 피해·상태 처리 분리다.
전체 37개 공격 이관, 비용 카탈로그, 경기 수명/슬롯 교체 정책은 아직 완료되지 않았다.
