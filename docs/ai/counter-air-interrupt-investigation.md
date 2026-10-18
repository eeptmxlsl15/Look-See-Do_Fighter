# 카운터 공중 피격 중 공격 지속 조사 — 2026-10-19

이 문서는 수정 전 조사 기록이다. 이후 사용자의 명시적 요청으로 [공중 피격 우선 전환](air-hit-interruption-fix.md)을 구현했으며,
현재 코드와 최신 검증 상태는 해당 후속 기록 및 [현재 상태](current-state.md)를 따른다.

기준 커밋: `fa955c72c4c6b672e67911a97ab3f01920f22ee6`. 요청은 원인 조사이며 게임 로직/Animator/입력 설정은 수정하지 않았다.

## 판단

2P 자동 입력만의 특수 동작으로 넘길 수 없는 **공통 피격 전환 결함 경로**를 소스에서 확인했다.
공중 물리 상태가 시작돼도 `Air` Animator 요청이 같은 틱의 공격/연타 전환에 밀려 사라질 수 있다.
다만 사용자가 본 정확한 기술·발생 프레임을 실행으로 재현하지 않았으므로 이번 관측의 최종 원인을 확정했다고 표현하지 않는다.

## 2P 자동 입력의 영향

- [설정](../../Assets/QuantumUser/Resources/LSDF_Player2TestInput.asset)은 Enabled=1, 중립 RP, Repeat=1,
  InitialDelayTicks=0, IntervalTicks=1, PressTicks=1이다. [재생기](../../Assets/QuantumUser/View/LSDFPlayer2TestInputPlayback.cs)는
  release 1틱을 보장하므로 실제 주기는 2틱이다. 60틱 기준 초당 약 30회 press-edge를 만든다.
- [입력 컴포넌트](../../Assets/QuantumUser/View/LSDF_PlayerInput.cs)는 일반 CallbackPollInput에 이 입력을 넣는다.
  기술 실행·Animator·Frame을 강제로 바꾸지 않는다.
- [커맨드 버퍼](../../Assets/QuantumUser/Simulation/LSDFInputBuffer.cs)의 CanExecute는 피격/가드/공중/공격 중 새 커맨드를 차단한다.
  [커맨드 처리기](../../Assets/QuantumUser/Simulation/LSDFCommandProcessor.cs)도 같은 경로를 사용한다.
- 자동 입력의 매우 짧은 주기는 커맨드 예약/연타 조건을 자주 만들 수 있다. 발생 빈도에 영향을 줄 가능성은 있지만
  피격 중 공중 공격을 허용하는 별도 우회 경로는 아니다. 수동으로 같은 입력을 보내도 아래 전환 경합은 가능하다.

## 확인한 전환 경합

1. [카운터 처리](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs)는 isAttack을 false로 하고
   LSDFJuggle.OnHit을 호출한 다음 `Air` 트리거를 켠다. [공중 처리](../../Assets/QuantumUser/Simulation/LSDFJuggle.cs)는
   Frame의 공중 상태와 물리를 시작한다. Animator가 Air에 들어갔는지 기다리는 구조는 아니다.
2. Player/Enemy 두 [그래프](../../Assets/QuantumUser/Animators/LSDF_PlayerAnimatorGraph.asset)는 상태 96개 중
   Any State가 마지막(0부터 센 순서 95)에 있고 `AllowTransitionInterruption: 0`이다.
   [적 그래프](../../Assets/QuantumUser/Animators/LSDF_EnemyAnimatorGraph.asset)도 같은 조건이다.
3. [AnimatorLayer](../../Assets/Photon/QuantumAddons/Animator/Simulation/Core/AnimatorLayer.cs)는 상태를 배열 순서로 처리한다.
   일반 상태가 먼저 전환을 선택하면 Duration=0이어도 레이어 처리 끝까지 ToStateId가 남는다.
4. [AnimatorTransition](../../Assets/Photon/QuantumAddons/Animator/Simulation/Core/AnimatorTransition.cs)은
   `AllowTransitionInterruption || ToStateId == 0`인 경우에만 다음 전환을 허용한다.
   따라서 뒤에 있는 Any State의 Air 전환이 막힐 수 있다.
5. [AnimatorTriggersSystem](../../Assets/Photon/QuantumAddons/Animator/Simulation/Systems/AnimatorTriggersSystem.cs)은
   그 틱의 트리거를 초기화한다. 채택되지 않은 Air도 다음 틱에 자동 재시도되지 않는다.

구체적인 경합 예는 `0_Jap → 0_JapTwo`, `1_Jap → 1_JapJap`, `1_JapJap → 1_JapJapTwo`의 NextAttack 전환이다.
[Jap 이벤트](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/JapWindowEvent.cs)는
이미 예약된 BufferedNextAttack을 피격 여부와 별도로 NextAttack에 반영한다.
카운터 처리에서 이 예약/NextAttack을 정리하지 않으므로 같은 틱의 연타 전환이 Air보다 먼저 선택될 여지가 있다.
일반 공격 이벤트 Execute도 isAttack=false만으로 자동 중단되지 않아 Animator가 공격 상태에 남으면 실행이 계속될 수 있다.
이 경로는 관찰된 “몸은 뜨는데 기술을 계속 수행함”과 일치하는 원인 후보다.
위 연타 전환의 허용 시점이 실제 카운터 가능 구간과 겹쳤는지는 실행으로 확인하지 않았다.
따라서 이 정적 경합 예를 사용자가 겪은 특정 카운터의 재현 사례로 취급하지 않는다.

추가로 `0_LeftUpper → Go To Idle`에는 0.25초 fade가 있으며 전이 중 Air 요청이 거부될 수 있다.
이는 별도의 경계 사례이며 사용자가 본 기술이 LeftUpper였다고 추정하지 않는다.

## 권장 수정 범위

피격 시 공중 전환을 일반 커맨드/연타 전환보다 우선시키고, 끊긴 공격의 예약 상태를 Frame에서 정리하는 프로젝트 확장 경로를 검토한다.
전역 AllowTransitionInterruption을 바로 켜거나 SDK 코어를 수정하면 다른 전환 순서까지 달라지므로 단순 토글로 해결하지 않는다.
후속 검증에서는 같은 기술·피격 tick에 수동 단발/수동 연타/자동 입력을 비교하고 양쪽 PlayerRef의 Animator 상태·공중 상태·공격 판정을 함께 관측한다.

## 확인 범위

소스/직렬화 에셋 분석만 수행했다. [분석 근거](evidence/counter-air-interrupt-investigation.json)는
입력 설정, 그래프 상태 순서/전환과 소스 해시를 기록한다. 정적 전환 모델은 런타임 재현 결과가 아니다.
사용자 증상의 실제 Frame 재현, 자동 입력 ON/OFF 실행 비교, 온라인·재생/롤백은 **NOT RUN**이다.
게임 코드 수정·컴파일·전투 테스트·커밋·Push는 수행하지 않았다.
