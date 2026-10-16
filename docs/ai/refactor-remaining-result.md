# 잔여 리팩토링 통합 기록

작업일: 2026-10-16. 기준 커밋 `ffcc356698ded5af353a17609aefaf5bb615c1bd`.
Unity 6000.3.26f1 / Photon Quantum 3.0.14.2241. 기존 저장소와 `dev/Prototype`을 유지한다.

## 범위와 현재 상태

R01–R12의 잔여 구조 개선을 구현했다. 최초 전체 검사는 104/106 PASS였고, 기준 데이터 타입명 오류 2건을 수정했다.
**후속 사용자 요청으로 실패했던 2개만 한 번 재실행해 2/2 PASS를 확인했다. 기존 통과 104개·컴파일·Quantum 검사는 반복하지 않았다. 로컬 커밋을 위한 후보 검사를 완료했으며, 이번 후속 요청에는 Push가 없다.**
이 문서는 게임 전체의 기능 완성이나 모든 공격 조합의 온라인 검증을 주장하지 않는다.
이전 1–5차 결과와 초기 감사는 해당 시점의 증거로 유지한다.

| 백로그 | 이번 구현 및 실제 경로 |
|---|---|
| R01·R11 로드아웃 | `LSDFLoadout`의 슬롯/ID/비용 정책을 메뉴 모델, 로컬 저장 어댑터, RuntimePlayer 수신 검증에서 공유. 현재 `Skill_` 우선, 없으면 `SkillMap_` 호환. 초기화는 로드아웃 키만 저장하며 DeleteAll하지 않음 |
| R02·R03 결정론 | 기존 FP/정수 틱·Frame 연타 버퍼 유지. 새 시스템 필드에 게임 상태를 두지 않음. 기존 5건의 미사용/계층 위반 기준선 제거, 허용 항목 추가 없음 |
| R04 공격 | 남은 30종을 직렬화 Hit 정의와 stateless 생성기로 이관하여 전체 37종 연결. 이벤트 타입명·GUID·asset ID·Animator 계약 유지. 런처/벽/앉기 예외·확정타 필드 포함 |
| R05 책임 | Update 진입점, `LSDFPlayerMotion`, `.Combat.cs`, `.Contacts.cs`, `LSDFPlayerHealth`로 이동·피격·벽/바닥·체력 규칙 분리. 시스템 등록/순서 불변 |
| R06 설정 | 기존 `LSDF_GameConfig.asset`에 HP 170, 회복 2, 잠금 210틱, 회복 시작 잔여 150틱, 보행 0.5, 대시 2, 스폰 간격 0.5, 비용 한도 140을 연결. 스키마 7×4와 알고리즘 상수는 분리 |
| R07 라운드·참가자 | `LSDFRoster`가 PlayerRef 0/1만 참가자로 취급. 메뉴/연결 최대 인원 2. `LSDFRoundPolicy`가 기존 Frame 필드로 대기/전투/회복 단계를 도출. 회복 중 중복 KO 처리를 차단 |
| R08 충돌 | `LSDFContactPolicy`에 회피→벽→카운터→스턴→콤보→가드→패링 우선순위 분리. 누락 컴포넌트·그래프·레이어·입력은 역참조하지 않고 무시 |
| R09 View | 새 `LSDF.View` 어셈블리. 실제 EntityViewUpdater별 `LSDFSceneBindings`에 카메라/HUD/엔티티 등록. Default Runner·전역 static 참조·런타임 이름 검색 제거. 비활성화/게임 변경/재활성화 정리 |
| R10 표현 | 게임별 단일 VFX/SFX 담당, 수동 구독 토큰 해제. 예측 이벤트 키를 추적하고 취소 시 제거/중지, 확정 시 추적만 해제. 오디오 풀 재사용 시 이전 취소 키 제거 |
| R12 계층 | 빈 MonoBehaviour 4종은 GUID와 타입명을 보존해 `View/Legacy`로 이관. 오디오 2종도 View로 이동. 이들 어셈블리는 LSDF.View로 변경되며 SDK/Generated는 편집하지 않음 |
| T01 자동화 확장 | 실제 Photon 2클라이언트 검사에 입력 기록, 두 재생의 verified checksum 비교, SDK ResetReplay를 통한 과거 Frame 복원/재시뮬레이션 비교 추가 |

## 보존한 예외와 의도적으로 바꾼 동작

- 공격 생성은 `StartupFrames - 1`, 파괴 예약은 생성 tick+1이다. Physics/Animator/명령/파괴 시스템 순서를 바꾸지 않았다.
- `MachineGun`의 기존 중복 collider Add 호출은 이번 구조 이관에서 임의 수정하지 않았다.
- `LarsSnake`와 기존 `LarsCutKick` 첫 타의 효과 좌표 원점은 기존 동작대로 보존했다. 시각 버그 수정은 별도 판단이 필요하다.
- B01: DownArrow를 눌렀을 때 Down만 입력되도록 기존 UpArrow 중복 매핑을 수정했다.
- B03: HP 0 이하를 같은 KO로 처리하고 회복 잠금 중 패배 횟수를 반복 증가시키지 않는다. 동시 KO는 양쪽 패배 횟수를 각각 한 번 증가시킨다.
- B02: 기존 1·3 hitCount의 일반 피해 0 분기는 밸런스 의도가 확인되지 않아 유지했다. 구조화와 피해 공식을 몰래 섞지 않는다.
- 메뉴에는 같은 슬롯/ID에 다른 비용을 가진 비활성 placeholder 버튼이 있다. 기존 **저장 복원 시 첫 일치 항목**의 비용을 단일 카탈로그로 채택했다. 기본 28칸 비용 47, Back LP ID2 선택 시 51이다. 버튼 내용/스킬을 새로 창작하지 않았다.
- 방 버전 접미사는 `lsdf-loadout-v2`로 올려 이전 규칙 클라이언트와 같은 방에 섞이지 않게 한다. 기존 리플레이와 새 버전의 raw checksum 동등성을 주장하지 않는다.
- 기존 GameConfig의 미사용 `AsteroidInitialSpeed`와 직렬화된 타입/namespace는 호환성 때문에 유지한다.
- Gameplay 씬에는 원래 SfxManager가 없었다. 메뉴 씬의 기존 AudioSource prefab/5종 사운드 설정을 Gameplay updater의 SfxManager로 복사하고 부모와 updater를 Gameplay 내부로 연결했다. 메뉴의 원본 설정은 삭제하지 않았다.
- Unity에서 씬 저장 시 현재 버전의 Renderer/Light/TMP/ShapeSettings 기본 직렬화 필드도 갱신됐다. 기존 GUID/객체 참조와 시스템/Map 데이터는 보존했다.

## 폴더와 수정 위치

```text
Assets/QuantumUser/
  Simulation/
    LSDFBalance.cs / LSDF_GameConfig.cs    # 읽기 전용 밸런스 조회/에셋 타입
    LSDFLoadout.cs / LSDFRoster.cs        # 28칸 계약과 1v1 참가자
    LSDFRoundPolicy.cs                   # Frame 상태 기반 라운드 정책
    LSDFContactPolicy.cs                 # 순수 충돌 분기
    LSDFPlayerMotion.cs                  # 이동·대시·콜라이더
    LSDF_PlayerSystem{,.Combat,.Contacts}.cs
    LSDFRemainingAttackDefaults.cs       # 신규/미직렬화 공격의 기본값
    LSDF_Animator_Window_Event/Attack/    # 기존 타입/GUID를 지닌 어댑터
    Generated/                          # 변경 없음, CodeGen 전용
  Resources/
    LSDF_GameConfig.asset                # 실제 밸런스·비용 84칸
    LSDF_Animator Assets/                # 실제 37종 공격 정의
  View/
    LSDF.View.asmdef                     # Simulation/Quantum.Unity/UI/TMP 참조, 역참조 없음
    LSDFSceneBindings.cs                 # updater 1개에 카메라와 양쪽 HUD
    LSDFPresentationLedger.cs            # 예측/취소/확정 추적
    LSDF_*View.cs / LSDF_IngameUI.cs / LSDF_PlayerInput.cs
    SfxManager.cs / LSDF_AudioSource.cs
    Legacy/                             # GUID 보존한 빈 호환 컴포넌트 4개
    Generated/                          # 기존 Quantum.Unity asmref 유지
Assets/Photon/QuantumMenu/
  Runtime/LSDFLoadoutSelection.cs        # 순수 메뉴 모델
  Runtime/LSDFLocalLoadout.cs            # Unity PlayerPrefs 저장 경계
  UnityUI/Scripts/                      # 자기 리스너만 등록/해제하는 표현
Assets/Scenes/LSDFGameplay.unity         # 명시적 씬 바인딩 저장 대상
Assets/Tests/Editor/
  LSDFRemainingAttackTests.cs            # 원본 수치 30종 계약 비교
  LSDFStructuralTests.cs                 # 비용·입력·KO·충돌·표현·씬 연결
  LSDFOnlineSmokeTests.cs                # 온라인+기록 재생+Frame 복원
docs/ai/evidence/remaining-attack-contracts.json
tools/ai/refactor_remaining_*.py          # 작업 당시 원본용 일회성 이관 기록
tools/ai/finalize_remaining_layout.py    # 작업 당시 원본용 일회성 파일 정리
```

폴더명 자체는 Quantum의 필수 규칙이 아니다. 어셈블리·FP 수학·Frame 상태 소유권을 기준으로 구성했다.
이관 스크립트는 이미 이관된 프로젝트에 재실행하는 빌드 명령이 아니며 일부는 원본 존재를 assert한다.
기존 소스의 CP949 바이트를 기계적 변경에서 보존했으며 전체 인코딩을 바꾸지 않았다.

## 최초 최종 검증 — 한 묶음, 자동 재실행 없음

| 검사 | 상태 | 근거 |
|---|---|---|
| Unity 컴파일·콘솔 | PASS / 경고 8건 | 컴파일 완료 후 콘솔 1회 조회: 오류 0. SDK/기존 에셋/AvailableScenes obsolete 및 MCP transport 경고. 제외 폴더의 파일 내용은 조사하지 않음 |
| LSDF.Tests 전체 | FAIL: 104/106 PASS, 2 FAIL, 0 SKIP | job `aa91be00a0b94e6c807fabe22e4da38f`, EditMode 1회. 실패 후 자동 재실행 없음 |
| 온라인 2클라이언트·재생·Frame 복원 | PASS | 위 job의 실제 UnityTest 성공. 서로 다른 payload, 피해·잽/어퍼/나락/Lars 타격·중복 AddPlayer 및 checksum, 두 재생과 과거 Frame 복원 후 같은 입력 재시뮬레이션 assertion 통과 |
| 신규 구조 검사 | PASS | 슬롯/비용/기본 에셋/KO/우선순위/가드/화살표/표현 ledger/Gameplay 씬 참조 검사 통과 |
| 신규 공격 계약 | 28 PASS, 2 FAIL → 수정 후 미검증 | 아래 원인과 수정 참조 |
| Quantum 정적 정책 | PASS | 89개 파일, 기존 0·신규 0. 검사는 의미적 결정론 증명이 아님 |
| 게시 후보/비밀정보 | NOT RUN | 필수 테스트 실패로 staging·commit·publish preflight·push에 진행하지 않음 |
| diff | 공백 4건 발견 → 수정 후 미검증 | Unity가 새로 직렬화한 빈 YAML 필드의 줄 끝 공백만 제거. diff 검사도 재실행하지 않음 |

[검사 결과와 실패 원문](evidence/refactor-remaining-validation.json)에 전체 테스트 이름/상태 및 실패 메시지를 기록했다.
온라인 통과 결과가 전체 테스트 실패를 대신하지 않는다. 사용자 방침과 `publish-unity-pr` Skill의 실패 시 게시 중단 규칙을 적용했다.

### 실패 원인과 수정 — 재실행하지 않음

`LSDFRemainingAttackTests`의 타입명 비교에서 두 건이 실패했다. 원본 Git 소스의 선언을 다시 읽어 다음을 확인했다.

| 파일명 | 기존 실제 클래스명 | 수정 |
|---|---|---|
| SitPunchWindowEvent.cs | SitPunchEvent | JSON 기준 데이터의 type만 실제 선언으로 수정 |
| SuperFlyingKickWindowEvent.cs | SuperFlyKickWindowEvent | JSON 기준 데이터의 type만 실제 선언으로 수정 |

마이그레이션 기록 스크립트도 파일명 대신 실제 클래스 선언을 추출하도록 수정했다.
기존 게임 클래스명·스크립트 GUID·asset ID는 바꾸지 않았고 테스트 assertion도 제거하지 않았다.
두 테스트는 타입명 비교에서 멈췄으므로 뒤의 피해/크기/틱 assertion까지 통과했다고 주장하지 않는다.
최초 보고 때는 **수정 후 미검증**으로 중단했다. 그 뒤 사용자가 명시적으로 2개 재실행과 성공 시 커밋을 요청했으며 아래에 별도 기록한다.

### 사용자 요청에 따른 실패 2개 재검증

- job: `b5ad49c80a6244fe875da459b08d12f0` / EditMode / 정확히 2개 실행 / 2 PASS, 0 FAIL, 0 SKIP.
- `LSDFRemainingAttackTests.LegacyContract_SitPunchEvent`: PASS.
- `LSDFRemainingAttackTests.LegacyContract_SuperFlyKickWindowEvent`: PASS.
- 타입명 비교 뒤의 피해·경직·히트박스 크기·FP 반전·발생 틱 경계 assertion도 모두 실행됐다. assertion 제거나 테스트 제외로 통과시키지 않았다.
- [재검증 원본 결과](evidence/refactor-remaining-retry.json). 최초 실패 기록은 위 JSON과 표에 그대로 보존한다.
- 이전 104개 통과 결과와 합쳐 106개 사례가 확인됐지만, **전체 106개를 다시 실행한 결과는 아니다**.
- 이번 요청은 로컬 커밋까지다. 비밀정보/후보 파일 검사를 거쳐 `dev/Prototype`에 커밋하고, Push/PR은 하지 않는다.

### 로컬 커밋 전 후보 검토

- `publish-unity-pr` preflight: `NO_KNOWN_FINDINGS`, 검사한 staged blob 158개, 미검사/탐지 0건. 기준 `origin/dev/Prototype`은 최초 검증의 기준 커밋과 동일하다.
- IDE 생성물은 제외했다. 기존 remote·이력·Generated·QTN·시스템 등록 순서는 바꾸지 않았다. 삭제 경로는 GUID 보존 파일 이동 또는 새 View 경로의 동일 타입 대체와 대응한다.
- staged diff 검사에서 Unity가 자동 확장한 신규 asmdef meta의 빈 필드 줄 끝 공백 3건을 발견했다. 해당 세 공백만 제거하고 GUID·Importer와 빈 값이 그대로임을 패치로 검토했다. 검사 명령/게임 테스트는 반복하지 않았다.
- 추가된 결과 문서와 JSON은 실행 소스가 아니며 별도로 내용을 검토했다. preflight의 기존 158 blob 검사에 이후 생성된 증거 파일까지 검사했다고 주장하지 않는다.
- [커밋 후보 검사 기록](evidence/refactor-remaining-commit-checks.json). 로컬 커밋 메시지: `Finish Quantum fighter refactoring and presentation boundaries`.

## 검증 한계와 기능 개발의 다음 범위

- 기록 재생과 Frame 복원 검사는 **네트워크 jitter를 강제로 주입한 예측 rollback 검사와 다르다**.
- 같은 Editor 프로세스의 실제 Photon 2클라이언트 검사다. 독립 실행 빌드/교차 플랫폼, 실제 연결 끊기 후 재접속, 사람이 보는 양쪽 HUD/VFX/SFX는 별도 실행 검증이 필요하다.
- 30종 신규 계약 테스트는 에셋 수치/반전/타격 경계 검증이지 모든 30종의 온라인 전투 조합 재현이 아니다.
- 제한시간 종료, 3승 경기 종료, 재대전, 메뉴 placeholder 콘텐츠 완성은 별도 기능 개발이다. 리팩토링 완료를 위해 임의 규칙을 만들지 않는다.
- 제외 에셋 폴더와 IDE 생성 파일은 작업/게시 범위에 넣지 않는다. 새 저장소, main push, force push, PR 자동 생성은 하지 않는다.
