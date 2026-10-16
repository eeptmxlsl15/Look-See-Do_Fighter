---
name: lsdf-game-development
description: Implement features, fix bugs, tune combat, improve UI and presentation, and finish Look-See-Do Fighter under Photon Quantum 3 architecture. Use for ongoing game development beyond refactoring, with one final validation pass per task.
---

# LSDF 게임 개발

루트 `AGENTS.md`와 [개발 운영 지침](../../../docs/ai/development-workflow.md)을 따른다.
일반 구현 선택은 재확인 없이 진행한다. 사용자가 조사만 요청한 경우에는 조사 범위를 유지한다.

## 작업 흐름

1. 사용자 요청을 플레이어가 경험할 동작과 완료 조건으로 구체화한다. 부족한 세부사항은 현재 게임에 맞춰 가정하고 알린다.
2. 관련 입력/메뉴 → RuntimePlayer 또는 Quantum Input/Command → Frame → 시스템/이벤트 → View 경로를 읽는다.
3. 필요한 상태는 QTN/Frame, 불변 수치는 Quantum AssetObject, 표현은 Unity View에 둔다.
   리팩토링이 끝날 때까지 기능 개발을 무조건 미루지 않는다. 해당 기능의 결정론을 방해하는 부분만 먼저 고친다.
4. 기능 전체를 연결한다. 코드 클래스만 만들지 말고 설정 참조, 시스템 등록, 입력, UI, 종료/재진입 경로까지 요청 범위를 완성한다.
5. 구현이 끝나면 [검증 매트릭스](../../../docs/ai/validation.md)의 필요한 항목을 **한 차례만** 실행한다.
   다른 스킬이 이미 실행했다면 그 결과를 재사용한다. 실패 수정 후 자동 재실행하지 않고 미검증을 표시한다.
6. 완료된 동작, 주요 설계, 한 차례 검증 결과, 남은 항목을 보고하고 관련 문서를 갱신한다.

## 작업별 판단

- 기능 추가: 수용 조건, 기존 기능과의 충돌, player/entity/runner 수명, 연결 실패·재진입 같은 경계를 함께 설계한다.
- 버그 수정: 증상과 원인을 구별한다. 기존 로그/소스/제공된 재현 자료로 가설을 좁혀 수정한다.
  실행 재현이 반드시 필요하면 그것을 한 차례 검증 예산으로 사용하고 이후 수정은 미검증으로 명시한다.
- 전투 밸런스: 의도적으로 바뀌는 수치를 명시한다. 판정 프레임, 데미지 버림, 경직, hitbox 수명은 설정과 동작을 함께 맞춘다.
- UI/연출: 예측/확정 이벤트 및 재활성화 수명을 고려한다. 코루틴이나 Unity Animator가 게임 결과를 결정하지 않게 한다.
- 게임 완성: 개발 운영 지침의 마일스톤에서 사용자가 지정한 범위를 구현한다. 미래 후보 기능을 이미 합의된 필수 기능으로 취급하지 않는다.

## 현재 프로젝트 주의점

실제 시스템 구성은 `Assets/Resources/LSDF_SystemConfig.asset`이다.
메뉴 커스텀 코드는 `Assets/Photon/QuantumMenu`에 섞여 있다.
스킬 슬롯 28개와 Animator 변수/ID, Asset GUID를 유지하거나 호환 이관한다.
자세한 초기 위험은 [소스 감사](../../../docs/ai/architecture-audit.md)를 해당 부분만 참고한다.
