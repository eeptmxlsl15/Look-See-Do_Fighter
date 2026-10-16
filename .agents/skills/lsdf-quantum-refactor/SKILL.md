---
name: lsdf-quantum-refactor
description: Refactor Look-See-Do Fighter gameplay under Photon Quantum 3 determinism, rollback, asset, and simulation/view constraints. Use for this project's combat, loadout, round, or architecture changes.
---

# LSDF Quantum 리팩토링

프로젝트 루트 `AGENTS.md`를 적용한다. 이 스킬 기준 상대 루트는 `../../..`이다.
[초기 감사](../../../docs/ai/architecture-audit.md)와 [로드맵](../../../docs/ai/refactor-roadmap.md)에서 해당 영역만 읽고 현재 소스로 재확인한다.

## 작업 방법

1. 요청이 조사인지 수정인지 확인하고 Git의 기존 변경을 식별한다. 조사만 요청했다면 코드 패치를 만들지 않는다.
2. 변경 대상의 입력 → Frame 상태 → System/Animator 이벤트 → View 흐름을 실제 파일로 추적한다.
3. 현재 동작을 구체적인 틱·피해량·입력/스킬 ID 사례로 고정한다. 버그 수정은 의도적으로 달라질 결과를 따로 명시한다.
4. 아래 경계에 따라 한 기능씩 변경한다. 기존 경로를 제거하기 전에 호출자, `.meta`, 프로토타입과 Animator 참조를 확인한다.
5. 구현을 모두 마친 뒤 [검증 절차](../../../docs/ai/validation.md)의 필요한 항목만 한 차례 실행하고 기록한다.
   다른 스킬/훅에서 이미 수행한 검사를 반복하지 않는다. 실패 수정 후 재실행하지 않고 미검증으로 보고한다.
   정적 검사 통과만으로 완료된 결정론 검증이라 주장하지 않는다.

## 이 프로젝트에서 특히 중요한 경계

- `LSDF_PlayerSpawnSystem.OnPlayerAdded`는 현재 `PlayerPrefs`를 읽는다. 저장소 접근은 메뉴 측으로 옮기고,
  실제 `QuantumMenuConnectionBehaviourSDK`의 `Runner.Game.AddPlayer` 이전에 각 플레이어의 검증된 payload를 준비한다.
  Quantum 3 JSON 직렬화, 28칸 길이, 허용 ID/코스트, 누락 데이터 기본값, 재접속 및 `firstTime` 의미를 함께 확인한다.
  `RuntimePlayer.IsLocalPlayer`로 각 클라이언트에서 서로 다른 판정을 하지 않는다.
- `AnimatorTimeWindowEventAsset`는 공유 AssetObject다. `bufferedNextAttack`처럼 틱을 넘는 상태는 엔티티 컴포넌트로,
  `currentFrame` 같은 재계산값은 지역변수로 옮긴다. 두 플레이어가 같은 에셋을 쓰는 테스트가 필요하다.
- 설치된 애드온은 시간창 평가에서 `Execute`를 `OnEnter`보다 먼저 호출할 수 있다.
  일반 Unity Animator 콜백 순서를 가정하지 말고 실제 애드온 구현과 첫 프레임 동작을 확인한다.
- `AsFloat * 60`은 FP/정수 계산으로 교체하되 기존 `HitFrame - 1` 타이밍과 반올림을 테스트한다.
  `SessionConfig.asset`의 현재 값은 60Hz이며 이를 바꾸는 작업과 섞지 않는다.
- 공격 37개를 한꺼번에 일반화하지 않는다. 단타 → 분기 연타 → 다단히트 → 런처/벽/패링 순으로 데이터 모델을 검증한다.
  Hitbox 생성·파괴 틱, 중복 피격 방지, 소유자 필터링을 보존하거나 명시적으로 수정한다.
- `LSDF_SystemConfig.asset`의 순서와 `SystemSetup.User.cs` 양쪽을 확인한다. 중복 등록하지 않는다.
- 메뉴 SDK 경로의 커스텀 코드를 옮길 때 Quantum.Menu → Quantum.Simulation 의존을 확인한다.
  새 View asmdef가 기존 메뉴와 서로 참조하는 순환을 만들지 않도록 한다.

## 결과

수정한 파일, 결정론/직렬화 영향, 유지된 밸런스, 실행한 검사와 미실행 검사, 다음 작업을 짧게 보고한다.
필요한 설계 결정은 `docs/ai/decisions/`에 작업 시 생성한다. 빈 ADR이나 증거 파일을 미리 양산하지 않는다.
리팩토링 이후 기능 추가·버그 수정은 [개발 운영 지침](../../../docs/ai/development-workflow.md)으로 이어간다.
요청 범위의 로컬 작업은 단계별 허가 질문 없이 완료한다.
