# 메인 화면 복귀 사용자 테스트 완료·푸시 요청 — 2026-10-19

확인 시각: 2026-10-19T23:15:48+09:00.
사용자가 **“테스트는 내가 다 했어 푸쉬해줘”**라고 확인했다.
이를 현재 기능의 사용자 테스트 완료 보고와 로컬 커밋·기존 작업 브랜치 푸시 요청으로 기록한다.
이번 사용자 지시를 근거로 자동 미실행 이력에 따른 중단 상태에서 게시 준비를 진행한다.
게임 자동 검사를 다시 실행하거나 과거 NOT RUN을 PASS로 바꾸지 않는다.

## 포함 범위와 근거

- 메뉴 C# 2개, `LSDFMatchReturnTests.cs`와 `.meta`.
- [구현 기록](match-return-to-main.md), [자동 검사 원본](evidence/match-return-validation.json),
  이 확인 기록 및 [현재 상태](current-state.md).
- 기준 HEAD `baed7c0d7777721bebe5762e52b4e05283dc4e39`, 브랜치 `dev/Prototype`.
  소스 C# 3개의 SHA-256은 이전 검증 근거와 모두 일치한다.
- 이전 자동 근거: 메뉴 컴파일, 정적 정책, 기존 라운드 19개 PASS.
  새 복귀 fixture 5개는 당시 Unity 미임포트로 NOT RUN이었다. 사용자 테스트의 명령·개수·상세 로그는 제공되지 않았다.
  온라인/재생/롤백 자동 측정 결과가 새로 생긴 것으로 기록하지 않는다.
- 기존 루트 `.csproj`/`.sln` 변경 9개 및 미추적 `.csproj` 4개는 제외하고 보존한다.

## 원격 대상

기존 저장소 `eeptmxlsl15/Look-See-Do_Fighter`, 단일 origin fetch/push URL을 확인했다.
GitHub 인증 성공, ADMIN 권한, 기본 브랜치 `main`을 확인했고 URL rewrite는 없었다.
`git fetch --no-tags origin` 후 `origin/dev/Prototype...HEAD`는 0/0이었다.
같은 head의 열린 PR은 없으며 이번 요청은 푸시만 포함한다. PR 생성·merge·기본 브랜치 푸시는 하지 않는다.

푸시 대상은 `origin`의 `refs/heads/dev/Prototype`이며 refspec은 `HEAD:refs/heads/dev/Prototype`이다.
본 문서를 포함한 정확한 로컬 커밋 SHA와 게시 전 검사 결과를 먼저 제시한다.
게시 결과는 실행 결과와 원격 ref 일치 여부로 구분하며, 사용자 테스트를 자동 테스트 결과로 바꾸지 않는다.
