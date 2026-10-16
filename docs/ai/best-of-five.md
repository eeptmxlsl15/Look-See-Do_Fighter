# 5판 3선승과 라운드 전환 — 2026-10-17

## 바뀐 동작

최초 KO 기능은 모든 KO를 영구 종료로 취급하여 첫 라운드의 YOU WIN에서 멈췄다.
현재는 **먼저 3승하면 매치 종료**, 그 전에는 다음 라운드를 시작한다. 최대 5개의 승패가 결정되는 라운드를 진행한다.
동시 KO는 양쪽 무득점이며 같은 번호의 라운드를 재시작한다. 무승부는 5개의 결정 라운드에 포함하지 않는다.

```text
전투 → KO/승점 1회 확정 → ROUND WIN/LOSE (기본 120tick)
     → HP·위치·전투 상태 초기화 → Round N / FIGHT (150tick) → 전투
     └ 누군가 3승이면 YOU WIN/LOSE 유지 (다음 라운드 없음)
```

첫 라운드 시작 방식은 기존대로 유지한다. 이후에는 두 플레이어를 동시에 최대 HP와 초기 위치로 되돌린다.
스킬맵과 점수는 유지하고 콤보/벽/공격/대시/선입력은 초기화한다. 이전 라운드에서 누르고 있던 공격을 새 입력으로 취급하지 않는다.
인트로 중 이동·공격·피격은 잠기고 마지막 60tick에 FIGHT를 표시한다. UI의 코루틴 시간이 아니라 Quantum tick이 실제 잠금 해제 시점을 결정한다.

## 자연스러운 앉기

KO 직후 패자는 기존 Quantum Animator 그래프의 **Sit Enter(SitEnter, 10프레임) → Siting**을 재생한다.
완전히 앉은 뒤에만 IgnoreTransitions를 켜 자세를 유지한다. Siting으로 곧바로 FadeTo하거나 Sit Enter를 중간에 잠그지 않는다.
승자는 Idle로 전환해 선다. 이 흐름은 일반 라운드 종료와 최종 매치 종료에 동일하게 적용한다.
기존 Animator 에셋/클립/SDK는 수정하지 않는다. 화면의 Unity Animator는 Quantum Frame의 상태를 표현할 뿐이다.

## 설정 위치

`Assets/QuantumUser/Resources/LSDF_GameConfig.asset` Inspector:

- **Round Result Ticks**: KO 결과 표시 후 다음 라운드 초기화까지. 기본 120tick(60Hz에서 2초), 최소 30tick.
- **Round Intro Ticks**: 초기화 후 Round/FIGHT 입력 잠금. 기본 150tick(2.5초), 최소 1tick.
- **Maximum Health / Spawn Offset**: 새 라운드의 HP/위치. 기존 설정 사용.

3승 조건은 `LSDFRoundPolicy.WinsRequired=3`이다. 사용자가 조정한 Input Buffer Frames=20은 보존했다.
기존 회복 관련 설정은 직렬화 호환을 위해 남겼지만 라운드 전환에 자동 회복을 사용하지 않는다.
설정은 게임 시작 전에 양쪽에 같은 에셋으로 적용한다.

## 구현 위치와 Quantum 계약

| 파일 | 역할 |
|---|---|
| `Assets/QuantumUser/Simulation/LSDFRoundPolicy.cs` | RoundEnd/Intro/Fighting/MatchEnd, 3승, 점수 1회 확정, HP/전투 상태 초기화 |
| `Assets/QuantumUser/Simulation/LSDF_IngameSystem.cs` | 기존 마지막 시스템 순서에서 전환, Teleport, 대시/입력/히트박스 정리, Sit Enter와 전환 잠금 |
| `Assets/QuantumUser/Simulation/LSDF_GameConfig.cs`, `LSDFBalance.cs` | 라운드 전환 시간 설정과 소비 |
| `Assets/QuantumUser/View/LSDF_IngameUI.cs` | VerifiedFrame 기준 ROUND/최종 결과, Round/FIGHT 표시, 기존 승점 아이콘 |
| `Assets/Tests/Editor/LSDFRoundResultTests.cs` | 전환 경계/32개 승패 입력열/무승부/3승 후 재개 금지/스킬 유지/결과 문구/그래프 전환 |
| `Assets/Tests/Editor/LSDFOnlineSmokeTests.cs` | 실제 입력의 5라운드, 4번 초기화/인트로, 3:2 최종 종료, Sit Enter 관측, 기록 재생/Frame 복원 |

지속 상태는 기존 Frame 컴포넌트의 `loseRound`, `playerHp`, `isRoundEnd`, `tickToEnableMove`와 Animator LayerData를 사용한다.
각자의 승수는 상대의 loseRound다. HP가 양쪽 모두 양수이고 isRoundEnd가 켜져 있으면 Intro,
HP 0을 포함하는 종료 상태이면 RoundEnd/MatchEnd로 구분한다. tickToEnableMove는 현재 결과/인트로의 종료 tick을 저장한다.
새 static/시스템/공유 에셋의 가변 상태나 별도 타이머를 만들지 않는다. 이 변경 자체는 QTN/CodeGen 변경이 없다.
프로토콜 접미사는 `lsdf-loadout-v5-best-of-five`로 올려 이전 규칙의 클라이언트와 방을 분리한다.

설치된 AnimatorComponent.ResetVariables는 Trigger를 활성화하므로 사용하지 않는다.
전환 시 Trigger는 ResetTrigger로 비우고 나머지 변수는 그래프 기본값으로 되돌린다. SDK 자체는 변경하지 않는다.

## 검증

구현 완료 후 Unity 컴파일, LSDF.Tests 전체(실제 Photon 2클라이언트 포함), Quantum 정적 검사와 diff를 한 번 실행한다.
온라인 검사는 번갈아 실제 중단 공격을 사용해 2:2 → 3:2를 만들고, 마지막 KO 뒤 240tick 이상 입력에도 종료가 유지되는지 확인한다.
모든 KO에서 Sit Enter가 먼저 관측되고 Siting에 도달하는지, 다음 라운드의 HP/점수/위치/입력 잠금이 올바른지 검사한다.
동일 기록을 두 번 재생하고 대기 입력이 있는 Frame을 복원해 라운드 전환/최종 종료까지 체크섬을 비교한다.
실패 시 자동 재검증하지 않고 수정 후 미검증으로 남긴다. 화면/UI 육안·독립 빌드·실제 재접속·강제 지터·교차 플랫폼은 별도 실행하지 않으면 NOT RUN이다.

이 요청에는 커밋/Push가 포함되지 않는다. 기존 미커밋 변경과 IDE 생성 파일을 보존한다.

### 이번 한 차례 결과

| 검사 | 결과 |
|---|---|
| Unity 게임 코드 컴파일 | PASS: 오류 0, 기존 SDK/obsolete/에셋/MCP 경고 8건 |
| Quantum 정적 정책 | PASS: 91개 파일, 기존 0·신규 0. 의미적 결정론의 증명은 아님 |
| 전체 LSDF.Tests | **156 PASS / 1 FAIL(timeout) / 0 SKIP**, 총 157개 |
| 라운드 정책/그래프/문구 | PASS: RoundResult 19개. 32개 승패 입력열의 3승·최대 5라운드 조건 포함 |
| 이전 선입력 설정 테스트 | 이번 전체 검사에서 PASS. InputBuffer 31개 모두 통과, 에셋 20 보존 |
| 실제 Photon 두 클라이언트 | 아래 5라운드와 4번 초기화/인트로, 5번 Sit Enter→Siting, 3:2 후 240tick 유지까지 관측된 assertion 통과 |
| 온라인 검사 전체/기록 재생·Frame 복원 | **FAIL**: 뒤이어 진행한 기록 재생 단계에서 테스트 러너의 180초 제한 초과. 매치 부분의 관측을 전체 PASS로 간주하지 않음 |
| timeout 수정 | 해당 UnityTest에 `[Timeout(360000)]` 추가. 내부 240초 취소/180초 매치 진행 제한과 게임 assertion은 보존. **수정 후 미검증**, 재컴파일/재실행하지 않음 |
| diff | 기존 SDK Generated의 줄 끝 공백 1건 보존. 생성 파일 직접 편집하지 않음 |
| UI 육안/독립 빌드/재접속/강제 네트워크 rollback/교차 플랫폼 | NOT RUN |
| 커밋/Push | 미실행 |

양쪽 클라이언트의 라운드 종료 관측은 동일했다.

| 라운드 | KO tick | PlayerRef 0/1 패배 수 |
|---|---:|---|
| 1 | 2875 | 1 / 0 |
| 2 | 4495 | 1 / 1 |
| 3 | 6115 | 2 / 1 |
| 4 | 7735 | 2 / 2 |
| 5 | 9355 | 3 / 2 |

실제 오류 원문: `Timeout value of 180000 ms was exceeded. If this is intended, increase the timeout value using a TimeoutAttribute.`
NUnit 기본 제한이 긴 시나리오보다 짧았으므로 게임 수치/조건을 낮추지 않고 검사 자체의 제한만 명시했다.
[실행 기록과 해시](evidence/best-of-five-validation.json). 검사 뒤 `LSDF_Menu`, `isDirty=false`, Play Mode 종료 상태를 확인했다.

### 후속 Push 요청의 게시 전 검증 — 2026-10-17

새 Push 요청에 따라 게시 후보 전체를 다시 한 차례 검증했다. 외부 UnityTest 제한은 360초,
실제 온라인 fixture의 내부 취소는 재생/복원 시간을 확보하도록 240초에서 300초로 조정했다.
게임 수치·판정·assertion·180초 매치 진행 제한은 변경하지 않았다.

**157 PASS / 0 FAIL / 0 SKIP**, 182.09초. 컴파일 오류 0(기존 obsolete 경고 2), Quantum 정책 91개/신규 0.
양쪽의 KO tick은 2871, 4491, 6111, 7731, 9351로 일치했다. 3:2 매치 종료/4번 재시작/5번 앉기 전환/최종 240tick 유지,
기록 재생 두 번과 대기 버퍼 Frame 복원 후 재시뮬레이션까지 포함한 온라인 검사 전체가 통과했다.
이전 시간 초과 이력은 위와 원본 evidence에 보존한다. UI 육안/독립 빌드/실제 재접속/강제 지터/교차 플랫폼의 미실행 범위는 그대로다.

게시 범위는 기존 origin/dev/Prototype 이후의 미게시 리팩토링 커밋과 이번 선입력·HP·5판 3선승 변경이다.
IDE 생성 파일은 제외해 보존하고, PR은 이번 요청에 포함하지 않는다.
[게시 검증 증거](evidence/fighter-publish-validation.json).
