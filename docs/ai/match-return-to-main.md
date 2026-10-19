# 매치 종료 후 메인 화면 복귀 — 2026-10-19

기준: `dev/Prototype`, `baed7c0d7777721bebe5762e52b4e05283dc4e39`.
요청은 승부가 결정나면 메인 화면으로 돌아가는 기능이다. 기존 3선승을 최종 승부로 해석했다.

## 동작과 구현

- [경기 메뉴](../../Assets/Photon/QuantumMenu/Runtime/QuantumMenuUIGameplay.cs)가 자신의
  `QuantumMenuConnectionBehaviourSDK.Runner.Game.Frames.Verified`에서 두 선수를 찾는다.
  `MatchEnd`이며 양쪽 `tickToEnableMove`가 지난 경우에만 복귀한다. 기본 결과 표시 시간은 120틱/2초다.
- 기존 `YOU WIN`/`YOU LOSE` 표시 시간이 끝나면 `DisconnectAsync(UserRequest)`를 호출한다.
  기존 연결 구현이 재접속 정보를 지우고 Runner 종료, Photon 연결 종료, 경기 씬 unload를 수행한 뒤
  `QuantumMenuUIMain`을 표시한다. 별도 씬 이름 하드코딩이나 전역 Default Runner를 사용하지 않는다.
- 자동 복귀와 나가기 버튼이 겹쳐도 같은 종료 요청을 중복 실행하지 않는다.
  [SDK 메뉴 확장](../../Assets/Photon/QuantumMenu/Runtime/QuantumMenu.Sdk.cs)의 네트워크 오류 처리도
  이미 종료 중이면 추가 종료/재접속을 시작하지 않는다. 메뉴를 숨길 때 종료를 잠그고 새 경기를 표시할 때 해제한다.
- 종료 예외는 기록하고 자동 재시도를 중단한다. 나가기 버튼으로 명시적으로 다시 시도할 수 있다.
- 일반 라운드/동시 KO 무승부/인트로는 기존 흐름을 유지한다.
  Simulation/QTN/CodeGen/프로토콜/시스템 순서/승패 규칙/씬 직렬화 참조는 변경하지 않았다.

## 최종 한 차례 검증

근거: [검증 JSON](evidence/match-return-validation.json), Unity job `4d95cf0b1d1f481093b5cb54b5904220`.

| 검사 | 결과 |
|---|---|
| 정적 정책 | PASS — Simulation 94개, 기존/신규 적중 0. 결정론 테스트를 대신하지 않음 |
| Unity 컴파일 | PASS — 메뉴의 새 `CheckLSDFMatchEnd`가 로드된 어셈블리에 존재. 오류 0, 기존 경고 9건. 콘솔을 지우거나 제외 폴더 소스를 열지 않음 |
| 기존 라운드 회귀 | PASS — `LSDFRoundResultTests` 19/19, FAIL/SKIP 0, job 0.643초 |
| 새 복귀 회귀 | **NOT RUN** — 아래 임포트 문제로 5개 사례가 이번 테스트 실행에서 빠짐 |
| 실제 온라인 매치 종료/씬 unload/육안 확인 | **NOT RUN** |
| 전체 입력 replay/강제 네트워크 rollback | **NOT RUN** — Simulation 변경 없음 |

새 [복귀 fixture](../../Assets/Tests/Editor/LSDFMatchReturnTests.cs)와 `.meta`를 추가했다.
양쪽 승리, 두 결과 표시 기한의 경계, 일반 라운드/무승부/인트로, 중복 종료,
cleanup 완료 전 메인 표시 금지, 재진입, 자신의 Runner가 없는 메뉴를 다루도록 작성했다.
실제 메뉴 씬의 preview와 종료 완료를 지연시키는 연결 대역을 사용하며 실제 Photon cleanup 검사는 아니다.

최종 refresh를 `scope=scripts`로 호출하여 기존 파일의 변경은 컴파일됐지만 새 테스트 파일은
AssetDatabase에 임포트되지 않았다. 테스트 실행 요청은 새/기존 두 그룹이었으나 실제 결과에는 기존 19개만 있다.
원인 조사에서 `menuHasMatchReturn=true`, `testTypeImported=false`, `scriptImported=false`를 확인했다.
19개 통과를 새 복귀 테스트 통과로 해석하지 않는다. 단일 검증 방침에 따라 임포트/컴파일/테스트를 다시 요청하지 않았다.
새 테스트의 컴파일과 동작은 미검증이다. 후속 별도 검증 요청 시 새 파일을 assets refresh로 임포트하고 실행해야 한다.

같은 최종 묶음의 마지막 diff 공백 검사, 관련 문서 링크 22개, 새 파일 공백 및 `.meta` GUID 형식 검사는 PASS다.
검사 후에는 결과 문구와 증거 메타데이터만 기록했으며 코드/테스트는 변경하지 않았다.
기존 루트 IDE 생성 파일 변경과 미추적 `.csproj`는 보존한다. 새 fixture/문서/evidence도 자동 staging/커밋하지 않는다.
과거 공중 피격/콜라이더/카메라의 미해결 자동 검사 이력은 [현재 상태](current-state.md)에 그대로 둔다.

## 후속 사용자 확인

사용자가 모든 테스트를 직접 완료했다며 기존 작업 브랜치 푸시를 요청했다.
[사용자 확인과 게시 범위](user-validation-match-return.md)를 최신 근거로 사용한다.
위 자동 검사 원본은 보존하며 게임 검사를 다시 실행하지 않는다. 코드와 테스트 소스는 사용자 확인 이후 변경하지 않는다.
