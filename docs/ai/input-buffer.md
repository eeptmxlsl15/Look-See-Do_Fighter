# 선입력 시스템

기준: `3f45e7d1` 이후 기능 추가. Quantum 3의 입력/Frame 상태/Animator 경계를 유지한다.

## 유지 프레임 변경

Unity Project에서 `Assets/QuantumUser/Resources/LSDF_GameConfig.asset`을 선택하고 Inspector의
**Input Buffer Frames**를 수정한다. 초기값은 **10**, 단위는 렌더 프레임이 아닌 시뮬레이션 tick(현재 60Hz)이다.
`0`이면 잠금 중 선입력을 유지하지 않으며, 입력 가능한 현재 tick의 새 공격은 그대로 실행한다. 음수는 0으로 처리한다.
게임을 시작하기 전에 설정하고 양쪽 클라이언트에 동일한 코드/설정 에셋을 사용한다. 실행 중 원본 Quantum 에셋을 변경하지 않는다.

## 동작 계약

- 새로 누른 공격 버튼과 **그 시점의 방향**을 함께 저장한다. 키를 놓거나 방향을 바꿔도 저장한 커맨드는 유지된다.
- 한 플레이어당 다음 커맨드 **1개**를 보관한다. 새 명령이 들어오면 이전 대기 명령을 교체한다. 매크로/FIFO 연속 재생은 아니다.
- 동시 새 입력의 우선순위는 기존과 같이 LP → RP → LK → RK다. 이미 누르고 있던 버튼보다 새로 누른 버튼이 우선한다.
- 같은 버튼을 계속 누르는 것만으로 타임스탬프를 갱신하거나 공격을 반복하지 않는다. 다시 예약하려면 놓았다가 다시 누른다.
- `현재 tick - 입력 tick <= N`이면 유효하다. N=10에서 10tick 전 입력까지 인정하고 11tick 전 입력은 버린다.
- 공격/피격/가드/스턴/콤보/공중/벽/입력 잠금 중에도 입력은 수집한다. 행동 잠금이 풀리고 실제 Animator 입력 진입점이 열리는 **첫 CommandSystem tick**에 유효 명령을 전달한다.
- N은 대기 시간이 아니다. 예를 들어 T=100에 저장하고 T=107에 행동이 끝나면 110까지 기다리지 않고 107에 전달한다.
- 기존 시스템 순서는 변경하지 않는다. Command → 다음 Animator 평가로 넘어가는 기존 그래프 전달 시간은 그대로이며, 버퍼 자체의 추가 지연은 없다.
- 이동/앉기/패링 등의 방향 단독 입력은 기존 지속 입력 경로를 유지한다. 이번 버퍼는 방향을 포함한 **공격 커맨드 선입력**이다.
- KO/라운드 회복 잠금에서는 대기/전달 중 명령을 비운다. 잠금 동안 키를 계속 누른 채 다음 라운드가 시작돼도 새 공격을 자동 등록하지 않는다.
- 기존 잽 계열의 `BufferedNextAttack`/`NextAttack` 연계 규칙은 별개로 유지한다. 기술의 캔슬 창이나 피해 수치를 바꾸지 않는다.

## Quantum 구조와 파일

| 파일 | 역할 |
|---|---|
| `Simulation/LSDF_CommandBuffer.qtn` | 엔티티별 대기 명령, 눌림 이력, 전달 중 명령을 Frame에 저장. 롤백/직렬화 대상 |
| `Simulation/LSDFInputBuffer.cs` | 순수 정수 tick 기반 캡처·만료·소비/전달 정책 |
| `Simulation/LSDFCommandProcessor.cs` | 실제 입력/설정을 읽고 기존 Animator 변수·트리거로 전달 |
| `Simulation/LSDF_CommandSystem.cs` | 기존 시스템/필터/순서를 보존한 진입점 |
| `Simulation/LSDF_GameConfig.cs`, `LSDFBalance.cs` | 설정 에셋과 실제 시뮬레이션 소비 지점 |
| `Resources/LSDF_GameConfig.asset` | `InputBufferFrames: 10` 기본값 |
| `Tests/Editor/LSDFInputBufferTests.cs` | 경계, 키 해제/홀드, 덮어쓰기, 잠금, 격리, 그래프 전달 단위 검사 |
| `Tests/Editor/LSDFOnlineSmokeTests.cs` | 실제 Photon 2클라이언트, 선입력 실행 관측, 기록 재생/대기 버퍼 Frame 복원 검사 |

위 Simulation/Resources 경로의 루트는 `Assets/QuantumUser`다. 테스트 루트는 `Assets`다.
컴포넌트는 기존 Player/Animator 필터의 첫 Command tick에 지연 생성되므로 기존 프로토타입을 재저장할 필요가 없다.
공유 에셋/static/시스템 필드에 플레이어 입력 기록을 두지 않는다.

현재 Animator는 Idle → Command Map → 버튼 → 방향 → 공격을 여러 tick에 걸쳐 통과한다.
한 tick짜리 입력이 중간에 사라지지 않도록 **이미 승인된 명령만** 공격 진입까지 별도로 유지한다.
`MaximumRoutingFrames=16`은 잘못된 그래프에서 트리거가 영구 유지되지 않게 하는 안전 한계이며 선입력 N과 다르다.
새 대기 입력이 전달 중 명령을 덮어쓰지 않으며, 피격 등으로 전달이 중단되면 그 전달은 취소한다.

새 QTN은 설치 SDK의 `QuantumCodeGenQtn`으로 생성한다. Generated를 수작업으로 편집하지 않는다.
Frame 스키마/입력 의미가 달라졌으므로 방 버전 접미사를 `lsdf-loadout-v3-input-buffer`로 올렸다.
이전 버전 Frame/리플레이의 바이너리 호환성을 주장하지 않는다.

## 검증 계획과 결과

구현 완료 후 CodeGen/Unity 컴파일, `LSDF.Tests` 전체(온라인 검사 포함), Quantum 정적 정책과 diff를 한 묶음으로 한 번 실행한다.
실패하면 자동 재실행하지 않고 원문/수정 후 미검증 상태를 기록한다. 이번 요청은 기능 구현이며 자동 commit/push는 포함하지 않는다.

온라인 검사에는 A의 한 tick 잽 입력 → 행동 종료 직전 down-forward LP 한 tick → 키 해제 →
첫 입력 가능 tick에 커맨드 전달 → 양쪽 클라이언트에서 LeftUpper 히트박스 확인 절차를 추가했다.
두 재생은 **명령이 대기 중인 Frame**을 저장했다가 SDK `ResetReplay`로 되돌린 후 동일 입력의 verified checksum을 비교한다.
이는 네트워크 jitter 강제 주입/실제 재접속/교차 플랫폼/독립 빌드 검사를 대신하지 않는다.

### 이번 최종 검증 결과

| 검사 | 결과 |
|---|---|
| SDK CodeGen | 생성 완료. 생성 준비 중 DSL 예약어 `Button` 충돌을 확인해 `PendingButton`으로 수정한 뒤 SDK로 17개 QTN을 생성했다 |
| Quantum 정적 정책 | PASS: Simulation 91개 파일, 기존 0·신규 0. 의미적 결정론의 증명은 아님 |
| Unity 최종 컴파일 | FAIL: `LSDFCommandProcessor.cs`에서 `AnimatorState`의 네임스페이스 참조 누락(CS0246) |
| 컴파일 오류 수정 | `using Quantum.Addons.Animator;` 추가. **수정 후 미검증**, 재컴파일/콘솔 재조회하지 않음 |
| 신규 단위 검사·전체 회귀·온라인·재생/복원 | NOT RUN: 최종 컴파일 실패 뒤 자동 재검증하지 않음. 테스트 소스 추가를 실행 성공으로 간주하지 않음 |
| diff | SDK 생성 코드 `Quantum.CodeGen.Core.cs`의 `unchecked { ` 줄 끝 공백 1건 발견. 생성 파일 수작업 편집 금지에 따라 그대로 보존 |
| commit/push | 실행하지 않음 |

[오류 원문 및 검증 기록](evidence/input-buffer-validation.json)을 보존했다.
위 기록은 최초 구현 검증의 이력이다.

### 2026-10-16 커밋 요청 후 검증

사용자가 정상 동작을 확인하고 커밋을 요청하여 별도 검증을 한 번 실행했다.
컴파일 오류 없음(MCP WebSocket 경고 1건), 정적 정책 PASS(91개, 신규 0), 전체 137개 중 136 PASS / 1 FAIL / 0 SKIP.
실제 Photon 2클라이언트 및 기록 재생/Frame 복원 검사는 PASS. 선입력 tick 2052, 전달 tick 2060을 양쪽에서 관측했다.

실패: `DefaultWindowIsEditableAndWiredToTheExistingAsset`가 변경 가능한 에셋의 값까지 10으로 고정했다.
현재 사용자가 조정한 에셋 값 **20**은 보존한다. 새 설정 객체의 초기값 10과 실제 에셋의 존재/비음수 값을
구분하도록 테스트를 수정했다. **수정 후 미검증**이며 자동 재실행/커밋/Push는 하지 않았다.
[이번 커밋 검증 기록](evidence/input-buffer-commit-validation.json). 이후 HP 0 종료 기능은 별도 작업이며 이 실패를 PASS로 바꾸지 않는다.

후속 5판 3선승 작업의 전체 회귀 검사에서 수정한 설정 테스트와 InputBuffer 31개는 모두 통과했다.
최초 실패 이력은 보존한다. 그 후속 온라인 검사 전체는 시간 제한으로 실패하여 커밋/Push를 실행하지 않았다.
[후속 검증 결과](best-of-five.md).

그 뒤 새 Push 요청의 게시 검증은 전체 157개(온라인/재생/복원 포함)가 모두 통과했다.
[게시 검증 증거](evidence/fighter-publish-validation.json)에 현재 후보 범위와 실제 결과를 별도로 기록했다.
