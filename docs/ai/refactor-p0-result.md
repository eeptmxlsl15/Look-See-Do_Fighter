# P0 리팩토링 작업 기록 — 2026-10-16

후속 상태: [1차 오류 수정·2차 결과](refactor-phase2-result.md)에서 컴파일 오류 해소와 테스트 23건 통과를 확인했다.
아래 실패/미실행 표는 최초 1차 작업 당시의 기록이다.

기준 커밋: `64a37db6`. 시작 시 Git 작업 트리는 깨끗했다. 커밋/푸시/배포는 하지 않는다.
범위는 R01 전달 경로, R02 결정론 수학, R03 상태 소유권과 R08의 static 가드 결과 제거다.
공격 데이터 일반화·라운드 기능·View 이관은 이번 변경에 포함하지 않는다.

## 변경 사항

- `RuntimePlayer.CommandSkills`에 28개 슬롯을 전달한다. 메뉴의 프로젝트 전용 partial에서
  로컬 저장값을 복사하고 기존 AddPlayer 경로를 사용한다. 원본 RuntimePlayer 설정 인스턴스를 직접 변경하지 않는다.
- Simulation은 payload만 읽는다. 잘못된 배열 길이는 기본 로드아웃, 슬롯별 잘못된 ID는 0으로 처리한다.
  허용 ID는 현재 `LSDF_PlayerAnimatorGraph`의 FinalNum 분기를 기준으로 한다.
- 같은 PlayerRef의 기존 전투 엔티티가 있으면 중복 생성/HP 리셋을 하지 않는다. 잘못된 avatar와
  필수 컴포넌트가 없는 프로토타입은 스폰을 진행하지 않는다. 전역 시간은 첫 PlayerRef 0 스폰에서만 초기화한다.
- 공격 이벤트 37개와 Air 이벤트의 시간 환산을 `LSDFCombatMath.AnimationFrame`으로 통일한다.
  60은 애니메이션의 기존 저작 기준이며 세션 주기 변경은 하지 않았다. 카운터 피해는 정수 `damage * 6 / 5`다.
- `currentFrame` 공유 필드는 호출 지역값으로 변경했다. Jap/JapJap의 연타 예약은
  `LSDF_Player.qtn`의 `BufferedNextAttack`으로 옮겼다. 다른 공격의 미사용 버퍼 필드는 제거했다.
- 변경 대상 공격 콜백의 Player 포인터 조회 실패는 즉시 반환한다. CollisionSystem의 가드 결과 static을 제거했다.
- Quantum CodeGen으로 DSL 생성물을 갱신한다. Generated 코드를 손으로 편집하지 않는다.
- 메뉴 연결의 AppVersion에 `lsdf-loadout-v1` 접미사를 붙여 이전 payload/DSL 버전과 매칭되지 않게 한다.
- 기존 CP949 소스는 바이트 보존 방식으로 필요한 구문만 치환했다. 기존 GUID/Animator 에셋/시스템 순서는 유지했다.

## 파일 위치

| 목적 | 위치 |
|---|---|
| 로드아웃 계약/ID 검증 | `Assets/QuantumUser/Simulation/LSDFLoadout.cs` |
| FP 틱·정수 피해 | `Assets/QuantumUser/Simulation/LSDFCombatMath.cs` |
| 중복 스폰 정책 | `Assets/QuantumUser/Simulation/LSDFSpawnPolicy.cs` |
| 로컬 저장 어댑터 | `Assets/Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviour.LSDF.cs` |
| 전송/복사/Frame 상태 | `RuntimePlayer.User.cs`, `LSDF_PlayerSpawnSystem.cs`, `LSDF_Player.qtn` |
| 단위 계약 테스트 | `Assets/Tests/Editor/LSDFDeterministicContractTests.cs` |
| 온라인 2클라이언트 스모크 | `Assets/Tests/Editor/LSDFOnlineSmokeTests.cs` |
| 일회성 인코딩 보존 이관 기록 | `tools/ai/refactor_p0.ps1` (재실행용 아님) |

## 명시적 제한/남은 작업

1. 메뉴에 같은 슬롯/ID의 서로 다른 비용과 기본값이 섞여 있다. 예: 슬롯 0/ID 0은 비용 0과 5,
   슬롯 4/ID 0은 0과 6이다. 비용을 임의로 결정하지 않았으므로 **수신 payload 비용 검증은 미구현**이다.
   R01 전체 완료로 표시하지 않는다. 권위 있는 불변 로드아웃 카탈로그를 정한 뒤 UI/Simulation에서 함께 사용해야 한다.
2. 재접속/중복 AddPlayer에서는 기존 전투 상태와 로드아웃을 보존한다. 퇴장한 슬롯에 다른 사람이 참여할 때의
   교체/초기화 정책과 실제 연결 해제·재접속 테스트는 후속 R07이다. `firstTime`을 재접속 여부와 동일시하지 않는다.
3. 온라인 테스트는 동일 Editor 프로세스 내 **별개 Photon 연결 2개/Quantum 세션 2개**다.
   같은 방에서 payload·전투 피해·중복 AddPlayer·같은 verified tick 체크섬을 확인한다.
   UI/카메라/독립 실행 빌드/교차 플랫폼/강제 지연/실제 재접속은 이 테스트가 검증하지 않는다.
4. FP 환산은 정확한 고정소수 값에서 절삭한다. 과거 float의 경계 반올림과 완전 동일하다고 주장하지 않는다.
   각 공격의 실제 활성 틱 전후 비교와 입력 재생/유도 롤백은 추가 근거가 필요하다.
5. 정적 검사 기준선은 제거한 패턴 151건을 삭제해 5건만 남겼다. 이는 버그 151개 해결이나 결정론 증명이 아니다.
6. 이관 스크립트는 모든 anchor를 확인한 뒤 기존 파일에 적용한 일회성 기록이다. 이미 이관된 프로젝트에 다시 실행하지 않는다.

## 한 차례 최종 검증

최종 검증은 한 차례 실행했다. Unity 컴파일 실패 후 원인을 수정했고 재실행하지 않았다.
따라서 이 변경은 **수정 후 미검증이며, 게임에서 사용 가능한 상태로 검증 완료된 것이 아니다.**

| 검사 | 결과 |
|---|---|
| Quantum CodeGen | 생성 코드에 BufferedNextAttack 필드/체크섬/직렬화 반영 확인 |
| Unity 컴파일 | FAIL → 접근 제한자 수정 후 미검증 |
| LSDF.Tests 단위 테스트 | NOT RUN — 컴파일 실패로 실행하지 않음 |
| 실제 온라인 2클라이언트 스모크 | NOT RUN — 컴파일 실패로 실행하지 않음 |
| quantum_guard | PASS — Simulation C# 81개, 기존 적중 5건, 신규 0건 |
| git diff --check / 결과 문서 링크 | PASS — 최종 결과 기록/접근 제한자 수정 이전 기준 |
| 독립 빌드·UI·입력 재생·유도 롤백·실제 재접속 | NOT RUN — 이번 테스트 범위 아님 |

실제 컴파일 오류:

```text
Assets\Photon\QuantumMenu\Runtime\QuantumMenuConnectionBehaviourSDK.cs(145,7): error CS0122:
'QuantumMenuConnectionBehaviour.PrepareLSDFPlayers(QuantumMenuConnectArgs)' is inaccessible due to its protection level
```

SDK 파일은 앞부분에 base partial, 뒷부분에 파생 SDK 클래스가 함께 있다.
파생 클래스의 ConnectAsyncInternal에서 호출하는 확장 메서드를 `private static`에서 `protected static`으로 수정했다.
기존 SDK 경고 CS0114/CS0618도 남아 있으며, 이를 이 작업의 범위 밖에서 일괄 수정하지 않았다.
테스트 실행을 재시도하지 않았고 PlayMode로 진입하거나 온라인 방을 생성하지 않았다.
다음 검증이 허용되는 작업에서는 컴파일을 먼저 확인한 뒤 `LSDF.Tests` EditMode 테스트 묶음을 실행해야 한다.
이 묶음의 온라인 테스트는 자체적으로 PlayMode에 진입하고 종료한다.

## 설계 근거

설치된 Quantum 3.0.14 소스의 RuntimePlayer JSON serializer, OnPlayerAdded, AnimatorTimeWindowEvent 평가 순서를 확인했다.
설계 경계는 [Photon Player](https://doc.photonengine.com/quantum/v3/manual/player/player)와
[Fixed Point](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/fixed-point)를 함께 참고했다.
`Execute` 뒤 `OnEnter`가 호출될 수 있는 기존 애드온 순서와 공격별 HitFrame/수치는 변경하지 않았다.
