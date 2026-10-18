# 공중 피격 시 공격 중단 — 2026-10-19

기준: `dev/Prototype`, `fa955c72c4c6b672e67911a97ab3f01920f22ee6` 이후 로컬 변경.
사용자는 [전환 경합 조사](counter-air-interrupt-investigation.md)의 Air 요청 유실 경로를 수정하도록 요청했다.

## 변경

- [피격 처리](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs)의 일반 런처·카운터 공중 피격·공중 재피격에서
  트리거 예약 대신 [LSDFHitReactions.EnterAir](../../Assets/QuantumUser/Simulation/LSDFHitReactions.cs)를 호출한다.
  피격 시점에 Frame의 Animator 현재 상태를 Air로 확정하고 진행 중인 일반 전환의 시간·대상을 비운다.
- 끊긴 공격의 `BufferedNextAttack`, `NextAttack`, 커맨드 예약/라우팅, 공격 버튼 트리거와 이동 예약을 정리한다.
  `PreviousAttackButtons`는 보존하여 누르고 있던 버튼을 새로운 입력으로 잘못 인식하지 않는다.
- 이미 생성된 피격자 소유 공격 hitbox의 만료를 현재 tick으로 당긴다. 물리 콜백 도중 바로 삭제하지 않고
  기존 `CollisionDestroySystem` 위치에서 제거하여 같은 tick의 상호 접촉 순서를 보존한다.
- 첫 Air 진입은 시간 0부터 native Animator 이벤트를 처리한다. 이미 Air인 재피격은 시간을 되감지 않아
  `AirWindowEvent.OnEnter` 및 `hitCount`를 중복 실행하지 않는다.
- 시스템 등록 순서, 그래프 전역 전환 옵션, SDK, 카메라 줌·전체 흔들림·체력바 연출, 2P 자동 입력 설정은 보존한다.
  피해량·경직 수치·공중 궤적 계산도 변경하지 않는다.
- 시뮬레이션 결과가 달라지므로 프로토콜을 `lsdf-loadout-v13-air-hit-interruption`으로 올린다.
  `.qtn` 변경은 없으므로 CodeGen 대상은 없다. 기존 Combat 파일은 ASCII 치환만 하여 원래 인코딩을 보존했다.

## 최종 검증 한 차례

`lsdf-quantum-verify` 절차를 같은 작업의 최종 묶음으로 적용한다. 결과는
[검증 근거](evidence/air-hit-interruption-validation.json)에 기록한다.

추가한 회귀 fixture는 로컬 Quantum runner의 Frame, 실제 Player/Enemy 그래프와 기존 Juggle/피격 반응 연산을 사용하도록 작성했다.
세 연타 상태 × 두 피격 종류 × 입력 없음/수동 반복 press/2P 자동 입력 producer를 조합하고,
각 그래프에서 이미 시작된 LeftUpper fade도 중단한다(총 38개 경계 사례).
각 사례는 Air 확정, 첫 이벤트 1회, 예약/소유 hitbox 정리, 다른 엔티티 격리,
동일 Frame snapshot 복원 후 체크섬 일치, 공중 재피격 및 기존 착지 전환을 관측한다.

| 검사 | 결과 |
|---|---|
| 시뮬레이션 정책 | PASS — 94개 파일, 신규 적중 0 |
| Unity 컴파일 | **수정 후 미검증** — 첫 시도에서 테스트의 CS0200/CS1503 발생. 읽기 전용 TriggerInfo2D 생성 제거 및 FP 비교식 보정 후 재실행하지 않음 |
| 전투/입력 회귀·snapshot 복원 | **NOT RUN** — 컴파일 실패로 테스트 job을 시작하지 않음. 38개 사례는 fixture 소스의 계획 수이며 통과 수가 아님 |
| 전체 입력 replay·강제 온라인 rollback | **NOT RUN** |
| diff 공백·문서 링크 | PASS — `git diff --check`, 관련 링크 35개 및 새 C# `.meta` 짝 확인 |

위 표는 당시 자동 검사 결과다. 이후 사용자가 **“테스트는 내가 했어 커밋해줘”**라고 확인했다.
[사용자 확인 기록](user-validation-air-hit-interruption.md)을 최신 로컬 커밋 근거로 사용하고, 게임 검사를 다시 실행하지 않는다.
사용자 확인을 자동 컴파일·회귀·체크섬 PASS로 바꾸지 않는다.

이 fixture는 피격 경계 상태를 직접 구성한다. 실제 물리 접촉에서 같은 입력으로 사용자가 본 특정 기술을 재현하는
전체 매치 테스트와 구분한다. TriggerInfo2D는 엔진 소유의 읽기 전용 접촉 데이터라 생성하지 않으며 접촉 dispatch와 피해 계산은 fixture 범위 밖이다.
snapshot 복원 비교 역시 강제 네트워크 rollback·입력 전체 replay·플랫폼 간 결정론의 증명은 아니다.
해당 온라인/전체 replay 검사는 실행하지 않으면 NOT RUN으로 남긴다.

## 보존할 이력

카메라·콜라이더 작업의 과거 자동 실패와 사용자 테스트 완료 확인은 [현재 상태](current-state.md)의 별도 기록을 유지한다.
이번 수정의 검사 결과로 과거 온라인 기상 실패를 해소했다고 주장하지 않는다.
후속 사용자 요청으로 이번 수정과 기록을 로컬 커밋에 포함하며 Push/PR은 요청받지 않았다.
