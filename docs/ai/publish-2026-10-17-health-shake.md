# 체력바 연출·로드아웃 진단 게시 후보 — 2026-10-17

사용자가 조정한 체력바 흔들림 강도 20을 포함해 현재 결과물을 기존 `origin`에 게시하는 요청이다. 요청 범위는 Push이며 PR 생성은 포함하지 않는다.

## 게시 범위

- 저장소: `eeptmxlsl15/Look-See-Do_Fighter`, origin fetch/push 모두 `https://github.com/eeptmxlsl15/Look-See-Do_Fighter.git`.
- 브랜치: `dev/Prototype`, 비교 기준 `origin/dev/Prototype`의 `a959b5bdf5057e2e02c4370f9a69294e8bd1ecd5`. fetch 후 미게시 기존 커밋 0개, index의 기존 staged 변경 0개.
- 포함: 피격 체력바 연출·두 HUD의 네 그룹 연결, 사용자 프리팹 강도 20/시간 0.22초, 연결 종료 진단, 메뉴·온라인 로드아웃 검사, 관련 작업·포트폴리오 문서.
- 제외: 기존 `.csproj`/`.sln` IDE 생성물. 삭제·stash·reset하지 않고 로컬에 보존한다. 제외 에셋 폴더는 포함하거나 읽지 않는다.
- 유지: SDK 코어·Animator 애드온·Simulation·QTN·Generated·Animator 그래프·시스템 등록 순서. 사용자 프리팹 저장에 따른 기존 기본값의 직렬화 확장은 GUID/asset ID와 판정 shape 값을 바꾸지 않는 것으로 소스 검토했다.

## 이번 한 차례 검증

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS: 현재 컴파일 완료, 콘솔 오류·경고 0 |
| LSDF.Tests 전체 | **235 PASS / 0 FAIL / 0 SKIP** |
| 메뉴/로드아웃 | 72가지 조합과 payload 독립성·재접속 제출값 유지, 합계 74개 통과 |
| 서로 다른 기술의 Photon 대전 | PASS: 같은 커맨드에 서로 다른 기술 10개 경로를 양쪽에서 관측, 공통 checksum 67개 일치 |
| 기존 Photon 매치 | PASS: 선입력, 5라운드·3:2 종료, 반복 AddPlayer, 두 재생·Frame 복원 포함 |
| 체력바/씬 연결 | PASS: 양쪽 피격 대상, 가드·회복·피해 0·수명·씬 root 참조 검사 |
| Quantum 정책 | PASS: Simulation 91개, 기존·신규 적중 0. 어휘 검사로 결정론 전체를 증명하지 않음 |

job `537a3260b7b340fb82c01bab7c1e94e7`. 실행기 요약 시간은 188.0325537초이며 PlayMode 전환을 포함한 개별 테스트 시간과 함께 원래 반환값을 보존한다. 합산하거나 게임 성능 수치로 사용하지 않는다.

컴파일 준비 완료 대기 중 editor state 조회 한 건은 `Unity did not respond to 'get_editor_state' within 2.0s`를 반환했다. 같은 컴파일의 완료 대기 뒤 ready 상태를 확인했으며 테스트 실행은 한 번이다. 테스트 뒤 활성 씬은 LSDF_Menu, 미저장 변경 없음이다.

[모든 테스트 식별자·상태와 핵심 관측](evidence/health-shake-publish-validation.json). 이전 컴파일 실패 및 씬 검사 실패 이력은 각각 [로드아웃 기록](loadout-isolation.md), [체력바 기록](health-bar-shake.md)에 보존한다.

독립 실행 빌드·다른 컴퓨터/플랫폼·실제 재접속·강제 네트워크 지터·화면 육안 확인은 NOT RUN이다. 새로운 서로 다른 세팅 시나리오는 통과했지만 최초 사용자가 경험한 Photon 종료의 원인은 확정되지 않았다.

## 후보 검토와 게시

검증한 소스·에셋·테스트 11개 파일의 해시가 그대로인 것을 확인했고, 관련 20개 파일만 명시적으로 staging했다. 문서 상대 링크 49개가 존재한다. `preflight.py --base origin/dev/Prototype`은 staged blob 20개·미게시 기존 커밋 0개에서 알려진 패턴/미검사 항목 0건(`NO_KNOWN_FINDINGS`)이었다. staged diff 공백 검사도 통과했다. 이는 비밀정보 부재의 보증이 아니다.

검사 시점 tree는 `844046b73ac8b519c9a8d12d85770285e203323a`다. 이후에는 이 비실행 문서와 JSON의 검사 결과/상태만 갱신해 별도로 내용을 검토했다. 게임 소스·설정·에셋·테스트는 바꾸지 않았다. 샌드박스 안의 최초 write-tree는 index.lock 쓰기 권한으로 실패했고, 허용된 실행 환경에서 tree 생성을 완료했다. 보안 검사나 게임 테스트를 재실행한 것은 아니다.

필수 검사를 통과한 현재 후보를 로컬 커밋으로 준비한다. 실제 Push는 정확한 커밋과 검사 결과를 사용자에게 제시한 뒤 승인받아 실행한다. PR 생성은 요청 범위에 없다.
