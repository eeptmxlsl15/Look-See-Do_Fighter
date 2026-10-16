---
name: publish-unity-pr
description: Prepare and publish verified Unity and Photon Quantum 3 changes to the existing Look-See-Do Fighter GitHub repository as a draft PR. Use for commit, push, or PR publication requests; require user approval before push and PR creation.
---

# 기존 저장소에 Unity Draft PR 게시

프로젝트 루트에서 [AGENTS.md](../../../AGENTS.md)와 [검증 기준](../../../docs/ai/validation.md)을 읽는다.
이 스킬을 만들거나 설명하는 요청은 게시 실행 요청이 아니다. 아래 절차는 게시를 요청받았을 때 실행한다.
기존 저장소는 `eeptmxlsl15/Look-See-Do_Fighter`, remote는 `origin`이다. 매번 실제 설정을 대조한다.

## 1. Git 변경 사항과 게시 범위 분석

- `git status --short --branch`, `git branch -vv`, `git remote -v`, staged/unstaged/untracked 목록을 확인한다.
  기존 변경을 삭제·되돌리기·자동 stash하지 않는다. 작업에 포함할 경로와 제외할 경로를 명시한다.
  비밀정보 가능성이 있는 파일의 본문을 콘솔에 출력하지 않는다.
- `origin`의 fetch/push URL을 **모두** 확인한다. HTTPS/SSH 표기 차이는 허용하되 대상은 위 저장소여야 한다.
  다른 대상·복수 push URL·URL rewrite 등으로 목적지가 불명확하면 중단한다.
  `git init`, `gh repo create`, fork 생성, remote 추가/변경, 강제 push, 이력 재작성은 하지 않는다.
- `gh auth status --hostname github.com`과 `gh repo view eeptmxlsl15/Look-See-Do_Fighter --json nameWithOwner,url,defaultBranchRef,viewerPermission`을 확인한다.
  Windows에서 PATH에 없다면 `C:\Program Files\GitHub CLI\gh.exe`의 존재를 확인해 사용한다.
  토큰을 출력하거나 인증 파일을 열지 않는다. `--show-token`, `gh auth token`을 쓰지 않는다.
  인증/네트워크 실패는 성공으로 간주하지 않는다. 실제 오류와 제한을 기록한다.
- 원격 조회가 성공하면 `git fetch --no-tags origin`으로 비교 기준을 갱신한다. 이는 원격 읽기이며 push 승인이 아니다.
  기존 PR, 사용자 지정 base, 브랜치 설정, 원격 기본 브랜치를 참고해 base를 정하고 커밋 목록과 PR 전체 diff 범위를 확인한다.
  다른 작업의 이력이 함께 포함되면 자동 병합·rebase로 정리하지 않는다.
- 현재 작업 브랜치를 사용할 수 있으면 유지한다. `main`, `master`, 원격 기본 브랜치, detached HEAD에서는
  충돌 없는 `codex/<task>` 작업 브랜치를 현재 HEAD에서 만든다. 기존 브랜치를 강제로 재설정하지 않는다.
  게시 범위 밖의 기존 staged 변경이 있으면 무단 unstage/commit하지 않고 범위를 먼저 해결한다.

## 2. Unity 컴파일과 테스트 — 최종 한 차례

- 검증 대상은 커밋 예정 소스·에셋·설정과 같아야 한다. 부분 staging으로 테스트한 파일과 커밋할 내용이 달라지지 않게 한다.
  제외한 기존 게임 변경이 빌드에 영향을 주면 그 작업 트리의 PASS를 게시할 커밋에 적용할 수 없다.
  안전한 별도 검증 환경이 없으면 `BLOCKED`로 남긴다.
- unityMCP `manage_scene(action="get_active")` 및 에디터 상태로 프로젝트를 확인한다.
  `.qtn` 변경은 설치된 Quantum 3 CodeGen으로 생성하고 결과를 포함한다.
  `refresh_unity(mode="if_dirty", scope="all", compile="request", wait_for_ready=true)` 후 컴파일 완료를 기다리고
  `read_console(action="get", types=["error","warning"])`로 오류를 확인한다. 콘솔을 지우지 않는다.
  과거 로그는 시점/스택으로 구분하며 현재 컴파일 성공을 확인할 수 없으면 `NOT RUN`이다.
- 실제 등록된 테스트를 확인하고 `run_tests(mode="EditMode", assembly_names=["LSDF.Tests"], include_details=true, include_failed_tests=true)`를 실행한다.
  비동기 job은 `get_test_job`으로 완료까지 확인한다. API 인자는 현재 MCP 도구 스키마를 우선한다.
  이 프로젝트의 `LSDF.Tests`에는 Play Mode에 진입하는 **Photon 온라인 2클라이언트 검사**가 포함된다.
  EditMode라는 이름만으로 오프라인 검사로 해석하거나 온라인 검사를 제외해 통과시키지 않는다.
- 게임 변경에 필요한 추가 PlayMode/입력 재생/강제 롤백 시나리오는 검증 매트릭스에 따라 같은 최종 묶음에 포함한다.
  checksum 비교와 실제 재접속·rollback의 증거를 구별한다. 단위 테스트만으로 멀티플레이 성공이라 쓰지 않는다.
- 컴파일 실패, 테스트 FAIL, 0 tests, 취소, timeout, 필수 검사 SKIP/NOT RUN은 **커밋·Push·PR 단계로 진행하지 않는다**.
  문서 게시도 이 스킬에서는 Unity 컴파일과 실행 가능한 테스트의 성공 증거가 필요하다.
  이번처럼 스킬을 설정만 하는 작업에는 Unity 게임 검사를 강제로 실행하지 않는다.
- 같은 내용에 대한 이번 작업의 완료된 검증 결과는 재사용하고 중복 실행하지 않는다.
  실패 후 자동 재실행하지 않는다. 수정 후에는 `수정 후 미검증`으로 중단하고 사용자의 새 검증 요청을 기다린다.
  이전 차수의 실패/수정 후 미검증 기록은 새 변경의 PASS가 아니다. MCP 실패 시 실제 오류를 보고하며 우회하지 않는다.

## 3. Quantum 결정론 검토

- `python tools/ai/quantum_guard.py check`를 최종 검증 묶음에 한 번 실행한다. 신규 위반이나 검사 오류면 중단한다.
  검사 통과를 위해 baseline을 확대하지 않는다. 기존 부채와 신규 변경을 구분한다.
- Simulation diff의 `FP`/정수 틱, Frame 소유 상태, 불변 에셋, `RuntimePlayer`/`AddPlayer`, 시스템 순서와 직렬화를 검토한다.
  float/Unity 시간·물리·난수, PlayerPrefs/로컬 IO, static/공유 이벤트 에셋의 실행 상태가 판정에 유입되는지 확인한다.
  View의 float와 Inspector 속성은 별개다. 어휘 검사는 의미 분석·결정론 증명이 아니다.
- `.meta` GUID, Quantum asset ID, Animator 계약과 CodeGen 결과를 보존한다. 미해결 결정론 위험은 게시 차단 사유다.

## 4. 비밀정보·불필요 파일 검사와 후보 확정

- 관련 경로만 명시적으로 `git add -- <paths>` 한다. `git add .`, `git add -A`, `git commit -a`를 사용하지 않는다.
  사용자 소유의 기존 staged 내용을 몰래 포함하지 않는다. staging은 커밋이 아니며 실패 시 파일을 삭제하지 않는다.
- [검사기 사용법](references/preflight.md)을 읽고 staged 내용과 **이번에 원격으로 나갈 커밋 이력**을 검사한다.
  삭제된 비밀정보도 중간 커밋에 남으면 업로드되므로 최종 diff만 보지 않는다.
  `python .agents/skills/publish-unity-pr/scripts/preflight.py --base <검토한-로컬-base-ref>`를 한 번 실행한다.
  이 도구는 로컬 읽기 전용이며 Unity 테스트/인증/승인 확인 도구가 아니다.
- 실제 토큰·비밀번호·개인키·인증서·Unity 라이선스, `.env`, 캐시/빌드/로그, IDE 생성물 포함 여부를 별도로 검토한다.
  이미 추적된 `.csproj`/`.sln`도 자동 포함하지 않는다. 지운다고 과거 이력의 비밀정보가 없어지지 않는다.
  Photon AppId는 관리자 비밀키와 구별하되 공개할 설정과 접근 권한을 검토하고 불필요하게 값 자체를 기록하지 않는다.
- `DinV`, `GabrielAguiarProductions`, `Matthew Guz`는 내용 검사에서 제외한다. 게시 후보에 들어오면 경로만 표시하고 중단한다.
  검사하지 않은 파일을 PASS로 처리하지 않는다. 바이너리/LFS 객체/대용량 파일은 도구의 자동 통과 대상이 아니다.
- 발견 사항·미검사 항목·Git 오류가 있으면 자동 게시를 중단한다. 검토 근거 없이 예외를 만들지 않는다.
  검사기 0은 알려진 패턴의 부재일 뿐 비밀정보 부재 보증이 아니므로 diff 및 커밋 메시지의 수동 검토도 기록한다.
- `git diff --cached --check` 및 후보 경로를 확인하고 `git write-tree`의 tree ID를 검증 증거와 함께 기록한다.
  테스트 후 실행 소스/에셋/설정이 바뀌면 PASS를 재사용하지 않는다. 증거 문서 추가는 그 파일을 따로 검토한다.

## 5. 통과한 후보만 로컬 커밋

위의 필수 검사가 모두 PASS이고 미해결 위험이 없을 때에만 작업 브랜치에 `git commit -m <검토한-메시지>` 한다.
게시 요청은 이 로컬 커밋을 포함하지만, Push/PR 승인은 별개다.
커밋 훅 실패를 `--no-verify`로 우회하지 않는다. 훅이 후보 내용을 바꾸면 검증 증거를 무효화한다.
`git rev-parse HEAD` 및 `HEAD^{tree}`가 검토한 tree와 맞는지 확인하고 잔여 변경과 실제 포함 커밋 목록을 기록한다.

## 6. 사용자 승인 후 기존 저장소에 Push

로컬 준비가 끝나면 **저장소 URL, 원격, head/base, 정확한 커밋 SHA/목록, 변경 요약, 검증 결과, Draft PR 제목/본문**을 제시한다.
사용자가 이 대상을 Push하고 Draft PR을 생성하도록 명시적으로 승인하기 전에는 네트워크 쓰기를 하지 않는다.
두 작업을 명시한 한 번의 승인으로 둘 다 진행할 수 있다. Push만 승인받았다면 PR 생성 승인은 별도로 받는다.
이 규칙은 사용자의 '일반 개발은 묻지 않고 진행' 방침보다 게시 작업에서 우선한다.

승인 후 remote와 HEAD가 승인 내용과 같은지, 원격 branch가 예상 상태인지 읽기 전용으로 확인한다.
변경되면 오래된 승인을 사용하지 않는다. `main`/`master`/기본 브랜치에는 직접 push하지 않는다.
명시적 refspec으로 `git push --set-upstream origin HEAD:refs/heads/<승인된-작업-브랜치>`를 실행한다.
강제 push, `--all`, `--mirror`, tag push는 금지한다. 실패 시 강제 재시도하지 않고 원인을 보고한다.
성공 후 `git ls-remote origin refs/heads/<branch>`가 승인 SHA인지 확인한다.

## 7. 승인된 Draft PR 생성

같은 head/base의 기존 PR이 있는지 `gh pr list --repo eeptmxlsl15/Look-See-Do_Fighter --head <branch> --state open --json number,url,baseRefName,isDraft`로 확인한다.
이미 있으면 중복 생성하거나 임의로 상태를 바꾸지 말고 URL을 보고한다.
없고 PR 생성까지 승인받았으면 다음을 사용한다. 본문은 검토된 UTF-8 파일로 전달한다.

```text
gh pr create --repo eeptmxlsl15/Look-See-Do_Fighter --base <base> --head <branch> --draft --title <title> --body-file <body-path>
```

`--head`를 반드시 지정해 CLI의 자동 fork/push 제안을 피한다. `gh pr create --dry-run`도 push할 수 있으므로 승인 전 미리보기 용도로 쓰지 않는다.
PR 생성이 실패/timeout이면 기존 PR을 읽어 중복 여부를 확인한 뒤 멈춘다. 새 저장소/fork를 만들거나 자동 재시도하지 않는다.
생성 후 `gh pr view --repo ... <number> --json url,isDraft,baseRefName,headRefName,headRefOid`로 Draft 상태와 승인 커밋을 확인한다.
merge, ready-for-review 전환, reviewer 요청은 포함하지 않는다.

최종 보고: 로컬 커밋 SHA / Push 여부 / PR URL·Draft 여부 / 실제 검증 결과 / 미완료 사유.
테스트 원본 결과와 비밀값을 제거한 근거를 PR 본문에 연결한다. 실패 기록은 성공으로 덮어쓰지 않는다.

근거: [Codex Skills](https://learn.chatgpt.com/docs/build-skills),
[GitHub PR 생성](https://cli.github.com/manual/gh_pr_create), [GitHub 인증 상태](https://cli.github.com/manual/gh_auth_status).
