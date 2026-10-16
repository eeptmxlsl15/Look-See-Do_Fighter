# 5차 리팩토링 — 전투 시그널과 체력 정책 분리

2026-10-16. R05의 첫 단계로 PlayerSystem의 전투 피격 처리와 체력 계산을 분리했다.

## 변경 구조

- `LSDF_PlayerSystem.cs`: 기존 Update, 이동/대시, 바닥/벽 충돌을 유지한다.
- `LSDF_PlayerSystem.Combat.cs`: 일반/카운터/가드/공격자 가드 반응/패링/런처 시그널 메서드를 옮긴 partial이다.
  런타임에는 기존 `Quantum.LSDF.LSDF_PlayerSystem` 하나이며 새 시스템을 등록하지 않는다.
- `LSDFPlayerHealth.cs`: Frame 소유 `LSDF_Player`를 ref로 받아 회복, 일반 피해, 카운터 피해를 처리하는 stateless 헬퍼다.
  공유 가변 필드, asset 실행 상태, 로컬 저장소, Unity 시간/난수를 추가하지 않는다.
- Spawn의 HP 초기화도 같은 `Maximum = 170`을 참조한다. 밸런스 에셋을 도입하는 R06 전체 구현은 아니다.

기존 시스템 GUID/등록 순서/시그널 인터페이스/Animator 트리거/효과와 소리 이벤트 순서를 유지한다.
새 QTN 필드나 직렬화 변경이 없으므로 CodeGen을 변경하지 않는다.
기존 CP949 바이트는 `tools/ai/refactor_phase5.ps1`의 anchor 기반 치환/이동으로 보존했다.
새로 이동한 Combat 파일의 행 끝 공백과 변경한 class 선언의 행 끝 공백만 정리했다.

## 동작 보존 계약

| 상황 | 유지한 결과 |
|---|---|
| 일반 피격 hitCount 0 | 기본 피해 |
| hitCount 2 / 4 / >4 | 기존 FP 0.75 / 0.50 / _0_33 계산 후 정수 변환 |
| hitCount 1 / 3 및 음수 | 기존처럼 피해 미적용. B02 버그 후보를 여기서 고치지 않음 |
| 기본 피해 3, hitCount >4 | `FP._0_33` raw 21845이므로 결과 0. 정확한 1/3으로 바꾸지 않음 |
| 카운터 | 기존 LSDFCombatMath의 6/5 계산, hitCount 보정 없음 |
| 치명적 피해 | 그 자리에서 0으로 clamp하지 않고 기존 Update 시점까지 음수 HP 유지 |
| Update 회복 | 음수 clamp → healing일 때 +2 → 170 이상이면 170 및 healing 해제 |
| 라운드 잠금 | 회복 처리 후 기존 tickToEnableMove 검사. healing을 켠 tick에는 추가 회복하지 않음 |

피격 메서드는 기존처럼 이벤트/반응 → isHit/DelayFrame → 피해 순서다.
일반 피해의 원래 로그 분기와 메시지는 보존하고 HP 대입만 헬퍼 호출 한 곳으로 옮겼다.
체력 외 hitCount·상태 플래그·경직 tick을 헬퍼가 대신 변경하지 않는다.

## 검증 범위

새 단위 검사 23건: combo 경계/FP 버림 13건, 회복 순서/완료 8건,
음수 HP 지연 clamp 및 카운터/플레이어 독립성 2건.
기존 단위/에셋 검사와 Photon 온라인 2클라이언트 시나리오를 같은 한 차례 최종 검증에 포함한다.
정적 정책, 컴파일, 게시 후보 비밀정보/불필요 파일 검사를 수행한다.
강제 rollback/실제 재접속/독립 빌드 검증을 했다고 주장하지 않는다.

최종 한 차례 결과:

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS — 오류 0건. MCP WebSocket 초기화 경고 1건 기록 |
| 체력 정책 단위 검사 | PASS — 새 23건 |
| 기존 단위/에셋 검사 | PASS — 32건 |
| Photon 온라인 2클라이언트 | PASS — 1건, 실제 전투/타격/verified-frame checksum/반복 AddPlayer |
| Quantum 정적 정책 | PASS — Simulation 86개, 기존 5건, 신규 0건 |
| 추적 변경 diff 및 문서 링크 | PASS |
| 게시 후보 파일 검사 | 알려진 비밀정보 패턴/미검사 항목 0건 — index 12개 blob |
| 최종 staged diff / 새 .meta 짝 | PASS |

job `e3830f86012c4ed48787e166d54630de`: 총 56 PASS / 0 FAIL / 0 SKIP.
[원본 결과](evidence/phase5-test-results.json)에 모든 테스트와 스택/출력을 기록했다.
이전 온라인 재검사 결과를 재사용하지 않고 이번 변경을 대상으로 전체 검사를 한 번 실행했다.
게시 검사 이후에는 이 결과 기록만 추가하고 런타임/테스트 코드를 변경하지 않았다.

## 남은 범위

R05의 이동/충돌/상태 전이 정책 분해는 계속 남아 있다. partial 분리는 별도 시스템 분리를 뜻하지 않는다.
B02 피해 보정의 1/3 분기, B03 라운드의 음수 HP 처리, 기존 포인터 가드, 라운드 기능은 이번에 변경하지 않았다.
관련: [백로그](refactoring-backlog.md), [4차](refactor-phase4-result.md).
