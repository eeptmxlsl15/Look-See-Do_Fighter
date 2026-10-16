# 1차 오류 수정 및 2차 리팩토링 — 2026-10-16

## 범위와 원인

1차 변경은 보존했다. 시작 시 Unity의 실제 오류는 다음 두 건이었다.

```text
Assets\Tests\Editor\LSDFDeterministicContractTests.cs(2,14): error CS0234:
The type or namespace name 'Deterministic' does not exist in the namespace 'Photon'
Assets\Tests\Editor\LSDFOnlineSmokeTests.cs(7,14): error CS0234:
The type or namespace name 'Deterministic' does not exist in the namespace 'Photon'
```

테스트 asmdef에 설치된 Quantum의 precompiled DLL 참조를 명시했다.
`Quantum.Deterministic`, `Quantum.Engine`, `Quantum.Log`, `PhotonClient`, NUnit을 직접 참조한다.
SDK DLL importer나 SDK 어셈블리 설정을 바꾸지 않았다. 이전 접근 제한자 수정은 유지했다.
이어 드러난 `NUnit.Framework.Assert`와 `Quantum.Assert`의 CS0104 이름 충돌은
세 테스트 파일의 `using Assert = NUnit.Framework.Assert;`로 해결했다.

## 2차: R04의 첫 단타 이관

- `LSDFSingleHitDefinition`: 공유 Quantum 이벤트 에셋에 직렬화되는 불변 설정. 실행 중 값을 쓰지 않는다.
- `LSDFHitboxFactory`: Transform/Collider/HitboxInfo/TickToDestroy를 생성하는 무상태 helper.
- `LeftUpperWindowEvent`: 기존 타입과 GUID를 유지하고 위 정의/생성기를 실제 호출한다.
- `Left Upper Window Event.asset`: 기존 asset ID를 유지하며 기존 타격 수치를 명시적으로 저장했다.
- 기존 CP949 파일은 필요한 구간만 바이트 보존 치환했다. DSL·시스템 등록 순서·다른 공격은 이 단계에서 변경하지 않았다.

보존 계약:

| 항목 | 기존값/2차 목표값 |
|---|---|
| 발동/생성 | 13프레임 / 애니메이션 12프레임에 생성 |
| 파괴 | 생성 시뮬레이션 tick + 1 |
| 위치 | 공격자 위치 + (0.25 × 방향, 0) |
| 반크기 | FP._0_10 / 2, (FP._0_33 - FP._0_03) / 2 |
| 공격 속성 | Mid / Normal counter / Normal guard / Homing parry |
| 피해 / 가드 / 일반 / 카운터 경직 | 12 / 19 / 25 / 25 |

주의: SDK의 `FP._0_33`은 정확한 0.33이 아니라 약 1/3이다. 기존 FP 식의 raw 값으로 저장해 의미를 보존했다.
설정 클래스를 추가하는 데 그치지 않고 기존 공격의 실제 생성 경로를 연결했다.
다단히트·연타 공격까지 일반화하지 않았으며 R04 전체 완료는 아니다.

## 검증 계획과 결과

기존 오류 로그 조회는 원인 조사이며 테스트 재실행이 아니다. 구현 후 최종 한 묶음만 실행한다.
단위 검사에서는 실제 에셋을 읽어 수치·판정 틱·좌우 반전·payload를 확인한다.
온라인 검사는 독립 Photon 연결 2개와 세션 2개로 다른 로드아웃, 피해,
중복 AddPlayer, LeftUpper 히트박스/수명, 같은 verified tick 체크섬을 관측한다.
온라인 테스트의 정리 과정에서 한 클라이언트 종료 실패가 다른 클라이언트 정리를 막지 않게 했다.

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS — DLL 참조·Assert 별칭 수정 후 완료, 콘솔 오류 0건 확인 |
| 단위·에셋 계약 테스트 | PASS — 23건 통과: 로드아웃/FP/피해/상태 18건 + 단타 정의/타이밍/반전 5건 |
| 온라인 2클라이언트 | FAIL — Photon 세션 시작 시 Session start timed out; 전투·체크섬 단계 미도달 |
| 정적 정책 / diff | PASS — Simulation C# 83개, 기존 패턴 5건, 신규 0건; diff 공백 오류 없음 |
| 강제 지연·롤백 재생·독립 빌드·UI/카메라 | NOT RUN — 이번 스모크 범위 아님 |

실제 Unity 테스트 작업 ID: `3e22e049178e46fcb4fffe267408afd5`, 총 24건 중 23 PASS / 1 FAIL / 0 SKIP.
[기록한 결과](evidence/phase2-test-results.json)에 요약과 실패 원인을 보존했다.

온라인 실패의 실제 오류:

```text
Quantum.SessionRunnerException: Session start timed out
RealtimeClient.SendOutgoingCommands() was not called for > 5 seconds.
```

두 연결의 matchmaking await를 지난 뒤 SessionRunner.StartAsync 대기에서 실패했다.
테스트 당시 Editor는 비활성 창이며 runInBackground=false였다. 따라서 백그라운드 Update/네트워크 서비스
중단이 원인 후보지만 서버/프로토콜 요인까지 배제한 것은 아니다.
테스트가 PlayMode에 진입한 직후 runInBackground=true로 설정하고 finally에서 원래 값으로 복원하도록 보완했다.
이는 **테스트 실행 보완 후 미검증**이다. 이번 실행을 반복하지 않았고 온라인 통과라고 기록하지 않는다.
검사 후 Editor의 원래 runInBackground=false를 복원했으며 씬/ProjectSettings 변경은 없었다.

23건의 통과는 백그라운드 설정 보완 전 게임 코드·데이터에 대한 결과다.
게임 코드·단타 에셋은 이후 변경하지 않았다. 컴파일 재검증이 필요한 후속 변경은 온라인 테스트 실행 설정뿐이다.

## 다음 범위

단타 경로의 검증 근거를 확보한 뒤 Jap 연타 → 다단히트 순으로 R04를 확장한다.
로드아웃 비용 데이터 불일치, 실제 재접속/슬롯 교체, PlayerSystem 책임 분리, 라운드 상태 기계는 남아 있다.
기존 [1차 기록](refactor-p0-result.md)은 당시 검증 이력이며 이 문서의 최신 결과와 구분한다.
