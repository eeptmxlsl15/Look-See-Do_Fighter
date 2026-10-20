# 이펙트 불투명도와 호밍·패링 흰색 변화 — 2026-10-20

기준: `dev/Prototype`, HEAD `98143dd36e0c0fc4f24490a7625d7fec80594d21`.
요청은 히트 이펙트의 투명도 조절과 호밍 기술/패링 발동 때 빠르고 부드러운 약한 흰색 변화다.
[이전 이펙트 작업](hit-effects-prefab-migration.md)의 컴파일 실패 후 미검증 기록은 과거 상태로 보존한다.

## 사용자 설정

- `LSDFGameplay` 씬 → `HitEffectManager` → `LSDF_HitEffectView`:
  `HitOpacity`, `GuardOpacity`, `ParringOpacity`, `CounterOpacity`를 0–1로 조절한다.
  0은 숨김, 1은 프리팹의 원래 알파다. 기본값 1로 기존 모습을 유지한다.
  각 스프라이트의 저장된 알파에 곱하며 RGB와 원본 프리팹을 변경하지 않는다.
  생성 시 자식 SpriteRenderer도 수집하고 재생 중 설정 변경을 반영한다. 반복 적용으로 계속 투명해지지 않는다.
- `LSDFGameplay` 씬 → `QuantumEntityViewUpdater` → `LSDFSceneBindings`:
  `ActionFlashStrength` 기본 0.22, `ActionFlashFadeIn` 0.035초, `ActionFlashFadeOut` 0.12초.
  강도 0으로 흰색 표시를 끈다. `ActionFlashMaterial`은 이번 공유 머티리얼에 연결했다.
- 사용자가 만든 `Red`/`White`/`DarkRed`와 경기 씬의 히트 Red / 가드 White / 패링 Red / 카운터 DarkRed 연결을 유지한다.
  이전 `1010_0.prefab`은 현재 Red로 이름이 바뀌었지만 GUID `f33e5e4393db624449efc9e078701a88`이 유지돼 있다.

## 구현과 경계

- [효과 View](../../Assets/QuantumUser/View/LSDF_HitEffectView.cs)는 종류별 불투명도를 스프라이트 인스턴스에 적용한다.
  이벤트 Game 범위·단일 소유권·중복/취소/확정·한 주기 수명은 유지했다. 현재 사용 중인 세 프리팹은 스프라이트 애니메이션이다.
- [캐릭터 View](../../Assets/QuantumUser/View/LSDF_ViewHandler.cs)가 PredictedFrame의 현재 기술을 읽고
  [LSDFActionFlash](../../Assets/QuantumUser/View/LSDFActionFlash.cs)의 표시 상태만 갱신한다.
  전환 중에는 ToState를 우선한다. QTN·시뮬레이션 이벤트나 판정 코드를 추가하지 않는다.
- 호밍은 실제 공격 창의 `LSDFSingleHitDefinition.ParryReaction == HomingType.Homing`으로 분류한다.
  다단 기술의 `LSDFTimedHitDefinition.Hit`도 읽는다. 기술 이름 목록을 별도 작성하지 않고,
  graph/state별 불변 메타데이터 분류를 View에 캐시해 매 프레임 reflection을 하지 않는다.
  다단 중 한 타가 호밍이면 그 기술 시작에 한 번 표시한다.
- 패링은 `ParringStart` 진입에 표시하고 `Parring`으로 이어질 때 중복 발동하지 않는다.
  패링 성공 때의 접촉 이펙트와 발동 표시를 구분한다.
- SmoothStep으로 밝아졌다가 원래 모습으로 돌아간다. 일반 상태/피격 중단·회복·라운드 종료·View 비활성화·게임 변경 시 정리하고,
  되감긴 예측 프레임에서는 반환된 상태 시간으로 재표본화해 긴 새 섬광을 재시작하지 않는다.
- 어두운 픽셀도 실제로 밝아지도록,
  알파 윤곽을 사용하는 [URP 흰색 오버레이 셰이더](../../Assets/QuantumUser/View/Rendering/LSDFWhiteFlash.shader)를 사용한다.
  원래 캐릭터 색/머티리얼은 그대로 두며 표시가 끝나면 오버레이를 숨긴다.
  캐릭터당 오버레이 렌더러 최대 하나를 재사용하고 두 캐릭터는 [공유 머티리얼](../../Assets/QuantumUser/View/Rendering/LSDFWhiteFlash.mat)을 참조한다.
  강도는 MaterialPropertyBlock으로 격리하며 스프라이트/방향/정렬을 따라간다. 독립 빌드/모바일 외부 알파 검증은 범위 밖이다.
- 참조용 내장 UnitySprites.cginc 읽기 MCP 호출은 설치 경로 없음(DirectoryNotFoundException)으로 실패했다.
  그 파일을 사용하지 않고 설치 URP Core.hlsl을 사용하는 프로젝트 셰이더를 작성했다. SDK/패키지는 수정하지 않았다.

Simulation/QTN/Generated/시스템 순서/프로토콜/피해·이동·패링 규칙은 변경하지 않았다.
이번 작업은 View 표현이며 새 결정론/플랫폼 간 체크섬 증명을 주장하지 않는다.

## 최종 한 차례 검증

구현과 테스트를 마친 뒤 Unity assets/셰이더 임포트 및 컴파일, 콘솔 1회,
[효과 fixture](../../Assets/Tests/Editor/LSDFHitEffectTests.cs) 4개와
[흰색 표시 fixture](../../Assets/Tests/Editor/LSDFActionFlashTests.cs) 8개(총 12개),
정적 정책, diff 공백, 관련 문서/메타를 한 묶음으로 각각 한 번 확인한다.
실패하면 범위 안의 원인만 보정하고 자동 재실행하지 않는다.

회귀 범위는 종류별 알파/RGB·원본 보존/누적 방지, 실제 dispatcher와 Game 격리·취소/확정·재활성화,
두 실제 graph의 호밍·패링 분류, 반복 프레임/중단/되감기/패링 연속 구간,
캐릭터별 독립 강도·공유 머티리얼·방향·복원, 셰이더 오류 및 RenderTexture 픽셀의 흰색/투명 윤곽이다.
테스트가 직접 생성한 프레임/이벤트 경계와 실제 온라인 입력·물리 접촉을 통한 플레이를 구분한다.

이전 fixture의 고정 파일명/네 슬롯 동일 프리팹 가정을 현재 사용자 에셋과 맞췄다.
실제 updater의 맵 View 동기화에 필요한 경기 씬을 테스트에서 명시적으로 불러오고 정리한다.
기존 수명 검사는 시간 고정값 대신 애니메이션 normalizedTime으로 한 주기 경계를 확인하도록 변경했다.

## 최종 한 차례 결과

근거: [검증 JSON](evidence/effect-opacity-action-flash-validation.json).
job `10870868b13b4a25852c662c4e5a4a54`, **12개 중 10 PASS / 2 FAIL / 0 SKIP**다. 자동 재실행하지 않았다.

| 검사 | 결과 |
|---|---|
| Unity 컴파일/콘솔 | PASS — 보정 전 오류 0, 경고 8. 테스트 중 발견한 초기화 보정 후 컴파일은 미검증 |
| 흰색 표시 8개 | PASS — 펄스/반복/중단·되감기/패링 연속, 캐릭터별 강도·원본 보존, 씬 참조, 셰이더 및 GPU 투명 윤곽 픽셀 |
| 효과 fixture 4개 | 2 PASS / 2 FAIL. 경기/메뉴 연결은 PASS. 통합 Frame 할당과 고정 크기 가정 실패는 아래 보정 참조 |
| 실제 두 graph 호밍/패링 분류 | 통합 fixture에서 해당 assertion을 마친 뒤 Frame 할당에 실패. 별도 통과 테스트 수로 더하지 않음 |
| 알파 이벤트/실제 Frame 관측 통합 | NOT RUN — 할당 실패가 이후 이벤트 생성·알파/취소·재활성화 검증보다 먼저 발생 |
| 정적 정책 | PASS — Simulation 94개, 기존/신규 적중 0. 의미적 결정론 검증을 대신하지 않음 |
| diff 공백 | FAIL — 씬의 빈 필드 줄 끝 공백 5곳. 해당 공백만 보정 후 미검증 |
| 문서/메타/새 파일 공백 | PASS — 보정 전 상대 링크 30개, 메타 짝 9개 |
| 실제 온라인 대전/육안·강제 네트워크 rollback | NOT RUN |

실패 후 다음을 보정했으며 컴파일/테스트/공백 검사는 다시 실행하지 않았다.

- 테스트 출력에서 `LSDF_ViewHandler`의 필드 초기화가 `MaterialPropertyBlock.CreateImpl`을 호출한다는 UnityException을 확인했다.
  실제 런타임 초기화 문제라서 helper의 네이티브 블록 생성을 `Apply()`의 메인 스레드 경로로 늦췄다.
  이 **런타임 코드 보정 후 미검증**이며 앞선 흰색 단위/픽셀 PASS를 최종 보정 코드의 재검증으로 재사용하지 않는다.
- 통합 fixture의 독립 Frame에 필요한 heap이 부족해 `All 4 heaps are already reserved`가 발생했다.
  테스트 runner 인자의 `HeapExtraCount = 1`로 한 개를 예약하도록 보정했다. 제품 SimulationConfig/SessionConfig는 바꾸지 않았다.
  Frame/알파 assertion을 제거하거나 현재 VerifiedFrame을 직접 수정하는 우회는 하지 않았다.
- 프리팹 크기가 반드시 1이라는 이전 assertion은 사용자 설정 0.7과 충돌했다.
  유효한 원본 크기를 허용하도록 바꾸고, 인스턴스 크기가 원본과 같다는 통합 assertion은 유지했다.
  사용자 프리팹 크기/색/속도는 변경하지 않았다.
- `LSDFGameplay.unity`의 보고된 빈 YAML 필드 공백 5곳만 제거했다. 참조/값은 바꾸지 않았다.

보정 전 소스 해시·실패 원문과 보정 후 소스 해시는 JSON에 남겼다.
Unity 자동 임포트/컴파일 가능성과 별개로 후속 결과를 조회하거나 PASS로 기록하지 않았다.
이 최종 결과 문서/요약/증거 추가도 자동 미검증이다. 과거 작업 결과는 덮어쓰지 않는다.

기존 IDE 생성 파일/미추적 csproj 4개, 메뉴/경기 씬의 사용자 변경과 세 프리팹·애니메이션,
선행 문서/근거를 보존한다. 새 소스/렌더링 에셋·메타/테스트/문서·증거는 미추적이며 staging/커밋/게시하지 않는다.
