# 히트 이펙트 미표시 원인 조사 — 2026-10-20

기준: `dev/Prototype`, HEAD `98143dd36e0c0fc4f24490a7625d7fec80594d21`.
요청은 갑자기 보이지 않는 히트 이펙트의 원인 조사다. 게임 코드·씬·에셋은 수정하지 않았다.

## 소스에서 확인한 결함

이벤트 수신 코드의 활성화 조건과 실제 씬 배치가 맞지 않는다.

1. [피격 처리](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs)의 일반 피격 경로에는
   `f.Events.OnHitEffect(hitbox->position)` 호출이 유지돼 있다.
2. [이펙트 View](../../Assets/QuantumUser/View/LSDF_HitEffectView.cs)는 `OnActivate(Frame)` 안에서만
   일반 피격·가드·카운터·패링 이벤트를 구독한다. `OnEnable()`도 `IsActive`와 `PredictedFrame`을 요구한다.
3. 이 스크립트의 GUID `9f3923d5c2162a84ab94e9ac7af440bc`를 사용하는 객체는 허용된 씬/프리팹 검색 범위에서
   [메뉴 씬](../../Assets/Photon/QuantumMenu/LSDF_Menu.unity)의 루트 `EffectManager` 한 곳뿐이다.
   `QuantumEntityView`와 `QuantumEntityPrototype`을 붙였지만 경기 updater의 하위 객체가 아니다.
4. [경기 씬](../../Assets/Scenes/LSDFGameplay.unity)의 EntityViewUpdater에는 이펙트 View가 없고,
   경기 MapEntityReferences에도 메뉴의 EffectManager가 없다. 별도 등록/활성화 코드도 찾지 못했다.
   [설치된 updater](../../Assets/Photon/Quantum/Runtime/QuantumEntityViewUpdater.cs)는 자신의 하위 View 컴포넌트나
   실제 엔티티/맵에 연결한 View를 활성화한다. 메뉴에 컴포넌트를 붙인 것만으로 이 경로에 들어가지 않는다.
5. 따라서 현재 배치에서는 구독 시작점이 실행되지 않는 경로가 있다. 수동으로 구독만 시작해도
   [씬 바인딩](../../Assets/QuantumUser/View/LSDFSceneBindings.cs)의 `For(EntityView)`에는 실제 updater가 필요하며,
   `Spawn()`은 Game 불일치나 `_bindings == null`이면 로그 없이 반환한다.

## 회귀 시점과 이전 검증의 한계

`git log -1 -p --follow -- Assets/QuantumUser/View/LSDF_HitEffectView.cs`에서
커밋 `3f45e7d1dfd79384919d04e8a3ec28df4944eb11` (2026-10-16)의 변경을 확인했다.
이전에는 Unity `Start()`에서 이벤트를 구독했으나 이 커밋에서 Quantum `OnActivate()`와 Game/바인딩 검사를 도입했다.
현재 파일 SHA256은 `08cd864449a4e43f0c2ce3672a6d204f68d1af02808b573d5b7916192b1513b8`이며,
[기존 View 소스 해시](evidence/counter-hit-zoom-source-hashes.json)의 값과 같다.
최근 카메라/공중 피격/메인 복귀 작업에서 이 스크립트가 다시 변경된 것은 아니다.

[리팩토링 기록](refactor-remaining-result.md)의 구조·ledger 검사와 컴파일 PASS는 실제 VFX 생성을 증명하지 않는다.
해당 기록에도 사람이 보는 HUD/VFX/SFX는 미검증으로 남아 있다.
현재 `GameplaySceneHasExplicitUpdaterBindings` 검사에는 이펙트 수신 객체의 연결/생성 assertion이 없다.

## 이번 관측과 검증 상태

- Unity MCP 연결 성공. 열린 씬은 메뉴, Play Mode는 꺼져 있었다. 씬 dirty는 false였다.
- 읽기 전용 객체 조회: EffectManager는 active/enabled=true, 이펙트 `IsActive=false`,
  EntityRef는 무효, EntityViewUpdater는 null이었다. **편집 모드 관측이므로 이것만으로 실행 중 미활성화를 재현했다고 주장하지 않는다.**
- 콘솔의 error/warning 조회 결과는 0건이다. 효과가 실제 생성됐다는 근거는 아니다.
- 소스/직렬화/커밋 diff의 활성화 경로 불일치는 확인했다. 실제 Photon 대전에서 이벤트 수신·파티클 생성·육안 재현은 **NOT RUN**이다.
- 제외 폴더의 파티클 프리팹/머티리얼은 읽지 않았다. 메뉴의 네 필드에 직렬화 참조가 있다는 사실만 확인했다.
- 최종 한 차례 문서 링크/공백 검사 명령은 **실행 FAIL / 실제 검사 NOT RUN**이다.
  PowerShell에서 Python `-c` 인자의 따옴표가 유실돼 `SyntaxError`가 발생했고 문서 검사를 시작하지 못했다.
  자동 재실행하지 않았다. 결과를 기록한 이 문서 수정도 자동 미검증이다.
- 최종 Git 상태 읽기에서 게임 코드/씬에 이번 조사 변경이 없고 새 조사 문서만 추가됐음을 확인했다.
  코드 변경이 없어 CodeGen·컴파일·전투/결정론·정적 정책 검사는 실행하지 않았다.

## 수정 방향

전투 전체 효과를 담당하는 씬 컴포넌트로 수명을 맞추고 경기 EntityViewUpdater/LSDFSceneBindings에 명시적으로 연결한다.
현재 프리팹 참조 4개, Game별 단일 소유권, 예측 이벤트 중복 억제·취소/확정 및 종료 시 정리를 보존해야 한다.
현재 클래스는 `EntityView`에 의존하므로 객체를 옮기기만 하는 것으로는 충분하지 않다.
수정 시 양쪽 시점의 네 효과, 종료/재진입, 이벤트 취소와 중복 생성을 최종 한 차례에 확인한다.
이번에는 조사만 수행했고 커밋·게시·게임 설정 변경은 하지 않았다.

기존 IDE 생성 파일, 메뉴 씬 변경, `Assets/Assets Resources`와 `.meta`, 기존 current-state 변경을 보존했다.
이 조사 문서는 새 미추적 파일이며 자동 staging/커밋하지 않는다.
