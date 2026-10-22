# 상대 KazumiHoming 표시·공중 콤보 피해 누락 조사

조사 시각: 2026-10-22T12:39:41+09:00. 기준 HEAD `97b6acb020a672193d44053edc955d109e8759b7`, 브랜치 `dev/Prototype`.
사용자의 원인 질문에 대한 조사다. 게임 코드·에셋·씬은 수정하지 않았다.
[읽기 전용 관측 근거](evidence/homing-air-damage-investigation-2026-10-22.json)에 실제 Unity 반환값을 기록했다.

## 상대 기술이 보이지 않는 원인

- [EnemyAnimator](../../Assets/Sprites/Character/Enemy/EnemyAnimator.controller)의 `0_KazumiHoming` 상태 Motion은
  삭제된 `Rk_Enemy/KazumiHoming_Enemy/KazumiHoming_Enemy.anim`의 GUID `f6d404bbc077a6047b1bf4ba5172e860`을 가리킨다.
- [Enemy Quantum 그래프](../../Assets/QuantumUser/Animators/LSDF_EnemyAnimatorGraph.asset)의 Clips 목록 84번(0 기준)도 같은 참조다.
  Unity에서 해당 Motion과 Clips[84]가 모두 null임을 확인했다. AssetDatabase가 옛 경로를 반환하더라도 실제 클립은 로드되지 않는다.
- 남아 있는 `Rp_Enemy/KazumiHoming_Enemy/KazumiHoming_Enemy.anim`의 GUID는 `765337c05fdbc284bb3d5b81e3d4fe2a`다.
  같은 파일명이어도 GUID가 달라 기존 참조에 자동으로 연결되지 않는다. 이 클립의 Sprite key 35개는 모두 유효하다.
- [AnimatorPlayables](../../Assets/Photon/QuantumAddons/Animator/View/AnimatorPlayables.cs)는 Frame의 그래프 Clips를 읽어 표시한다.
  Clips 로더가 null 방어 없이 `.name`을 읽으므로 상대 View 초기화에도 영향을 줄 수 있다. 실제 플레이에서의 예외 발생은 재현하지 않았다.

수정 방향은 삭제된 사용자 에셋을 임의로 복원하기보다 사용할 클립과 Controller/Quantum 그래프 참조를 함께 맞추는 것이다.
현재 새 클립은 12 FPS·약 2.9167초, 기존 베이크 기술은 60 FPS·35프레임·약 0.5833초다.
기존 판정 타이밍을 유지하려면 새 시각 클립의 시간 규칙을 함께 확인해야 한다. 전체 그래프 재베이크는 이번 조사에서 수행하지 않았다.

## 공중 콤보에서 피해가 빠지는 원인

[피해 함수](../../Assets/QuantumUser/Simulation/LSDFPlayerHealth.cs)의 `NormalHitDamage`는 다음과 같다.

| LSDF_Player.hitCount | 기본 피해 20일 때 실제 반환값 |
|---|---:|
| 0 | 20 |
| 1 | **0** |
| 2 | 15 |
| 3 | **0** |
| 4 | 10 |
| 5 이상 | 6 |

1·3 분기가 비어 있어 마지막 `return 0`으로 내려간다. 기존 동작 보존 주석과
[체력 테스트](../../Assets/Tests/Editor/LSDFPlayerHealthTests.cs)도 이 0 피해를 정답으로 고정하고 있어 과거 PASS가 버그 부재를 뜻하지 않는다.
컴파일된 실제 함수에 `hitCount=1`, HP 170을 주고 기본 피해 20을 네 번 적용했을 때 HP는 매번 170이었다.

카운트 연결도 어긋나 있다.

- [Juggle.RegisterHit](../../Assets/QuantumUser/Simulation/LSDFJuggle.cs)는 실제 후속타 수를 Frame의 `LSDF_AirCombo.HitCount`에 누적한다.
- [Combat](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs)은 HP 계산 때 별도 필드 `LSDF_Player.hitCount`를 읽는다.
- 이 필드는 [AirWindowEvent.OnEnter](../../Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Move/AirWindowEvent.cs)에서만 증가한다.
  [HitReactions.EnterAir](../../Assets/QuantumUser/Simulation/LSDFHitReactions.cs)는 이미 Air이면 진입 이벤트를 재실행하지 않는다.
  따라서 첫 Air 진입 뒤 값이 1에 머문 채 여러 공중 후속타의 피해가 0이 되는 경로가 있다.
- [최근 공중 중단 수정](air-hit-interruption-fix.md)은 재피격 시 Air 재진입을 생략하도록 했으며,
  해당 fixture는 `hitCount` 불변을 확인하지만 실제 피해 계산·접촉 dispatch를 검증하지 않았다고 명시한다.

수정 시 실제 접촉 타수와 피해 보정의 기준을 통일하고 모든 유효 타수에 피해 배율을 정의해야 한다.
1·3 분기만 채워도 타격별 보정 단계가 정상 증가하는 문제까지 해결되는 것은 아니다.
시동기 포함 여부, 후속타 5회 제한, 마무리·벽·착지 초기화와 카운터 피해 규칙을 함께 보존해야 한다.
사용자의 정확한 3~4번째 타격에서 어떤 필드 값이었는지는 입력·프레임을 재현하지 않아 미확정이다.

## 최종 한 차례 관측과 범위

| 항목 | 결과 |
|---|---|
| Unity 참조 관측 | PASS — 옛 clip 로드 불가, 상태 Motion/graph Clips[84] null, 현재 클립 Sprite 35/35 유효 |
| 실제 피해 함수 관측 | PASS — count 0~6 반환값 및 count=1에서 네 번 피해 누락 확인. 결함 재현의 성공이며 게임 정상 판정 아님 |
| 콘솔 조회 | 오류 0 / 기존 Input Manager 경고 1. 강제 컴파일은 수행하지 않음 |
| 실제 대전 3~4타 재현·전체 replay·강제 rollback | NOT RUN |
| 정적 정책·CodeGen | NOT RUN — 게임 코드/QTN 변경 없음 |
| 문서 링크·JSON·변경 공백 | PASS — 상대 링크 48개 누락 0, JSON 파싱, 문서 끝 공백 0, current-state의 git diff --check 종료 0 |

기존 IDE 수정 9개·미추적 csproj 4개, `Rk_Enemy/KazumiHoming_Enemy` 폴더의 추적 파일 삭제 75개와
기존 current-state 게시 결과 변경을 보존했다. 새 조사 문서/근거 JSON은 미추적이며 staging·커밋·게시하지 않는다.
과거 FAIL/NOT RUN/수정 후 미검증 상태는 변경하지 않는다.
