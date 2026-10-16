# 단계별 리팩토링 계획

현재 소스 근거, 작업 ID별 수정 범위·완료 조건·멀티플레이 검증은 [리팩토링 백로그](refactoring-backlog.md)를 참고한다.

이 문서는 후속 구현 계획이다. 이 세팅에서 아래 게임 코드 수정이 완료된 것은 아니다.
각 작업은 필요한 검사를 구현 완료 후 한 차례만 수행한다. 이 로드맵 전체의 완료가 기능 개발의 선행 조건은 아니다.
기능 추가·버그 수정·완성도 개선은 [개발 운영 지침](development-workflow.md)에 따라 병행한다.

## P0 — 결정론과 상태 소유권

| 작업 | 범위 | 완료 조건 |
|---|---|---|
| P0-1 로드아웃 전달 | 메뉴/RuntimePlayer/Spawn | 로컬 저장값을 메뉴에서 수집 → 길이/ID/코스트 검증 → AddPlayer payload → Frame 복사. 서로 다른 저장값의 두 클라이언트에서도 각 PlayerRef의 동일 payload 사용 |
| P0-2 판정 수학 | 공격 이벤트/Air/카운터 피해 | FP 또는 정수만 사용. 60Hz 경계 틱과 피해 버림 규칙의 전후 비교, 동일 입력 체크섬 검증 |
| P0-3 공유 실행 상태 | 이벤트 asset/새 QTN 상태 | 연타 버퍼 등은 엔티티별 Frame 상태, 계산값은 지역변수. 같은 스킬을 두 플레이어가 동시에 사용하고 롤백 후 결과 일치 |

P0-1에서 `firstTime` 재호출/재접속, 중복 스폰, invalid PlayerAvatar, null/짧은/긴 스킬맵,
허용되지 않은 ID, 코스트 초과를 다룬다. UI 검증만으로 충분하다고 가정하지 않는다.
게임 도중 로드아웃 변경이 필요하면 그 요구가 생겼을 때 결정론적 Command 경로를 설계한다.

## P1 — 데이터와 책임 분리

1. 현재 수치·틱 결과를 테스트 fixture/기록으로 고정한다. HP/이동/라운드 설정을 `AssetObject`로 옮기고 실제 참조를 연결한다.
2. 공격 정의(읽기 전용)와 공격 상태(Frame)를 분리한다. 기존 이벤트 AssetObject는 필요하면 어댑터로 유지한다.
3. 단타 하나를 공통 hitbox 생성기로 이관한다. 다음은 `Jap` 연타, `NarakTwo`/`LarsCutKick` 다단히트로 일반성 검증.
4. PlayerSystem의 이동/피격/상태/라운드 책임을 작은 시스템 또는 stateless helper로 나눈다.
   시스템 분리 자체가 Update 순서 변화를 만들면 최종 한 차례 검증에 회귀 시나리오를 포함한다.
5. 라운드는 Intro/Fighting/RoundEnd/MatchEnd 등의 명시 상태를 검토한다. 실제 제한시간/종료 기능 추가는 리팩토링과 분리한다.

설정화 시 기존 asset ID/GUID, 변수명과 값의 의미를 보존한다. 새 데이터가 사용되지 않는 '설정 파일만 추가' 상태를 완료로 보지 않는다.

## P2 — 표현·메뉴 경계

- 빈 MonoBehaviour와 사용자 View를 올바른 계층으로 정리한다. 참조 없는 코드는 사용처를 확인한 후에만 제거한다.
- 메뉴 사용자 코드 이관 전에 `Quantum.Menu.asmdef`와 새 어셈블리의 참조 방향을 설계한다.
- 직렬화된 참조로 UI/카메라 이름 검색을 줄이고 local player, entity, runner 수명에 맞춰 재바인딩한다.
- Quantum 이벤트 예측/취소/확정에 맞는 VFX/SFX 정책을 정한다. 모든 이벤트를 무조건 확정 전용으로 바꾸지 않는다.
- `Down` 화살표, 콤보 피해 분기, 포인터 실패 경로는 각각 재현 가능한 버그 수정으로 처리한다.

## 작업 후 목표 구조 (아직 생성/이관하지 않은 설계)

```text
Assets/QuantumUser/Simulation/
  Combat/       # systems, stateless helpers, DSL state
  Data/         # immutable Quantum asset types
  Players/      # spawn, loadout, player data
  Match/        # round state and lifecycle
  Generated/    # codegen only
Assets/QuantumUser/View/
  Input/  UI/  Camera/  Effects/
```

이는 프로젝트 내부 분류 제안이며 Photon이 강제하는 하위 폴더 명칭은 아니다.
Quantum의 요구는 올바른 어셈블리·결정론·상태 경계다. 폴더만 바꾸는 것으로 이를 달성했다고 주장하지 않는다.

## 변경 단위의 완료 정의

범위와 보존 동작 → 구현/필요한 CodeGen → 한 차례 최종 검증(컴파일·관련 시나리오/체크섬) → 증거 문서.
못 실행한 검사가 있으면 코드 작업 완료와 검증 완료를 구분한다. 다음 단계로 넘어갈 때 그 제한을 이어받는다.
실패 시 재검증을 반복하지 않는다. 수정 후 재실행하지 않은 결과는 `수정 후 미검증`으로 남긴다.
