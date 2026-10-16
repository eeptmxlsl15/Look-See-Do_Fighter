# 초기 소스 감사 — 2026-10-16

## 환경과 조사 범위

- 프로젝트: `Look-See-Do_Fighter`, 시작 HEAD `70e19ab8`.
- Unity `6000.3.26f1`, 설치된 Quantum.Engine 파일 버전 `3.0.14.2241`.
- unityMCP 활성 씬: `LSDF_Menu`, `Assets/Photon/QuantumMenu/LSDF_Menu.unity`, 미저장 변경 없음.
- 빌드 활성 씬: 위 메뉴와 `Assets/Scenes/LSDFGameplay.unity`.
- `Assets/QuantumUser/Resources/SessionConfig.asset`: `UpdateFPS: 60`, `ChecksumInterval: 60`.
  실제 세션 실행 때 전달/오버라이드되는 설정은 별도로 확인해야 한다.
- 시작 시 `Assembly-CSharp*.csproj`, `Packages/manifest.json`, `Packages/packages-lock.json`에 기존 변경이 있었고
  루트 `AGENTS.md`는 미추적 0바이트 파일이었다. 기존 변경은 보존했다.

직접 검토: 사용자 시뮬레이션 최상위 C#/QTN, View 4개, `Assets/Scripts` 4개,
메뉴 스킬 선택 스크립트와 Legacy, 이동 이벤트 18개. 공격 이벤트 37개는 공통 단타/다단히트 구현을 읽고
전체 파일을 대상으로 코드의 중복을 제거한 고유 구문 및 필드·분기·수치 차이를 조사했다.
메뉴 연결/AddPlayer 경로, Animator 애드온 평가/트리거 처리, asmdef/asmref, 시스템/세션 설정과 일부 프리팹 참조도 확인했다.
Photon SDK 전체, 모든 Animator 그래프/씬 오브젝트, 모든 아트 에셋을 전수 검증한 것은 아니다.
`DinV`, `GabrielAguiarProductions`, `Matthew Guz` 폴더 소스는 읽지 않았다.

## 현재 구조

| 계층 | 실제 파일/위치 | 역할과 주의점 |
|---|---|---|
| 스키마 | `Assets/QuantumUser/Simulation/*.qtn` | 플레이어 상태, 28칸 커맨드 맵, 히트박스, 입력, 이벤트/시그널 |
| 시뮬레이션 | 같은 폴더의 `LSDF_*System.cs` | 이동·피격·명령·생성·라운드 |
| 전투 애니메이션 | `Simulation/LSDF_Animator_Window_Event` | 공유 AssetObject 콜백에 공격별 타이밍/수치/분기 구현 |
| Unity View | `Assets/QuantumUser/View`, `Assets/Scripts` | 입력 폴링, HP/라운드 UI, 카메라, VFX/SFX |
| 메뉴/로드아웃 | `Assets/Photon/QuantumMenu/UnityUI/Scripts` | 커스텀 UI가 SDK 경로와 섞임, PlayerPrefs 저장 |
| 연결 | `Assets/Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviourSDK.cs` | 약 340행의 `Runner.Game.AddPlayer` |
| 시스템 구성 | `Assets/Resources/LSDF_SystemConfig.asset` | 게임에서 사용하는 SystemsConfig, ID `536336001698627310` |
| 설정 에셋 | `Assets/Resources/LSDF_SimulationConfig.asset`, `Assets/QuantumUser/Resources` | Simulation/Game/Menu/Session 설정 |
| 생성 코드 | `Simulation/Generated`, `View/Generated` | DSL/CodeGen 산출물 |

현재 데이터 흐름: 메뉴 선택 → 로컬 `PlayerPrefs` → `OnPlayerAdded`에서 직접 읽기 → `CommandSkillMap` →
`LSDF_CommandSystem` → Animator 변수/상태 → 공격 이벤트 → Quantum 히트박스 → 충돌 시그널 →
`LSDF_PlayerSystem`의 피해/상태 처리 → Quantum 이벤트/Frame → Unity 표현.

시뮬레이션의 asmref는 실제 `Quantum.Simulation` GUID로 연결된다. 하지만 SDK asmdef의
`noEngineReferences`가 false이므로 Unity API가 컴파일된다는 사실만으로 올바른 계층 분리라 판단할 수 없다.

## 확인된 우선 문제

| 우선순위 | 근거 | 영향과 다음 조치 |
|---|---|---|
| P0 | `LSDF_PlayerSpawnSystem.cs:39`의 `PlayerPrefs.GetInt` | 클라이언트 로컬 설정이 동일 Frame을 다르게 만들 수 있음. RuntimePlayer 전달 계약으로 이동 |
| P0 | 공격 37개와 `Move/AirWindowEvent.cs`의 `Time.AsFloat * 60.0f` | 판정에 부동소수 사용. FP/정수 계산 및 경계 프레임 검증 |
| P0 | `JapWindowEvent.cs`, `JapJapWindowEvent.cs`의 `bufferedNextAttack` 등 | 공유 에셋의 변경 가능한 상태는 엔티티 격리/롤백 복원 대상이 아님. 컴포넌트로 이동 |
| P0 | `LSDF_PlayerSystem.cs`의 카운터 피해 `attackDamage * 1.2f` | 피해 계산을 FP/정수로 바꾸되 버림 규칙을 테스트 |
| P1 | `LSDF_CollisionSystem.cs`의 `static public bool isGuard` | 호출 지역 결과를 static 필드에 저장. 현재 지속 상태로 쓰는지 구분 후 지역화 |
| P1 | `LSDF_IngameSystem.cs`의 `lastTimeTickFrame` | 필드 존재, 현재 타이머 로직은 주석 처리. 재활성화 시 Frame 상태로 옮겨야 함 |
| P1 | `LSDF_AnimatorView`, `LSDF_GameUI`, `LSDF_DashSystem`, `LSDF_TimerSystem` | 시뮬레이션 폴더 안의 빈 MonoBehaviour. 참조 조사 후 View 이관/제거 결정 |
| P1 | `LSDF_PlayerInput.cs:21` | Down 입력에도 `UpArrow`가 매핑되어 Up/Down 동시 입력 가능. 개별 버그 수정으로 검증 |
| P1 | `LSDF_PlayerSystem`의 피해 보정 분기 | hitCount 0/2/4/>4만 직접 처리. 1/3의 의미와 실제 이벤트 증가 타이밍을 재현해야 함 |
| P1 | `LSDF_CollisionSystem`, 각 이벤트의 TryGetPointer | 성공 여부 확인 없이 포인터 사용되는 경로. 필터/프로토타입 보장과 런타임 가드를 검토 |
| P1 | `LSDF_IngameSystem` | 라운드 타이머 주석, 60초/3승 상수 미사용. 기능 구현과 동작 보존 리팩토링을 분리 |
| P2 | View/UI의 `GameObject.Find`, `QuantumRunner.Default`, static 참조 | 멀티러너, 재접속, 재활성화에서 수명/소유권 검증 필요 |
| P2 | `LSDF_HitEffectView`의 전체 이벤트 구독 | 복수 인스턴스 구독 시 중복 VFX 가능성. 실제 씬 배치와 취소/재생 정책 확인 |
| P2 | 메뉴 `SkillSelectionManager`와 Legacy | `Skill_`/`SkillMap_` 키가 다름. `ResetRegistry`에 `DeleteAll`; 현재 자동 호출은 주석 처리 |
| P2 | 메뉴 설정 `_maxPlayers: 6`, 전투의 PlayerRef 0/그 외 분기 | 1v1 가정과 메뉴 허용값 불일치 후보. 실제 연결값 확인 후 수정 |

위 항목은 소스 근거에 따른 위험/결함 후보다. 재현하지 않은 네트워크 desync나 크래시를 이미 관측했다고 주장하지 않는다.

## 프레임 순서가 중요한 이유

`LSDF_SystemConfig.asset` 순서:

```text
Culling3D → Physics3D → Culling2D → Physics2D
→ EntityPrototype → PlayerConnected → PlayerSpawn → Player
→ Animator → AnimatorBehaviour → AnimatorTriggers
→ Command → CollisionDestroy → Collision(signals) → Ingame
```

신호 전용 시스템의 메서드는 시그널을 발생시키는 시점에 호출된다. 위 배열의 위치를 독립적인 충돌 Update 시점으로 해석하지 않는다.
현재 Physics2D가 Animator보다 앞서고, 공격 히트박스는 `f.Number + 1`에 제거된다.
Command는 AnimatorTriggers 뒤에 있으므로 입력 소비/트리거 초기화 순서 역시 계약의 일부다.
`HitFrame - 1`을 보기 좋게 고치는 작업이나 시스템 정렬만으로도 체감 타이밍이 달라질 수 있다.
설치된 `AnimatorTimeWindowEvent.Evaluate`는 유효 시간창에서 `Execute` 후 조건부 `OnEnter`를 호출한다.

## 하드코딩 분류

- 밸런스 에셋 후보: HP 170, 210틱 잠금, 회복 2/tick, walk 0.5, dash 2, spawn ±0.5,
  hit/guard/counter 지연, 데미지, pushback, hitbox offset/extents, 60초 라운드/3승.
- 스키마 계약: 방향 7 × 버튼 4 = 28. 동적 배열로 무작정 바꾸지 말고 검증된 인덱스 계약을 공유한다.
- Animator 계약: `InputCommand`, `FinalNum`, `NextAttack`, `Stun`, `Parring` 등은 그래프와 동시 이관한다.
- 표현 설정: 카메라 줌, HP 표시, 효과 크기/수명은 View 설정으로 둔다. 시뮬레이션 설정과 용도를 구분한다.
- `LSDF_GameConfig`는 현재 Asteroids 잔재 `AsteroidInitialSpeed` 중심이므로 전투 설정이 실제로 소비되는지 확인하며 확장한다.

## 검증 상태

MCP 콘솔 조회에는 View의 obsolete API 경고와 MCP 로컬 HTTP 서버 시작 실패 메시지가 있었다.
제외 폴더의 경고도 콘솔에 노출됐지만 해당 소스는 열지 않았다. 기존 로그를 지우지 않았다.
이 세팅 작업은 게임 소스/씬/프리팹/설정 에셋을 변경하지 않으며 PlayMode, 빌드, 온라인 대전, 체크섬 재생은 실행하지 않았다.
파일 검색에서 자체 NUnit/EditMode/PlayMode 테스트 소스는 찾지 못했다. `LSDF_TestEnemySystem`은 테스트 스위트가 아니다.

## 기준 문서

설치 SDK 소스와 아래 Quantum 3 문서를 함께 사용한다. 최신 웹 문서의 API를 설치 버전에 그대로 가정하지 않는다.

- [Photon: 프로젝트 구조](https://doc.photonengine.com/quantum/v3/manual/quantum-project)
- [Photon: 시스템과 Frame 상태](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/systems)
- [Photon: 불변 데이터 에셋](https://doc.photonengine.com/quantum/v3/manual/assets/assets-simulation)
- [Photon: Fixed Point](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/fixed-point)
- [Photon: RuntimePlayer/AddPlayer](https://doc.photonengine.com/quantum/v3/manual/player/player)
