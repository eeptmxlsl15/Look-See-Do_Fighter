# 히트 이펙트 프리팹 교체와 경기 씬 연결 — 2026-10-20

기준: `dev/Prototype`, HEAD `98143dd36e0c0fc4f24490a7625d7fec80594d21`.
요청은 `Assets/Assets Resources/Effect Prefabs`의 이펙트 사용이다.
[선행 조사](hit-effects-investigation.md)의 미연결 문제를 함께 수정한다.

## 적용한 동작

- 당시 지정 폴더에는 `1010_0.prefab` 하나(현재 [Red.prefab](<../../Assets/Assets Resources/Effect Prefabs/Red.prefab>))가 있어
  일반 히트·가드·카운터·패링 네 슬롯에 모두 연결했다. 종류별 다른 프리팹을 새로 만들지 않았다.
- [LSDF_HitEffectView](../../Assets/QuantumUser/View/LSDF_HitEffectView.cs)의 기존 클래스명·스크립트 GUID·네 필드명을 보존하고
  기반을 `QuantumSceneViewComponent`로 바꿨다. 실제 전투 엔티티가 없어도 지정한 updater의 활성화/종료/게임 변경을 받는다.
- [경기 씬](../../Assets/Scenes/LSDFGameplay.unity)에 루트 `HitEffectManager`를 추가하고 해당 씬 EntityViewUpdater를 명시했다.
  updater의 초기 하위 컴포넌트 탐색과 중복 등록되지 않도록 독립 루트로 둔다. 런타임 이름 검색/Default Runner를 사용하지 않는다.
- [메뉴 씬](../../Assets/Photon/QuantumMenu/LSDF_Menu.unity)의 기존 EffectManager도 네 참조를 새 프리팹으로 바꾸고 효과 컴포넌트만 비활성화했다.
  사용자가 이미 둔 `1010_0` 미리보기 인스턴스와 기존 메뉴/프로토타입/EntityView 및 다른 로컬 변경은 보존했다.
- 새 에셋은 SpriteRenderer + Unity Animator다. 파티클 전용 90도 회전과 강제 축소/0.1초 삭제를 제거했다.
  프리팹 크기(기본 1배)를 유지하고 경기 씬에서 활성인 1P/2P 카메라를 향하게 한다.
- 프리팹은 설치된 URP `Sprite-Unlit-Default` 머티리얼과 sortingOrder 10을 사용한다.
  캐릭터의 sortingOrder 0/1보다 앞에 표시하고 조명에 따라 효과가 어두워지지 않게 한다.
  에셋 GUID, 스프라이트 시트, Animator controller/clip 및 원래 재생 속도 8은 유지했다.
- Animator의 첫 주기(normalizedTime 1)를 마치면 제거한다. 현재 1초 clip/속도 8이므로 약 0.125초다.
  애니메이션 없는 프리팹은 `FallbackLifetime` 기본 0.15초, 끝나지 않는 애니메이션은 `MaximumLifetime` 기본 2초로 정리한다.
- 같은 Game의 단일 담당·이벤트 키 중복 억제·예측 취소·확정 처리를 보존했다.
  확정 후 ledger에서 빠진 효과도 별도 재생 목록에 남겨 비활성화/게임 변경 때 정리한다.
- Simulation/QTN/Generated/SDK/시스템 순서/프로토콜/판정/카메라 줌·흔들림 수치는 변경하지 않았다.

## 최종 한 차례 검증 계획

[검증 지침](validation.md)과 `lsdf-quantum-verify`를 같은 최종 묶음으로 적용한다.
구현/테스트/문서를 마친 뒤 assets 임포트와 Unity 컴파일, 콘솔 1회, 새 fixture 4개,
Quantum 정책 검사, 변경 diff 공백 및 문서/메타 확인을 각각 한 번 수행한다.
컴파일/테스트 실패 시 자동 재실행하지 않는다.

[LSDFHitEffectTests](../../Assets/Tests/Editor/LSDFHitEffectTests.cs)는 다음을 확인하도록 작성했다.

- 실제 경기/메뉴 씬의 새 프리팹 참조 및 한 명의 활성 씬 담당.
- Sprite/Animator/머티리얼과 캐릭터보다 높은 표시 순서.
- 로컬 Quantum runner 두 개와 실제 updater/이벤트 dispatcher를 사용한 활성화, 네 효과 생성,
  Game 격리, 중복 억제, 양쪽 카메라 방향, 취소/확정, 단일 담당, 재활성화, 한 주기 종료 및 게임 변경 정리.

마지막 항목은 테스트가 이벤트를 직접 주입하는 표현 경계 검사다. 실제 물리 접촉으로 이벤트가 발생하는 온라인 매치,
화면 픽셀/육안 평가 및 강제 네트워크 rollback의 증명은 아니다. 해당 검사는 실행하지 않으면 NOT RUN으로 남긴다.
이전 카메라·공중 피격·기상 작업의 실패/미검증/사용자 확인 이력은 이번 결과로 바꾸지 않는다.

## 보존/게시 범위

기존 IDE 생성 파일, 미추적 csproj 4개, 메뉴 씬의 기존 수정과 미리보기 인스턴스,
`Assets/Assets Resources` 전체 및 선행 조사 문서/current-state 변경을 보존한다.
이번에는 대상 프리팹, 두 씬, 효과 View, 새 테스트와 `.meta`, 이 문서/증거와 요약만 변경한다.
제외 폴더의 에셋은 검색·읽기·수정하지 않는다. staging/커밋/Push/PR은 요청받지 않았다.

## 최종 한 차례 결과

근거: [검증 JSON](evidence/hit-effects-prefab-validation.json).

| 검사 | 결과 |
|---|---|
| Quantum 정적 정책 | PASS — Simulation 94개, 기존/신규 적중 0. 표현·결정론 검증을 대신하지 않음 |
| Unity assets 임포트/컴파일·콘솔 | **FAIL → 테스트 별칭 수정 후 미검증**. 새 fixture에서 NUnit/Quantum의 Assert가 모호한 CS0104 발생. 콘솔 30건 중 해당 오류 26건·obsolete 경고 4건 관측. 전체 오류 개수는 주장하지 않음 |
| 새 표현 회귀 4개 | **NOT RUN** — 컴파일 실패로 job 시작 안 함. 계획된 테스트 수와 통과 수를 구분 |
| 변경 diff 공백 | PASS — 테스트 별칭 수정 전. LF→CRLF 안내만 있음 |
| 문서/메타/새 파일 공백 | PASS — 수정 전 상대 링크 27개, 소스·씬·프리팹 메타 짝 5개 확인 |
| 실제 온라인 피격/양쪽 화면 육안/네트워크 rollback | **NOT RUN** |

실패 후 새 테스트에 `using Assert = NUnit.Framework.Assert;`를 추가했다. 런타임 코드·프리팹·씬은 검사 후 바꾸지 않았다.
사용자 한 차례 방침에 따라 컴파일·테스트·정적/공백/문서 검사는 재실행하지 않았다.
Unity가 파일 변경을 감지할 수 있으나 후속 컴파일 결과를 조회하거나 성공으로 기록하지 않았다.
수정 전 소스 해시/콘솔 원문과 수정 후 테스트 해시를 JSON에 보존했다.
이 결과 문서와 요약/JSON의 후속 기록도 자동 미검증이다.

새 문서/테스트/증거 및 Resources 폴더는 미추적 상태다. staging/커밋/게시하지 않았다.
