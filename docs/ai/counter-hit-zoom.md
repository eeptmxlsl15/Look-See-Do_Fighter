# 기술별 카운터 히트 줌

## 설정 방법

1. Project에서 `Assets/QuantumUser/Resources/LSDF_CounterHitZoom.asset`을 선택한다.
2. Inspector의 **전체 사용**을 켜고 목록에서 원하는 기술을 체크한다. 기술 이름으로 검색할 수 있다.
3. 기술 이름 왼쪽의 화살표를 펼쳐 아래 항목을 조절한다.

| 항목 | 의미 | 초기값 |
|---|---|---|
| 줌 배율 | 화면에서 커지는 배율. 1이면 확대 없음 | 1.7 |
| 줌인 시간 (초) | 타격점으로 확대하는 시간. 0이면 즉시 | 0.035 |
| 유지 시간 (초) | 가장 확대된 상태로 머무는 시간 | 0.08 |
| 복귀 시간 (초) | 기존 카메라로 돌아가는 시간 | 0.18 |
| 타격점 중심 이동 | 0은 기존 중심 유지, 1은 타격점을 화면 중앙에 배치 | 1 |
| 타격점 위치 보정 | 히트박스 중심에서 월드 좌표 X/Y 보정. Y를 올리면 위쪽으로 이동 | (0, 0) |

37개 기술 에셋을 목록에 연결했으며 **기술별 체크는 모두 꺼둔다**. 실제 사용할 기술은 사용자가 고른다.
새 기술 에셋을 만들면 **새 기술 목록 추가 / 기존 설정 유지** 버튼으로 목록에 추가할 수 있다.
기존 수치와 체크는 유지한다. 런타임 C# 수정이나 Animator 재베이크 없이 카메라 수치를 편집할 수 있다.

예: 원하는 기술을 체크하고 배율 1.8, 줌인 0.02초, 유지 0.07초, 복귀 0.16초로 설정한다.
같은 기술이 어느 커맨드에 배치되어 있어도 같은 설정을 사용하며, 연타의 후속 기술은 별도 항목이다.
Narak Two와 Lars Cut Kick의 두 타격은 각 기술의 설정 하나를 공유한다.

## 동작과 소유권

- 확정된 카운터 히트에만 실행한다. 일반 히트·가드·회피에는 줌 이벤트를 내보내지 않는다.
- 공격 Window Event의 Quantum GUID를 히트박스 생성 시 Frame의 `SourceAttack`에 기록한다. 상대의 로드아웃이나 충돌 순간의 Animator 상태로 기술을 추측하지 않는다.
- `OnTriggerCounterHit`에서 `synced event OnCounterHitZoom`으로 기술과 물리 히트박스 중심 좌표를 전달한다. 예측 취소된 타격은 카메라를 확대하지 않는다. 네트워크 확정까지의 지연은 남는다.
- 접촉 부위의 기준은 **히트박스 중심**이다. 뼈/픽셀 단위의 충돌점 추적은 아니다. 기존 VFX 좌표가 `(0, 0)`인 다단 공격도 실제 히트박스 위치를 사용한다.
- 설정은 Unity View의 ScriptableObject이며 게임 판정의 권위가 아니다. 피해·경직·히트박스 발동/파괴 틱·시스템 순서는 유지한다.
- `LSDFSceneBindings`가 해당 EntityViewUpdater의 Game과 로컬 시점을 확인해 하나의 구독으로 실행한다. 1P/2P 모두 동일 월드 위치를 바라보고 카메라 회전은 유지한다.
- 평상시 추적 좌표와 줌 좌표를 분리해 연속 발동 시 위치와 배율이 누적되지 않는다. 줌이 끝나면 움직이고 있는 평상시 추적 좌표와 기존 렌즈로 복귀한다.
- 같은 확정 틱에 여러 카운터가 발생하면 먼저 선택된 이벤트 하나를 사용한다. 다른 틱의 카운터는 연출을 다시 시작한다.
- Game 변경, 로컬 시점 소실, 카메라 변경, 비활성화, 라운드 회복/인트로, 뒤로 되감긴 확정 프레임에서 연출을 정리한다. KO 카운터의 짧은 줌은 결과 표시 중에도 끝까지 재생할 수 있다.
- 씬의 `EntityViewUpdater` → `LSDFSceneBindings.CounterHitZoom`에 설정 에셋을 연결했다. 카메라·프리팹의 기존 GUID와 참조는 유지한다.

## 검증

2026-10-17 최종 한 차례 검증 결과:

| 검사 | 결과 | 근거 |
|---|---|---|
| 설치된 Qtn CodeGen | PASS | 생성 Core/Events/Prototypes/UnityPrototypeAdapters에 SourceAttack과 Synced 이벤트 생성 |
| `python tools/ai/quantum_guard.py check` | PASS | Simulation 91개, 기존/신규 적중 0. 어휘 검사이며 결정론 증명은 아님 |
| 생성/참조 정적 검사 | PASS | 공격 생성 호출 39곳에 원본 GUID 전달, 검사 대상 소스/씬/설정 159개와 meta 짝 확인 |
| `git diff --check` | FAIL | CodeGen이 생성한 이벤트 생성자 2곳의 행 끝 공백. 생성 코드를 직접 편집하지 않음 |
| Unity 컴파일 | FAIL → 수정 후 미검증 | `LSDFOnlineSmokeTests.cs:233` CS0411. 테스트의 `FindAsset` 제네릭 타입을 명시하도록 수정 |
| 카메라/설정/수명 자동 테스트 | NOT RUN | 컴파일 실패 후 재검증하지 않음. 새 테스트 9개 작성 |
| 기존 회귀 / Photon 두 클라이언트 / 재생·Frame 복원 | NOT RUN | 같은 이유로 테스트 job을 시작하지 않음 |

컴파일 콘솔에는 기존 `QuantumMenuConfig.AvailableScenes` 사용 중단 경고 3건도 있었다.
사용자 방침에 따라 실패 수정 후 검사를 재실행하지 않았다. 코드와 씬 연결은 구현했지만 게임 내 동작 검증은 완료되지 않았다.
상세 결과는 [`evidence/counter-hit-zoom-validation.json`](evidence/counter-hit-zoom-validation.json)에 남긴다.
[`evidence/counter-hit-zoom-source-hashes.json`](evidence/counter-hit-zoom-source-hashes.json)은 검증 시작 시점의 해시다.
이후 위 테스트 1개 호출을 수정했으므로 최종 코드의 통과 근거로 재사용하지 않는다.
독립 빌드/다른 플랫폼, 실제 네트워크 롤백 유도, 게임 화면의 연출 감각에 대한 수동 평가도 NOT RUN이다.
