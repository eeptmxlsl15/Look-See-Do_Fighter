# 멀티플레이 리팩토링 백로그

작성일: 2026-10-16. 대상: Look-See-Do Fighter / Photon Quantum 3.

이 문서는 **후속 작업 후보**이며 구현 완료 보고서가 아니다. 현재 소스를 다시 확인한 결과와
[초기 감사](architecture-audit.md)를 바탕으로 작성했다. 온라인 대전에서 desync나 크래시를 재현한 것은 아니다.
이번 작업에서는 문서만 변경하며 게임 코드·씬·프리팹·설정 에셋은 변경하지 않는다.
`DinV`, `GabrielAguiarProductions`, `Matthew Guz` 폴더는 조사 대상에서 제외했다.

## 1. 우선순위와 권장 순서

- P0: 서로 다른 클라이언트의 동일한 시뮬레이션 결과와 롤백 복원을 위협하는 구조.
- P1: 전투 확장·밸런스 조정·라운드 유지보수에 직접 영향을 주는 구조.
- P2: 표현 계층·메뉴·프로젝트 정리. 관련 기능 개발 시 먼저 처리할 수 있다.
- B: 동작을 바꾸는 버그 수정 후보. 동작 보존 리팩토링과 별도 작업으로 기록한다.
- T: 검증 기반 구축. 자동화 완료로 오해하지 않도록 별도 관리한다.

| ID | 우선순위 | 작업 | 변경 위험 | 선행/연계 |
|---|---|---|---|---|
| R01 | P0 | 로드아웃의 로컬 저장소 의존 제거 | 높음: 전달/직렬화 | 첫 구현 권장 |
| R02 | P0 | 공격 틱·피해 계산의 float 제거 | 높음: 타이밍/반올림 | R03과 별도 작업 가능 |
| R03 | P0 | 공유 공격 에셋의 실행 상태 분리 | 높음: DSL/롤백 | R04 이전 |
| R04 | P1 | 공격 정의 데이터화와 히트박스 생성 공통화 | 높음: 공격별 예외 | R02·R03 이후 |
| R05 | P1 | PlayerSystem 책임 분리 | 높음: 실행/시그널 순서 | R02 이후, R04와 연계 |
| R06 | P1 | 밸런스 설정과 스키마 계약 분리 | 중간: 에셋 연결 | R01·R04와 연계 |
| R07 | P1 | 라운드 상태와 참가자 수명 명시화 | 높음: 대전 흐름 | R01과 연계 |
| R08 | P1 | 충돌 판정과 포인터 접근 계약 정리 | 중간~높음 | R05와 연계 |
| R09 | P2 | View/UI의 Runner·엔티티 소유권 분리 | 중간: 재접속/시점 | 독립 진행 가능 |
| R10 | P2 | 효과/사운드 구독 및 예측 이벤트 정책 정리 | 중간: 중복/취소 | R09와 연계 |
| R11 | P2 | 메뉴·저장·커맨드 매핑 경계 정리 | 중간: 저장 호환성 | R01 이후 |
| R12 | P2 | 빈 코드·샘플 잔재·폴더/의존성 정리 | 중간: Unity 참조 | 핵심 구조 안정화 이후 |
| T01 | 기반 | 실제 두 클라이언트 검증 실행 장치 | 높음: 환경/관측 | P0 검증과 병행 준비 |

권장 순서: T01의 최소 실행·관측 기반 준비 → R01 → R02 → R03 → R04/R06 → R05/R08 → R07 → R09~R12.
전체를 한 번에 구현하거나 모든 항목이 끝날 때까지 기능 개발을 막지 않는다.
각 ID를 독립 변경 단위로 삼고 관련 버그 수정은 의도적으로 달라지는 결과를 명시한다.

## 2. 최우선: 결정론과 상태 소유권

### R01 — 로드아웃 전달 경로 수정

- 근거: [LSDF_PlayerSpawnSystem.cs](../../Assets/QuantumUser/Simulation/LSDF_PlayerSpawnSystem.cs)의
  `OnPlayerAdded`가 `PlayerPrefs.GetInt($"Skill_{i}", 0)`를 28회 읽어 `CommandSkillMap`에 넣는다.
  [RuntimePlayer.User.cs](../../Assets/QuantumUser/Simulation/RuntimePlayer.User.cs)에는 현재 `IsLocalPlayer`만 있다.
- 위험: 동일 플레이어의 스킬맵이 각 클라이언트의 로컬 저장값으로 만들어질 수 있다. 로컬 저장값 차이가 게임 상태 차이로 이어질 구조다.
- 방향: 메뉴에서 선택값 수집 → payload 구성 → 기존
  [AddPlayer 호출 경로](../../Assets/Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviourSDK.cs) → `f.GetPlayerData()` → Frame 복사.
  설치된 Quantum 3의 직렬화 경로를 사용하고 Quantum 2의 `SerializeUserData` 방식을 도입하지 않는다.
- 범위: 슬롯 길이/순서, 허용 ID, 비용, 기본값, 잘못된 avatar, 참가·재참가 중복 처리.
  UI 검증과 별개로 수신 데이터를 모든 클라이언트에서 동일한 규칙으로 검증한다. `firstTime` 의미를 확인한 뒤 스폰 정책을 정한다.
- 완료 조건: Simulation에서 `PlayerPrefs`를 읽지 않으며, 서로 다른 로드아웃의 A/B가 접속해도
  양쪽 Frame에서 각 PlayerRef의 28개 스킬 ID가 일치한다. 재접속 시 의도하지 않은 중복 엔티티가 생기지 않는다.
- 제외: 게임 도중 로드아웃 변경 기능과 전역 저장 데이터 초기화.

### R02 — 판정 수학을 FP/정수로 통일

- 근거: [JapWindowEvent.cs](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/JapWindowEvent.cs),
  [NarakTwoWindowEvent.cs](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Rk/NarakTwoWindowEvent.cs),
  [AirWindowEvent.cs](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Move/AirWindowEvent.cs) 등에
  `(int)(layerData->Time.AsFloat * 60.0f)`가 있다. 초기 감사에서 공격 이벤트 37개에 해당 패턴을 확인했다.
  [LSDF_PlayerSystem.cs](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs)의 카운터 피해는 `attackDamage * 1.2f`다.
- 방향: FP/정수 기반의 명시적인 틱 환산 및 피해 계산으로 치환한다. 절삭 시점, 음수 여부, 범위와 오버플로를 고려한다.
  애니메이션 기준 60과 세션 업데이트 주기의 관계를 확인하고, 단순히 모든 60을 설정값으로 바꾸지 않는다.
- 완료 조건: 활성 게임 판정 경로에 float 변환/계산이 없고, 공격 시작·히트박스 활성·피해량의 경계 사례가 목표값과 일치한다.
- 멀티 검증: 양쪽 같은 verified tick의 결과 비교, 기록 입력 재생, 지연으로 실제 롤백이 발생한 상황에서 결과 비교.
- 제외: View의 렌더링용 float 제거, 세션 틱레이트 변경, 공격 밸런스 조정.

### R03 — 공격 에셋의 가변 상태를 Frame으로 이동

- 근거: [JapWindowEvent.cs](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/JapWindowEvent.cs)와
  [JapJapWindowEvent.cs](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/JapJapWindowEvent.cs)의
  `bufferedNextAttack`은 공유 `AnimatorTimeWindowEventAsset` 필드다. `currentFrame`도 에셋 필드에 기록된다.
- 방향: 연타 예약처럼 다음 틱까지 필요한 값은 엔티티별 `.qtn` 상태로, 매 호출 재계산 값은 지역변수로 옮긴다.
  사용하지 않는 버퍼는 실제 사용처를 확인하고 제거한다. 모든 필드를 무조건 컴포넌트로 옮기지 않는다.
- 주의: 설치된 Animator 애드온의 `Execute`/`OnEnter` 평가 순서를 확인하고 진입·중단·종료 초기화를 설계한다.
- 완료 조건: 같은 공격 에셋을 쓰는 두 엔티티가 버퍼를 공유하지 않으며 롤백 후 연타 예약이 복원된다.
- 멀티 검증: A만 후속타 입력/B는 미입력, 양쪽 동시 입력, 한쪽 피격 중단을 구분해 후속타와 HP를 확인한다.

## 3. 전투 확장을 위한 구조 개선

### R04 — 공격별 복사 코드를 정의 데이터와 실행기로 분리

- 근거: [공격 이벤트 폴더](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack)의 개별 클래스가
  자세 초기화, Transform/Collider/Hitbox 생성, 피해·경직·크기 설정, `TickToDestroyAt = f.Number + 1`을 반복한다.
- 방향: 불변 공격/타격 정의에 FP 수치와 타이밍을 두고 stateless 생성 helper를 공유한다.
  실행 중 타격 단계·예약·중복 방지 상태가 필요하면 Frame에 둔다. 기존 이벤트 타입은 참조 보존용 어댑터로 남길 수 있다.
- 이관 순서: 단타 하나 → Jap 연타 → NarakTwo/LarsCutKick 다단히트 → 런처/벽/패링 예외.
- 완료 조건: 데이터 변경이 실제 대전에 반영되며 공격별 활성 틱, 타격 횟수, 소유자, 중단 시 정리가 보존된다.
  37개 클래스를 범용 클래스 하나로 일괄 교체하거나 모든 공격에 과도한 옵션을 만들지 않는다.

### R05 — PlayerSystem을 책임별로 분리

- 근거: [LSDF_PlayerSystem.cs](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs)에 이동, 입력 잠금/회복,
  일반·카운터·가드·패링 피격, 벽/바닥 반응이 모여 있다.
- 방향: 먼저 순서를 유지하는 stateless helper로 이동/피해/상태 전이를 추출한다.
  별도 시스템이 필요할 때만 분리하고 [실제 시스템 설정](../../Assets/Resources/LSDF_SystemConfig.asset)에 반영한다.
- 완료 조건: 한 규칙의 수정 위치가 명확하고 기존 신호 호출 순서·HP·경직 종료 틱이 보존된다.
  `SystemSetup.User.cs`와 설정 에셋에 중복 등록하지 않는다. 시스템 필드에 플레이어 상태를 캐시하지 않는다.

### R06 — 하드코딩을 의미에 따라 분류

- 근거: Spawn/PlayerSystem의 HP 170, IngameSystem의 잠금 210틱, 이동 속도, 공격별 피해/경직이 여러 위치에 흩어져 있다.
  [LSDF_GameConfig.cs](../../Assets/QuantumUser/Simulation/LSDF_GameConfig.cs)는 `AsteroidInitialSpeed` 중심의 샘플 잔재다.
- 방향: HP·이동·공격·라운드 밸런스는 불변 Quantum 설정 에셋, 표현 수치는 Unity View 설정으로 분리한다.
  방향 7 × 버튼 4 = 28은 [플레이어 DSL](../../Assets/QuantumUser/Simulation/LSDF_Player.qtn)까지 연결된 스키마 계약으로 관리한다.
- 완료 조건: 기본값을 유지하고 모든 소비 지점이 실제 설정을 읽는다. 에셋만 추가하고 기존 상수를 남기는 상태는 미완료다.
- 제외: 모든 숫자 설정화, 스킬 ID/슬롯 순서 변경, 기존 asset ID/GUID 재생성.

### R07 — 라운드 상태·참가자 수명 정리

- 근거: [LSDF_IngameSystem.cs](../../Assets/QuantumUser/Simulation/LSDF_IngameSystem.cs)가 매 Update 참가자를 찾고
  `PlayerRef == 0`/그 외로 분류한다. HP로 패배·리셋을 처리하며 타이머 코드는 주석 처리되어 있다.
  `lastTimeTickFrame` 필드는 있지만 현재 타이머가 작동한다는 근거는 아니다.
- 방향: 대기/전투/라운드 종료/경기 종료 상태와 진입 시 한 번 수행할 처리를 Frame에 명시한다.
  1v1 참가자 식별, 퇴장/재접속, 동시 KO 정책을 메뉴·스폰·UI와 맞춘다.
- 완료 조건: 패배 횟수/종료 이벤트가 의도한 횟수만 발생하고 재접속 후 상태가 일치한다.
- 분리할 기능: 제한시간 종료, 3승 종료, 재대전 기능은 현재 동작의 단순 정리가 아니다. 요구사항을 명시한 별도 기능 작업으로 진행한다.

### R08 — 충돌 조건과 포인터 접근 계약 명시

- 근거: [LSDF_CollisionSystem.cs](../../Assets/QuantumUser/Simulation/LSDF_CollisionSystem.cs)의 `static public bool isGuard`와
  일부 `TryGetPointer` 결과를 확인하지 않는 역참조/시그널 전달 경로.
- 방향: `isGuard` 외부 사용처를 확인한 뒤 지역 결과로 바꾼다. 필터/프로토타입이 보장하는 컴포넌트와
  런타임에 없을 수 있는 컴포넌트를 구분하고, 누락 시 실패/무시 정책을 정한다.
  가드·회피·카운터·패링 우선순위는 명시적인 판정 함수로 분리한다.
- 완료 조건: 기존 공격 조합의 판정 우선순위를 보존하고 제거된 엔티티/잘못된 프로토타입에서 잘못된 포인터를 사용하지 않는다.
  `isGuard`가 존재한다는 사실만으로 실제 desync를 단정하지 않는다.

## 4. 표현·메뉴·프로젝트 경계

### R09 — View/UI의 소유권과 바인딩 수명 정리

- 근거: [LSDF_ViewHandler.cs](../../Assets/QuantumUser/View/LSDF_ViewHandler.cs)의 static 카메라/플레이어 참조와
  `GameObject.Find`, [LSDF_IngameUI.cs](../../Assets/QuantumUser/View/LSDF_IngameUI.cs)의 `QuantumRunner.Default` 및 이름 검색.
- 방향: 실제 Runner/Game, Local Player, Entity에 묶인 바인딩과 해제 경로를 만들고 씬 참조를 명시적으로 연결한다.
- 완료 조건: A/B 각 클라이언트의 자기 시점·HP UI가 올바르고 메뉴 복귀/재접속 후 이전 세션 참조가 남지 않는다.
  동일 프로세스 멀티러너 테스트를 지원하려면 Runner별 격리도 확인한다.

### R10 — 효과·사운드의 구독/롤백 정책 정리

- 근거: [LSDF_HitEffectView.cs](../../Assets/QuantumUser/View/LSDF_HitEffectView.cs)는 엔티티 View에서 전체 효과 이벤트를 구독한다.
  [SfxManager.cs](../../Assets/Scripts/SfxManager.cs)에도 전투 이벤트 구독이 있다.
- 방향: 게임 단위 단일 표현 담당 또는 엔티티 식별 필터 중 실제 씬 구조에 맞는 방식을 선택한다.
  설치 SDK의 자동 구독 해제 동작부터 확인한다. 메서드가 안 보인다는 이유만으로 누수를 단정하지 않는다.
  예측 효과의 취소/중복 억제와 확정 전용 효과를 구분한다.
- 완료 조건: 두 플레이어 View가 존재해도 한 타격의 효과/사운드가 의도한 횟수만 재생된다.
  재접속·재활성화·롤백 이후 중복/잔상이 없는지 확인한다. 모든 효과를 확정 전용으로 바꿔 입력 반응을 늦추지 않는다.

### R11 — 메뉴 로드아웃 모델·저장소·표현 분리

- 근거: [SkillSelectionManager.cs](../../Assets/Photon/QuantumMenu/UnityUI/Scripts/SkillSelectionManager.cs)에 UI·비용·저장이 결합되어 있다.
  [SkillSetPanelUI.cs](../../Assets/Photon/QuantumMenu/UnityUI/Scripts/SkillSetPanelUI.cs)도 저장하고,
  [Legacy 저장 코드](../../Assets/Photon/QuantumMenu/UnityUI/Scripts/Legacy/SkillSaveManager.cs)는 `SkillMap_` 키를 쓴다.
  현재 경로는 `Skill_`를 사용한다. `ResetRegistry`의 `DeleteAll`은 존재하지만 자동 호출은 주석 처리된 것으로 초기 감사에서 확인했다.
- 방향: 순수 로드아웃 모델/검증, 로컬 저장 어댑터, UI 바인딩을 분리하고
  [CommandSystem](../../Assets/QuantumUser/Simulation/LSDF_CommandSystem.cs)의 슬롯 계산과 같은 계약을 사용한다.
- 완료 조건: 저장·재실행·접속에서 같은 스킬/비용이 복원되고 잘못된 저장값이 일관되게 처리된다.
  필요한 저장 키만 관리하며 타 기능의 PlayerPrefs를 삭제하지 않는다.
- 이관 주의: SDK 경로의 사용자 코드를 옮길 때 asmdef 순환 참조, 씬/프리팹 직렬화 참조와 `.meta`를 보존한다.

### R12 — 빈 코드와 샘플 잔재 정리

- 근거: Simulation의 `LSDF_AnimatorView`, `LSDF_GameUI`, `LSDF_DashSystem`, `LSDF_TimerSystem`은 빈 MonoBehaviour다.
  GameConfig와 일부 namespace에는 Asteroids 명칭이 남아 있다.
- 방향: 실제 참조를 조사한 뒤 제거 또는 View 이관한다. 타입/namespace 변경은 직렬화 마이그레이션과 함께 한다.
  전투/플레이어/경기/데이터별 폴더는 [로드맵의 목표 구조](refactor-roadmap.md)를 참고한다.
- 완료 조건: missing script/asset 참조 없이 역할과 어셈블리가 일치한다. Generated/SDK 코어를 직접 정리 대상으로 삼지 않는다.
  Photon이 특정 사용자 하위 폴더명을 강제한다고 설명하지 않는다. 전면 인코딩 변환·포맷 변경도 섞지 않는다.

## 5. 리팩토링과 분리할 버그 후보

| ID | 근거 | 확인/수정 기준 |
|---|---|---|
| B01 | [PlayerInput](../../Assets/QuantumUser/View/LSDF_PlayerInput.cs)의 Down에도 `UpArrow` 사용 | 소스상 중복 매핑은 확인됨. Up/Down·대각 입력의 실제 Quantum 입력 결과를 확인하고 DownArrow로 수정하는 별도 버그 작업 |
| B02 | PlayerSystem의 피해 보정이 hitCount 0/2/4/>4만 처리 | 1/3에서 함수가 실제 호출되는지, 증가 시점과 목표 보정식을 확인한 뒤 수정. 아직 실전 피해 누락 재현으로 기록하지 않음 |
| B03 | IngameSystem은 HP <= 0으로 패배 판단, ResetPlayer는 HP == 0만 증가 | 다른 HP 갱신 경로와 clamp를 추적하고 음수 HP 도달 시 라운드 중복 종료 여부 확인. 재현 전 확정 버그로 단정하지 않음 |

키 매핑 수정처럼 작아 보여도 동작 변경은 기록한다. 구조 정리 PR에 미확인 밸런스 변경을 숨기지 않는다.

## 6. T01 — 멀티플레이 검증 기반 구축

현재 문서와 Python 정책 검사기는 존재하지만 **온라인 대전 자동 실행·입력·결과 수집 장치는 아직 구현하지 않았다.**
정적 검사나 Editor Play 버튼 실행만으로 온라인 멀티플레이 통과라고 보고하지 않는다.

후속 구축 범위:

1. 같은 소스/SDK/설정/asset DB의 Editor + 독립 실행 빌드 또는 두 실행 빌드로 실제 온라인 방에 접속한다.
   서로 다른 플레이어 식별과 로드아웃을 사용한다. 같은 프로젝트를 별도 Editor로 중복 실행하지 않는다.
2. 방/리전/버전 조건, 참가 성공, PlayerRef/엔티티 매핑을 기록한다. 인증 정보는 증거에 남기지 않는다.
3. 공격·가드 등 관련 시나리오의 입력을 두 클라이언트에 전달하고 같은 verified tick의
   체크섬·HP·위치·라운드 상태를 수집한다. 입력 재생/수집 API는 설치 SDK를 확인해 구현한다.
4. 변경에 필요한 경우 지연/롤백 또는 재접속을 포함하고 실제 발생 여부를 로그로 확인한다.
5. 결과를 PASS/FAIL/NOT RUN으로 출력하고 빌드 식별자·시나리오·로그 위치를 남긴다.

같은 PC의 두 클라이언트는 첫 통합 검증이지 교차 플랫폼 결정론 보증이 아니다.
구조 변경 전후의 raw 체크섬은 직렬화 레이아웃 차이로 달라질 수 있다. 체크섬은 같은 버전의 실행끼리 비교하고,
리팩토링 전후의 동작 보존은 타격 틱·HP·타격 횟수 등 의미 있는 결과로 비교한다.

## 7. 각 작업의 완료 기준과 증거

- 현재 소스 확인 → 범위·보존 동작 결정 → 구현/필요한 CodeGen → **최종 검증 한 묶음, 한 차례** → 결과 기록.
- 게임 로직 변경에는 컴파일, 관련 온라인 2인 시나리오, 필요한 결정론/롤백 검사를 포함한다.
  두 클라이언트와 여러 관측 항목은 한 검증 묶음의 내부 절차이지 검증 반복이 아니다.
- 문서만 바꿀 때는 문서 링크/형식/변경 범위만 검사한다. 온라인 대전은 불필요하다.
- 실패 후 자동 재검증하지 않는다. 수정했지만 재실행하지 않았으면 `수정 후 미검증`으로 보고한다.
  실행 장치나 환경이 없으면 해당 항목을 NOT RUN으로 남기며 결정론이 검증되었다고 하지 않는다.
- 기존 사용자 변경을 보존하고 `.meta`, asset ID, Animator 계약, 시스템 실행 순서를 유지한다.
- 포트폴리오 증거: 문제 소스 → 설계 선택/대안 → AI 변경의 사람 검토 → 실제 두 클라이언트 결과/로그 → 남은 한계.
  성능 향상·버그 해결·네트워크 안정성은 실제 관측 없이 수치나 성공 사례로 쓰지 않는다.

실행 절차는 [validation.md](validation.md), 개발 운영은 [development-workflow.md](development-workflow.md),
증거 작성은 [evidence-template.md](evidence-template.md)를 따른다. 이 문서는 작업 선택용이며 전체 구현에 대한 포괄적 승인으로 사용하지 않는다.
