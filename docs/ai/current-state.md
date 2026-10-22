# 현재 작업 상태

갱신: 2026-10-22T17:11:00+09:00. 브랜치: `dev/Prototype`, 현재 HEAD `97b6acb020a672193d44053edc955d109e8759b7` (`Add flat stage background and inspector camera height`).
현재 HEAD와 이 문서를 포함한 로컬 커밋은 `git log -1`로 확인한다.
이 요약을 읽은 뒤 요청에 관련된 아래 기록/증거와 실제 Git 상태·소스를 대조한다. Git 조회 실패는 `UNKNOWN`이다.
초기 감사와 과거 PASS는 해당 시점의 기록이며 현재 변경의 검증을 대신하지 않는다.

## 최근 작업

최신 요청: [공중 피해·사용자 재베이크 테스트 완료와 로컬 커밋](user-validation-air-combo-2026-10-22.md).
사용자가 현재 작업을 모두 테스트했다고 확인하고 커밋만 요청했다. 자동 게임 검사는 재실행하지 않는다.
피해 C# 3개/테스트 2개는 기존 구현·보정 후 해시와 같다. 사용자 수정 clip/Controller/양쪽 그래프와 중복 Rk 폴더 삭제 75개도 관련 후보로 포함한다.
IDE 수정 9개/미추적 csproj 4개는 보존·제외한다. 자동 FAIL/NOT RUN/수정 후 미검증 이력은 그대로 유지한다.
현재 사용자 재베이크는 Rp clip 참조와 60 FPS를 반영했으나 KazumiHoming clip/그래프 공격 이벤트는 비어 있다. 이 상태를 보존하며 후속 항목으로 기록한다.
로컬 후보 90개(수정/추가 15·기존 삭제 75)만 명시적으로 staging했다. staged 공백/문서 링크 63개/JSON 2개 PASS.
preflight는 두 그래프의 2 MiB 초과로 종료 1/BLOCKED이고 다른 발견은 없다. 두 UTF-8 YAML 전체 비밀 패턴 0/의미 diff 별도 검토를 기록해 포함한다.
사용자 직접 테스트 완료와 로컬 커밋 명시 요청을 근거로 커밋한다. 게임 검사/preflight/공백 재실행은 하지 않으며 Push/PR은 수행하지 않는다.
자세한 후보 검토와 최종 결과는 위 사용자 확인 기록에 연결한다.

최신 사용 안내: 설치 소스에서 전체 메뉴 `Tools → Quantum Animator → Bake all Graph Assets`와
[그래프 Inspector](../../Assets/Photon/QuantumAddons/Animator/View/Editor/AnimatorGraphAssetEditor.cs)의 `Bake Animator Graph` 버튼을 확인했다.
EnemyAnimator의 0_KazumiHoming Motion을 현재 Rp_Enemy 클립에 연결한 뒤 기존 LSDF_EnemyAnimatorGraph를 다시 굽는 순서를 안내한다.
현재 새 클립은 12 FPS/약 2.9167초이고 m_Events가 비어 있다. 재베이크 전 Kazumi Homing Window Event의 시작/끝 이벤트 쌍과 기존 기술 시간을 맞춰야 한다.
[설치 Baker](../../Assets/Photon/QuantumAddons/Animator/View/Editor/AnimatorGraphBaker.cs)는 Controller의 clip/events를 다시 수집한다.
이번 요청은 사용법 안내이며 실제 연결·재베이크·코드 변경·게임 재검증은 수행하지 않았다. 앞선 피해 변경과 미검증 상태는 그대로 보존한다.
이번 문서 최종 한 차례 상대 링크 41개/끝 공백 PASS. 게임 검사는 NOT RUN이며 재실행하지 않았다.

최신 구현: [공중 콤보 후속타 피해 70/50/30%](air-combo-damage-scaling.md),
[검증 근거](evidence/air-combo-damage-scaling-validation.json).
시동기 제외 실제 LSDF_AirCombo.HitCount로 일반/카운터 피해를 계산하도록 연결했고 1타 70%/2타 50%/3~5타 30%를 정수 버림으로 적용한다.
지상 카운터 120%·시동기 100%/5후속타 제한·궤적·시스템 순서는 유지한다. 매칭 프로토콜은 v14-air-combo-damage다.
제품 C# 3개와 기존 테스트 2개를 수정했다. QTN/Generated/SDK/에셋 수정 없음.
최종 한 차례 정적 94개/신규 0, Unity 컴파일 오류 0/기존 경고 5개, 체력·공중·마무리·라운드 109개, 문서 링크 46개/JSON/공백 PASS.
전체 job 110개 중 109 PASS/1 FAIL/0 SKIP. Frame/그래프 통합 1개 안의 내부 46개 사례가 구 entry count 예상 1/관측 2에서 실패했다.
Air 이벤트 RawValue 1092와 SDK의 LastTime <= Time 경계가 관련됨을 소스로 확인했다. 피해는 해당 구 카운트와 분리되어 있다.
실패 뒤 fixture만 시간 보존/진행·구 카운트 1/3과 피해 독립성 검사로 보정했고 **수정 후 미검증**이다. 제품 코드는 검사 후 변경하지 않았다.
신규 5타 통합 완료/전체 replay/강제 온라인 rollback은 NOT RUN. 검사를 재실행하지 않았고 과거 FAIL/NOT RUN 이력은 유지한다.
KazumiHoming 참조 누락·기존 IDE 9개/미추적 csproj 4개·Rk_Enemy 삭제 75개·선행 조사 문서와 요약 변경을 보존한다.
새 구현 문서/검증 JSON은 미추적이며 staging/커밋/게시하지 않는다. 이전 자동 실패/미실행 상태도 유지한다.

최신 조사: [상대 KazumiHoming 표시·공중 콤보 피해 누락](homing-air-damage-investigation-2026-10-22.md),
[관측 근거](evidence/homing-air-damage-investigation-2026-10-22.json).
게임 코드/에셋/씬 수정 없음. 상대 Controller Motion과 Quantum 그래프 Clips[84]가 삭제된 Rk_Enemy 클립 GUID를 참조해 null임을 Unity에서 확인했다.
남아 있는 Rp_Enemy 클립은 다른 GUID이며 Sprite 35개 모두 유효하다. 현재 12 FPS 클립과 기존 60 FPS 베이크의 시간 차이도 후속 수정 시 고려한다.
피해 함수는 legacy hitCount 1/3에서 0을 반환한다. 실제 후속타는 LSDF_AirCombo.HitCount에 쌓이지만 피해는 LSDF_Player.hitCount를 읽으며,
최근 Air 재진입 생략으로 후자가 1에 머물 수 있다. 실제 함수에 count=1/HP 170을 주고 기본 피해 20을 네 번 적용해 HP 170 유지를 확인했다.
읽기 전용 프로브 PASS(결함 확인), 콘솔 오류 0/기존 경고 1. 강제 컴파일/실제 사용자 3~4타 재현/replay/rollback은 NOT RUN이다.
최종 한 차례 문서 상대 링크 48개/JSON 파싱/끝 공백/current-state diff 공백 PASS. 결과만 기록하고 재검사하지 않았다.
기존 IDE 9개·미추적 csproj 4개·Rk_Enemy의 KazumiHoming 폴더 추적 파일 삭제 75개와 기존 게시 결과 요약을 보존한다.
새 조사 문서/JSON은 미추적이며 current-state는 기존 변경 위에 추가했다. staging/커밋/게시 없음. 이전 자동 검증 이력은 유지한다.

최신 게시 결과: [배경·카메라 사용자 테스트 완료와 푸시 승인](user-validation-stage-camera-2026-10-22.md)을 근거로
`97b6acb020a672193d44053edc955d109e8759b7`을 기존 `origin/dev/Prototype`에 푸시 완료했다.
`git ls-remote`가 같은 SHA를 반환했고 원격과 0/0이다. 검토 tree와 커밋 tree `b2c07176f1b603c92845ac9bb0332c3dc2eb1a1b`가 일치한다.
176개 경로 변경을 포함했고 PR은 만들지 않았다. 게시 후 이 결과 요약만 로컬에 갱신한다.
사용자가 지금까지의 작업을 모두 테스트했다고 확인하고 푸시를 요청했다. 게임 자동 검사는 재실행하지 않는다.
배경/평면 줌/높이 설정과 사용자 씬·Map·메뉴, 배경 PNG 및 새 적 동작 PNG/clip/controller를 게시 후보로 포함한다.
IDE 생성 파일은 보존·제외한다. 이전 자동 FAIL/NOT RUN/수정 후 미검증은 유지한다.
게시 준비에서 씬이 실제로 없는 `_export.png`를 가리키는 것을 발견해 남아 있는 원본 PNG로 참조 3곳만 복구했다.
정적 참조 확인 3곳과 PNG 73개의 구조/메타 짝은 PASS, 복구 후 실제 게임은 NOT RUN이다. 사용자 높이 0.09와 배경 위치·배율은 보존한다.
origin/권한/fetch 확인 완료, 게시 전 작업 브랜치와 0/0, 열린 PR 없음. 사용자의 명시적 승인에 따라 푸시했다.
새 사용자 확인 기록과 에셋 근거 JSON을 이번 게시 후보에 명시적으로 포함했다.
게시 최종 한 차례 정적 정책 94개/신규 0, staged 공백, 문서 링크 51개·새 에셋 메타 짝 76개/동작 참조 PASS.
preflight는 staged 항목 174개/blob 173개/미게시 커밋 0개를 검사해 PNG의 LARGE 1개/BINARY 72개로 종료 1(BLOCKED)이다.
텍스트 비밀 패턴/불필요 경로 적중 없음. 해당 73개는 별도 구조·메타·표본 육안 검토를 기록한 사용자 게임 에셋으로 포함하며 검사기 자체를 PASS로 바꾸지 않는다.
게임 검사와 preflight를 재실행하지 않았다. 결과 문서를 수동 검토해 포함했고 검증 후 제품 코드/에셋 추가 변경은 없었다.
기존 IDE 수정 9개와 미추적 csproj 4개는 로컬에 그대로 남겼다. 이 게시 결과 요약 변경도 미커밋이다.

최신 구현: [카메라 높이 인스펙터 설정·검증](camera-height-inspector.md).
`LSDFGameplay → EntityViewUpdater → LSDFSceneBindings → Camera Follow → Camera Height`로 양쪽 카메라의 월드 Y를 조절한다.
기본 0 유지, 양수면 카메라가 올라가며 기존 보간/거리 줌/피격 연출을 거친다. 제품 수정은 해당 View 필드와 목표 Y뿐이다.
최종 한 차례 컴파일/콘솔(오류·경고 0), 기존 카메라 회귀 8개, 정적 94개/신규 0, 문서 링크 34개는 PASS.
Inspector 기본 0 및 0.1/-0.1/0.2 적용·JSON 왕복은 통과했으나 뒤이은 프리뷰 카메라 참조 확인에서 프로브 FAIL.
전체 컴포넌트 JSON 덮어쓰기 전 참조를 따로 관측하지 않아 원인은 미확정이며 원본 씬에는 두 참조가 있다. 실제 게임 구도는 NOT RUN.
공백 검사는 후속 Git 조회 실패와 합쳐져 개별 종료 코드 미확보로 UNKNOWN. 정상 권한으로 diff 읽기만 수행했고 검사 재실행은 하지 않았다.
사용자 씬/Map은 선행 기록 이후 변경된 시작 해시와 최종 해시가 일치한다. 이번 작업에서 씬·Map·이미지·배경 소스/테스트를 저장/수정하지 않았다.
기존 IDE/씬/Map/PNG 추가·삭제/배경 코드·테스트·문서와 이전 실패 이력을 보존한다. 새 카메라 기록은 미추적, View/요약은 미커밋이다.
커밋·푸시 없음. 이전 배경 원근 2개 보정 후 미검증 상태도 유지한다.

최신 구현: [평면 배경 줌 동기화](stage-background-flat-zoom.md), [검증 근거](evidence/stage-background-flat-zoom-validation.json).
매 프레임 화면 채우기로 배경 축소가 상쇄되는 문제와
캐릭터/배경 깊이 차이를 수정했다. 초기 거리 0.8의 구도는 유지하고 배경이 캐릭터 평면과 같은 화면 좌표/줌 비율을 갖도록 투영한다.
사용자가 PNG를 연결하고 Map/씬을 수정한 상태를 확인했다. 두 파일·이미지·설정 값은 보존하며 View/Inspector/관련 fixture만 수정한다.
최종 한 차례 컴파일/콘솔(오류·경고 0), 정적 94개/신규 0, 관련 공백/문서 링크 41개·메타 짝 3개는 PASS다.
13개 중 11 PASS / 2 FAIL / 0 SKIP이다. 직교 줌 양쪽/기준 비율/반전 GPU/수명·씬 연결은 PASS.
원근 두 검사는 실제 그림이 아닌 두께 0.2 경계 박스의 앞뒤 Z 면을 비교한 측정점 문제를 보정했고 **수정 후 미검증**이다.
제품 View/Inspector는 검사 후 변경하지 않았다. 테스트/검사를 다시 실행하지 않았다. 실제 PNG 게임 화면·Photon/rollback은 NOT RUN이다.
씬과 사용자 Map의 최종 해시가 시작 해시와 일치해 이미지 연결·설정 보존을 확인했다.
현재 새 View/Inspector/테스트와 메타·배경 문서/두 증거는 미추적이다. 기존 IDE/Map/씬·PNG 삭제/추가와 이전 기록을 보존하고 커밋·푸시하지 않았다.
Unity 연결은 회복되어 메뉴 씬 편집 모드에서 검사를 수행했다. 이전 준비 TIMEOUT/NOT RUN 이력은 아래에 유지한다.

최신 구현: [양쪽 카메라용 긴 도트 배경](stage-background.md), [검증 근거](evidence/stage-background-validation.json).
사용자가 PNG를 직접 연결하도록 게임 씬의 `StageBackground → Background Sprite` 슬롯을 비워 두고
두 뷰의 배경 배치/비율 유지·화면 채우기/2P 반전·레이어 격리를 구현했다. 기존 카메라 연출·Simulation은 변경하지 않았다.
현재 소스와 Git을 대조해 ee3628ba 이후 사용자 에셋 커밋 debf379c를 확인했다. 배경 두 PNG/메타 삭제와 새 사용자 PNG/메타 추가,
기존 IDE 변경 및 이전 요약 변경을 보존한다. 새 View/Editor/테스트 3개와 메타·작업 기록/증거는 미추적이며 씬/이 문서는 미커밋이다.
최종 한 차례 정적 정책 94개/신규 0, diff 공백, 문서 링크 30개/메타 짝 3개는 PASS다.
Unity refresh는 60초 준비 대기 TIMEOUT, 후속 상태/콘솔은 `no_unity_session`으로 실패했다.
컴파일 결과 확인 불가, 새 9개 fixture/에디터 씬 재로드/실제 PNG 화면은 NOT RUN이다. 테스트 job을 시작하지 않았고 재실행도 하지 않았다.
에디터와 Player 2 프로세스는 실행 중인 채 보존했다. 최종 연결 목록도 0개여서 다른 인스턴스/도구로 게임 검증을 우회하지 않았다.
디스크의 게임 씬은 연결 슬롯을 포함하며 Unity가 준비되면 다시 열어 PNG를 연결한다. 커밋/푸시는 요청받지 않았다.

최신 게시 결과: `ee3628bae82cb4044a33d3a94492ee2d4daa98ef` (Restore hit effects and add opacity and action flashes)을
기존 `origin/dev/Prototype`에 푸시 완료했다. `git ls-remote`가 같은 SHA를 반환했다. PR은 생성하지 않았다.
[사용자 확인·게시 범위](user-validation-effects-2026-10-20.md): 사용자가 테스트 완료와 재질문 없는 푸시를 명시했다.
관련 View/씬/세 프리팹·필요 의존성/테스트/문서 46개만 커밋했다. 사용자 불투명도 0.5와 프리팹 크기/색/속도를 보존했다.
검토 tree `eefafd8c5566894c3e9c81c5e357ef6f5b4cb028`와 커밋 tree가 일치했다. 정적 정책 94개/신규 0과 staged 공백 PASS, Simulation diff 없음.
preflight는 46개 파일/46개 blob/미게시 커밋 0개를 검사해 필수 PNG 1개의 BINARY_NOT_SCANNED로 종료 코드 1이었다.
알려진 텍스트 비밀 패턴/불필요 경로 적중은 없으며 PNG의 구조/스프라이트 육안 별도 검토 근거로 포함했다. 검사기 자체를 PASS로 바꾸지 않았다.
게임 자동 검사는 재실행하지 않았다. 이전 자동 FAIL/NOT RUN/수정 후 미검증과 사용자 완료 보고를 구분한다.
루트 IDE 변경 9개/미추적 csproj 4개, 사용하지 않는 스프라이트 11개/GIF 및 각각의 메타는 로컬에 보존했다.
이 게시 결과 요약은 푸시 후 로컬에만 갱신했다. 현재 이 문서 변경은 미커밋이며 새 게임 변경은 없다.

이전 게시 결과: `98143dd36e0c0fc4f24490a7625d7fec80594d21` (Return to main menu after match result)을
기존 `origin/dev/Prototype`에 푸시 완료했다. `git ls-remote`의 원격 SHA가 일치했다. PR은 생성하지 않았다.
게시 후보 8개 파일/8개 blob의 preflight는 발견 사항 0, staged 공백 검사는 PASS였으며
검토한 tree `c90356339f446f6c510135271c94249b512b387b`와 커밋 tree가 일치했다.
게임 자동 검사는 재실행하지 않았다. 기존 IDE 변경은 보존하며 이 게시 결과 요약만 푸시 후 로컬에 갱신했다.

| 작업 | 기록·근거 | 현재 알려진 상태 |
|---|---|---|
| 평면 배경 줌 동기화 (2026-10-22) | [구현·결과](stage-background-flat-zoom.md), [근거](evidence/stage-background-flat-zoom-validation.json) | 기준 거리 0.8의 구도를 유지하고 캐릭터 평면과 같은 줌/이동 비율로 투영. 사용자 씬/Map/이미지 연결 보존. 컴파일/정적/공백·문서·메타 PASS. 13개 중 11 PASS / 2 FAIL 후 테스트 측정점 보정만 수행, 원근 2개 수정 후 미검증. 재실행 없음 |
| 양쪽 카메라용 긴 도트 배경 (2026-10-21) | [연결·구현 방법](stage-background.md), [근거](evidence/stage-background-validation.json) | 단일 Sprite 슬롯과 1P/2P 배경·원본 비율/화면 채우기·반전 옵션 구현. 정적/공백/문서·메타 PASS. Unity 준비 TIMEOUT 뒤 MCP 세션 소실로 컴파일 확인·9개 표현 검사/실제 PNG 화면 NOT RUN. 이미지 슬롯 비워 둠 |
| 이펙트 사용자 테스트 완료·푸시 (2026-10-20) | [사용자 확인·게시 범위](user-validation-effects-2026-10-20.md) | 사용자 테스트 완료 보고를 근거로 46개 파일을 ee3628ba에 커밋하고 origin/dev/Prototype 푸시 완료·원격 SHA 일치. 게임 자동 검사 재실행 없음. 정적/공백 PASS, PNG 자동 미검사 1개는 별도 검토. 이전 FAIL/NOT RUN은 보존 |
| 효과 불투명도·호밍/패링 흰색 표시 (2026-10-20) | [구현 기록](effect-opacity-action-flash.md), [검증 근거](evidence/effect-opacity-action-flash-validation.json) | 네 종류 알파와 0.035초 상승/0.12초 복원 구현. 사용자 Red/White/DarkRed 선택 보존. 최초 컴파일/정책/문서·메타 PASS, 12개 중 10 PASS / 2 FAIL. 흰색 GPU·셰이더 포함 8개 PASS. 네이티브 블록 초기화·fixture heap/크기 가정·씬 공백 보정 후 **수정 후 미검증**. 알파/Frame 통합·실제 대전 NOT RUN. 재실행 없음 |
| 새 효과 프리팹·경기 씬 연결 (2026-10-20) | [구현 기록](hit-effects-prefab-migration.md), [검증 근거](evidence/hit-effects-prefab-validation.json) | 지정 폴더의 1010_0을 네 효과에 연결. 씬 View와 경기 updater 명시, 기존 메뉴 담당 비활성화. 양쪽 카메라 방향·한 주기/취소·확정 후 정리 구현. 정책/수정 전 diff·문서·메타 PASS. 컴파일은 새 테스트 Assert 충돌 후 별칭 수정으로 **수정 후 미검증**, 4개 회귀·실제 플레이 NOT RUN. 재실행 없음 |
| 히트 이펙트 미표시 원인 조사 (2026-10-20) | [조사·관측 근거](hit-effects-investigation.md) | 10/16 리팩토링에서 이벤트 구독을 Start→OnActivate로 변경했으나 수신 객체는 메뉴 루트 EffectManager에만 남아 경기 updater/Map과 미연결. 소스·씬·커밋 이력의 불일치 확인. 편집 모드 객체 조회와 콘솔 읽기만 수행, 실제 대전 재현 NOT RUN. 코드/씬 수정 없음 |
| 메인 복귀 사용자 테스트 완료·푸시 요청 (2026-10-19) | [사용자 확인·게시 범위](user-validation-match-return.md) | 사용자가 모든 테스트 완료와 푸시를 명시적으로 요청. C# 3개는 앞선 검증 해시와 일치. 자동 게임 검사 재실행 없이 관련 코드/테스트/기록만 게시 후보로 확정. origin·인증/권한·fetch 확인, 원격 작업 브랜치와 0/0, 열린 PR 없음. 대상은 origin/dev/Prototype이며 PR 생성은 요청받지 않음 |
| 최종 승부 후 메인 메뉴 자동 복귀 (2026-10-19) | [구현 기록](match-return-to-main.md), [검증 근거](evidence/match-return-validation.json) | 자신의 Runner의 Verified MatchEnd와 기존 결과 표시 기한(기본 2초)을 확인한 뒤 연결/경기 씬 정리 완료 후 메인 표시. 중복 종료 방지·재진입 초기화. 메뉴 컴파일/정적 정책/기존 라운드 19개 PASS. 새 복귀 fixture 5개는 Unity 미임포트로 **NOT RUN**, 실제 온라인 복귀/씬 unload도 NOT RUN. 재실행 없음 |
| 공중 피격 우선 전환 수정·사용자 확인 (2026-10-19) | [구현 기록](air-hit-interruption-fix.md), [검증 근거](evidence/air-hit-interruption-validation.json), [사용자 확인](user-validation-air-hit-interruption.md) | Frame Animator를 즉시 Air로 확정하고 공격 예약·소유 hitbox 정리. 사용자가 테스트 완료 후 로컬 커밋을 명시적으로 요청함. 게임 검사를 재실행하지 않고 커밋에 포함. 정적 정책 PASS, 이전 자동 컴파일 실패 후 보정·전투/복원 NOT RUN 이력 보존 |
| 카운터 공중 피격 중 공격 지속 조사 (2026-10-19) | [원인 조사](counter-air-interrupt-investigation.md) | 소스상 공통 전환 결함 경로 확인. 일반/연타 전환이 Any State Air보다 먼저 선택되면 Air 트리거가 유실될 수 있음. 자동 입력은 일반 입력 경로이며 정확한 증상 프레임 재현은 NOT RUN. 로직 수정 없음 |
| 사용자 테스트 완료·로컬 커밋 (2026-10-19) | [사용자 확인과 포함 범위](user-validation-2026-10-19.md) | 사용자가 현재 작업물을 모두 테스트했다고 확인하고 커밋을 요청함. 게임 검사를 다시 실행하지 않고 아래 누적 변경을 로컬 커밋 대상으로 확정. 이전 자동 결과는 역사적 기록으로 보존 |
| 피격 시 전체 카메라 흔들림 (2026-10-19) | [작업 기록](camera-hit-shake.md), [검증 근거](evidence/camera-hit-shake-validation.json) | View/씬 구현. 컴파일·정적 검사 PASS, 23개 중 20 PASS / 3 FAIL. 수명/줌 선택 테스트만 보정 후 미검증. 기존 줌·체력바와 동시 재생 검사는 PASS |
| 컨텍스트 재개 안내 보완 (2026-10-19) | [작업 기록](context-resume.md) | 완료. 훅/검사기 14개, 정적 정책, 문서 링크 63개, diff 공백 PASS. 새 세션 자동 호출은 NOT RUN |
| 기상 콜라이더 중심 (2026-10-18) | [구현 기록](get-up-collider-center.md), [검증 결과](evidence/get-up-collider-center-validation.json), [최종 수정 소스 목록](evidence/get-up-collider-center-postfix-sources.json) | 해당 게임 시나리오는 **수정 후 미검증**. 검사 당시 99 PASS / 1 FAIL. 실패 뒤 착지 조건·반동 처리를 보정했으며 게임 재검사를 하지 않음. 이번 View 컴파일 통과로 이 온라인 실패를 해소한 것으로 보지 않음 |
| 날리기 콜라이더 중심 (2026-10-18) | [구현 기록](ender-collider-center.md), [검증 결과](evidence/ender-collider-center-validation.json) | 검사 당시 93 PASS / 1 FAIL. 이후 기상 작업에 통합됐으며 이전 실패 근거는 보존 |
| 직전 커밋의 낙하·스프라이트 개선 | [재피격 낙하](natural-rehit-fall.md), [스프라이트 위치](air-sprite-pivot-fix.md) | HEAD까지의 기록. 후속 콜라이더 변경의 검증 근거로 재사용하지 않음 |

## 보존할 변경과 검증 이력

- 최신 요청으로 효과/캐릭터 View와 씬 바인딩·경기 씬을 수정하고, 흰색 표시 helper/셰이더/머티리얼/메타·테스트·문서를 추가했다.
  사용자 세 프리팹과 애니메이션·기존 씬/IDE/선행 기록은 보존한다. 기존 효과 fixture는 바뀐 프리팹 이름/선택과 새 알파 기능에 맞췄다.
  Simulation/QTN/SDK/판정/프로토콜 변경 없음. 새 파일·문서/증거는 미추적이며 자동 staging/커밋/게시하지 않는다.
  이번 최종 한 차례는 정책 94개 신규 0, 최초 컴파일 오류 0, 문서 링크 30/메타 짝 9 PASS, 12개 중 10 PASS / 2 FAIL이다.
  통합 fixture는 Frame heap 4개 소진으로 이후 알파/Frame 관측에 도달하지 못했고, 이전 고정 크기 1 가정은 사용자 0.7과 충돌했다.
  테스트 runner에 HeapExtraCount 1 예약/크기 계약을 보정하고, 실제 초기화 오류는 MPB 생성을 Apply로 늦춰 수정했다.
  씬 공백 5곳도 보정했다. 런타임/테스트/공백 모두 수정 후 미검증이며 재검증하지 않는다.
  `effect-opacity-action-flash.md`, `evidence/effect-opacity-action-flash-validation.json`, helper/Rendering 에셋·메타/새 테스트는 미추적이다.
- 후속 프리팹 교체 요청으로 `LSDF_HitEffectView`, 경기/메뉴 씬, 새 프리팹의 머티리얼·표시 순서를 변경하고
  `LSDFHitEffectTests.cs`/`.meta` 및 미추적 구현 기록을 추가했다. 기존 메뉴 미리보기 인스턴스와 다른 사용자 변경은 보존한다.
  Simulation/QTN/프로토콜/SDK/전투 규칙은 변경하지 않았다. 최종 한 차례 정책 94개/신규 0, 수정 전 diff·문서 링크 27개·메타 짝 5개는 PASS.
  새 테스트 Assert 별칭 누락으로 컴파일 FAIL 후 테스트만 보정했다. 컴파일/4개 표현 회귀는 수정 후 미검증/NOT RUN이며 재실행하지 않았다.
  테스트/메타·구현 기록과 `evidence/hit-effects-prefab-validation.json`은 미추적이다. 검사 후 런타임/씬/프리팹 변경은 없다.
- 2026-10-20 조사 시작 시 정상 권한 Git 읽기로 위 HEAD/브랜치를 확인했다. 기존 루트 IDE 파일 9개와 미추적 csproj 4개,
  메뉴 씬 수정, `Assets/Assets Resources` 및 `.meta`, 이전 current-state 미커밋 변경을 보존했다.
  이번에는 이 요약과 새 미추적 `hit-effects-investigation.md`만 기록한다. 게임 코드/씬 변경·staging/커밋/게시 없음.
  최종 한 차례 문서 검사 명령은 Python 인자 quoting 유실로 실행 FAIL, 링크/공백 실제 검사는 NOT RUN이며 자동 재실행하지 않았다.
  컴파일/실제 효과 생성/온라인 재현/결정론은 NOT RUN이다. 마지막 Git 상태 읽기에서 기존 변경 보존과 새 조사 문서 추가를 확인했다.
  아래 과거 자동 실패·수정 후 미검증 및 사용자 확인 상태는 바꾸지 않는다.
- 시작 Git 조회는 실행 계정의 소유권 오류로 UNKNOWN이었고, 승인된 정상 권한 읽기로 HEAD `baed7c0d`와 `dev/Prototype`을 확인했다.
  Git 신뢰 설정은 변경하지 않았다. 기존 변경은 루트 `.csproj`/`.sln` 9개 및 미추적 LSDF.Tests/LSDF.View/LSDF.View.Editor/LSDF.View.Player `.csproj` 4개였다.
  이번에는 메뉴 C# 2개, 새 `Assets/Tests/Editor/LSDFMatchReturnTests.cs`와 `.meta`, 미추적 구현 문서/evidence를 추가·수정했다.
  기존 IDE 변경은 보존하며 staging/commit/push/PR은 하지 않는다. Simulation/QTN/프로토콜은 기존 v13을 유지한다.
  새 복귀 테스트는 파일만 작성됐고 Unity에 아직 임포트되지 않았다. 이번 기존 라운드 19개 PASS를 새 기능 테스트 통과로 기록하지 않는다.
  최종 diff 공백·관련 문서 링크 22개·새 파일 공백/메타 검사는 PASS다. 이후 코드/테스트 변경 없이 결과만 기록했다.
  위 내용은 최초 구현 종료 시점이다. 이후 사용자 테스트 완료·푸시 요청에 따라 이번 기능 관련 후보만 staging/커밋·게시 준비한다.
  원격 읽기는 정상 권한 환경에서 성공했으며 source hash 일치를 확인했다. 이전 자동 NOT RUN/실패 이력은 유지한다.
- 기존 커밋 `fa955c72`는 `LSDFJuggle`/설정/이동/PlayerSystem/기상 이벤트/라운드/프로토콜,
  관련 테스트/Config, View/씬과 작업 재개 문서·훅을 포함했다. 당시 프로토콜은 `lsdf-loadout-v12-get-up-collider-center`였다.
  IDE 생성 파일은 기존 변경 그대로 작업 트리에 보존하며 커밋에서 제외했다.
- 그 커밋에는 이전에 미추적이던 View/콜라이더 테스트와 `.meta`, 작업 문서/evidence도 포함했다. Push/PR은 요청받지 않았다.
- 과거 Photon 자동 검사는 5타 종료를 관측했지만 회복 0틱·복구 False로 실패했다.
  당시 온라인 기상 완료·확정 체크섬 최종 비교·재생·Frame 복원은 **NOT RUN**이었다.
  최신 사용자 테스트 완료 확인과 구분하며 새 자동 측정이 있다고 주장하지 않는다.
- 카메라 작업 당시 정적 정책은 Simulation 93개 파일에서 신규 적중 0이었다. 위 게임 실패·미검증 상태를 해소하는 근거는 아니다.
- 카메라는 기존 카운터 줌 합성 경로와 게임 씬의 새 설정 2개를 연결했다. 기존 체력바 구현/강도와 카운터 줌 설정 에셋을 보존한다.
  테스트 보정 이후의 사용자 직접 테스트 완료 확인을 수신했다. 자동 검사 3개를 재실행한 것으로 기록하지 않는다.
- Git 소유권 오류 시 시작 훅은 안내를 유지하고 상태를 `UNKNOWN`으로 표시하도록 보완했다. Git 신뢰 설정은 자동 변경하지 않는다.
  변경된 훅의 새 세션 자동 호출/신뢰 적용 여부는 직접 명령 검사와 구분한다.
- 원인 조사는 문서/근거만 추가했으며 이후 명시적 수정 요청으로 `LSDFHitReactions`와 Combat 호출 5곳, 프로토콜 및 회귀 fixture를 추가했다.
  현재 프로토콜은 `lsdf-loadout-v13-air-hit-interruption`이다. SDK·그래프·시스템 순서·2P 자동 입력 설정과 기존 카메라/체력바는 보존했다.
  이번 정적 정책은 94개 파일에서 신규 적중 0이며 테스트 오류 보정으로 Simulation 소스는 바뀌지 않았다.
  당시 자동 게임·snapshot 복원 체크섬은 실행하지 못했다. 38개 사례는 작성된 fixture의 계획 수이고 PASS 수가 아니다.
  최종 diff 공백 및 관련 문서 링크 35개 검사는 PASS다. 오류 보정 뒤 자동 재검증은 하지 않았다.
  이후 사용자가 직접 테스트 완료와 커밋을 요청했다. 최신 사용자 확인을 근거로 수정·테스트·조사/검증 문서를 로컬 커밋에 포함한다.
  IDE 생성 파일은 작업 트리에 보존한다. 원본 자동 결과를 PASS로 덮어쓰거나 게임 검사를 재실행하지 않는다.
  GitHub 인증/권한 조회는 도구 환경에서 실패했으며 원격 연결 없이 로컬 커밋만 진행한다. Push/PR은 요청받지 않았다.

## 다음 작업

공중 피해는 실제 타수 기준 70/50/30% 구현 후 사용자 테스트 완료 확인과 로컬 커밋 요청을 받았다.
Frame 통합 fixture 보정 후 미검증이다. 별도 검증 요청 때 전 타수 HP·두 피격자·Frame 복원/새 콤보 완료를 확인한다.
KazumiHoming 참조·60 FPS는 사용자가 수정·재베이크했다. 현재 clip/그래프의 공격 이벤트는 비어 있으므로 후속 기술 판정 작업 시 시작/끝 이벤트 연결을 확인한다.
실제 대전의 정확한 기존 3~4타 프레임은 미재현이다. 다음 후보는 자동 수정·재검증·게시 승인이 아니다.

배경·카메라의 현재 작업물은 사용자 테스트 완료 보고를 근거로 97b6acb0에 커밋·푸시했고 원격 SHA를 확인했다.
누락된 내보내기 PNG 참조를 원본으로 복구한 뒤 실제 게임은 다시 실행하지 않았다. 기존 자동 실패와 구분한다.

카메라 높이: 편집 모드에서 `Camera Height`를 0.1부터 조절하고 씬을 저장한다. 실제 대전 구도는 아직 미확인이다.
프리뷰 참조 프로브 FAIL/공백 검사 UNKNOWN은 상세 기록에 보존한다. 별도 검증 요청 시 원래 참조를 건드리지 않는 방식으로 확인한다.
이 후속 항목은 자동 재검증·게시 승인이 아니다.

평면 배경: 현재 연결한 이미지와 설정으로 카메라가 물러나면 맵도 같이 축소하도록 구현했다. `Fit Reference View`는 초기 구도만 맞춘다.
원근 1P/2P 자동 좌표 검사는 측정점 보정 후 미검증이다. 별도 검증 요청 때 수정된 13개 fixture와 실제 PNG 양쪽 줌 화면을 확인한다.
이미지 끝이 드러나면 Minimum Height로 기준 크기를 늘릴 수 있다. 현재 상태는 자동 재검증/게시 승인이 아니다.

긴 도트 배경: Unity가 준비되면 `LSDFGameplay`를 다시 열고 `StageBackground → Background Sprite`에 사용자의 PNG 스프라이트를 연결한다.
Import는 Sprite/Single/Point/Compression None, 높이는 Offset Y/Minimum Height로 맞춘다. 코드/씬은 구현했지만 MCP 연결 소실로 컴파일과 9개 fixture는 NOT RUN이다.
별도 자동 검증 요청이 있을 때 연결된 에디터에서 컴파일/새 fixture·실제 양쪽 시점을 확인한다. 자동 재검증이나 게시를 진행하지 않는다.

효과 알파/호밍·패링 흰색 표시는 사용자 테스트 완료 보고를 받아 origin/dev/Prototype에 푸시 완료했다.
초기화/테스트 보정 후 자동 검사는 미실행이다. 별도 자동 증거 요청 시 컴파일/12개 fixture와 실제 대전 양쪽 시점을 확인한다.
이번에는 게임 검사를 재실행하지 않으며 기존 자동 온라인 육안/rollback NOT RUN은 보존한다.

히트 이펙트의 새 프리팹/경기 updater 연결은 구현했다. 새 테스트 Assert 별칭 보정 후 컴파일/표현 회귀는 미검증이다.
사용자 별도 검증 요청 시 컴파일과 4개 fixture·실제 대전 양쪽 시점을 확인한다. 이번에는 재실행하지 않았고
실제 온라인 플레이·육안 확인과 표현 경계 자동 검사 범위를 구분해 기록했다.

최종 승부 복귀는 사용자 테스트 완료 보고를 근거로 origin/dev/Prototype에 푸시 완료했다. PR 생성은 범위 밖이다.
추후 자동 상세 증거가 별도로 필요하면 새 검증 요청에 따라 복귀 테스트 파일을 assets refresh로 임포트하고
컴파일/5개 fixture와 실제 Photon 복귀 경로를 확인한다. 이번 게시 준비에서 게임 검사를 재실행하지 않는다.

카메라 후속 요청 시 실제 플레이에서 강도·시간을 조절할 수 있다. 사용자 확인 뒤에도 자동 검사 상세 증거가 필요하면 별도 검증 요청에 따른다.
공중 피격 수정은 사용자 테스트 완료 확인을 받았다. 자동 검사 상세 근거가 별도로 필요하면 새 검증 요청에 따라 수정된 fixture와 관련 회귀를 실행한다.
2P 자동 입력은 그대로이며 전체 replay·강제 네트워크 rollback의 자동 측정 근거는 아직 없다.

1. 다음 세션에서 변경된 시작 안내가 전달되는지 확인한다. 앱이 훅 신뢰 검토를 요구하면 `Hook.md`의 절차를 따른다.
   안내가 없어도 AGENTS의 읽기 순서로 복원한다. 이번에 통과한 검사를 추가 확신을 위해 반복하지 않는다.
2. 게임 후속 요청 시 과거 검사 기록·최종 수정 소스와 최신 사용자 확인을 함께 읽는다.
   컨텍스트 초기화만으로 기존 테스트를 다시 실행하지 않는다.
3. 게시 요청 시 실제 커밋/원격 차이와 필수 근거를 확인하고 명시적 Push/PR 승인을 따른다. 로컬 커밋 요청은 원격 게시 승인이 아니다.

다음 작업 목록은 실행 승인이 아니다. 작업 종료·인계 시 관련 상태와 시각을 갱신하고 다른 작업의 미해결 항목은 유지한다.
