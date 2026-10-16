# 기존 GitHub 저장소 게시 워크플로

2026-10-16 설치. 새로운 GitHub 저장소를 만들지 않았으며 이력·remote·기존 변경을 보존했다.
이번 요청은 워크플로 구축이다. 실제 프로젝트의 staging, 브랜치 생성, 커밋, Push, PR 생성은 실행하지 않았다.

## 확인한 연결 상태

| 항목 | 확인 결과 |
|---|---|
| 로컬 프로젝트 | `C:\Users\Admin\Look-See-Do_Fighter` |
| 기존 origin fetch/push | `https://github.com/eeptmxlsl15/Look-See-Do_Fighter.git` |
| GitHub 저장소 | [eeptmxlsl15/Look-See-Do_Fighter](https://github.com/eeptmxlsl15/Look-See-Do_Fighter) — 공개 저장소 |
| 현재 브랜치 | `dev/Prototype`, upstream `origin/dev/Prototype` |
| 시작 HEAD | `64a37db6` — skill,hook 세팅 / 리팩토링할 부분 서칭 |
| 캐시된 upstream 대비 | ahead 0 / behind 0. fetch하지 않았으므로 원격 최신 동기화 보증은 아님 |
| GitHub 기본 브랜치 | `main`; 읽기 전용 `ls-remote`로 HEAD `b2d70111c4a7c56dcba47ee59b219f33227aac65` 확인 |
| GitHub CLI | `2.102.0`; `C:\Program Files\GitHub CLI\gh.exe` 설치, 현재 셸 PATH에서는 발견되지 않음 |
| 인증 | `eeptmxlsl15` 활성 계정, keyring, HTTPS. 인증 및 저장소 조회 성공; 권한 ADMIN |
| 시작 index | staged 변경 없음 |
| 기존 작업 트리 | P0~4차 게임 소스·에셋·CodeGen·테스트·결과 문서와 IDE 생성 파일 변경이 존재함 |

첫 샌드박스 조회는 소켓 접근 제한 때문에 실패했고 인증 메시지에 invalid token도 표시됐다.
허용된 네트워크 환경에서 같은 읽기 전용 조회를 수행해 인증과 저장소 접근을 확인했다.
토큰 자체는 출력/저장하지 않았고 로그인·로그아웃·토큰 변경·remote 변경도 하지 않았다.
여기에 인증 scope나 비밀값을 복사하지 않는다. 실제 게시 시 다시 현재 상태를 확인한다.

## 설치한 파일

```text
Look-See-Do_Fighter/
├── AGENTS.md                              # GitHub 승인·검증·브랜치 규칙 추가
├── Skill.md                               # 새 스킬 색인/호출 예시
├── AI_SETUP.md                            # 폴더 구조와 안내 링크 갱신
├── .agents/skills/publish-unity-pr/
│   ├── SKILL.md                           # 변경 분석부터 승인 후 Draft PR까지
│   ├── references/preflight.md            # 검사 범위·실행법·한계
│   └── scripts/
│       ├── preflight.py                   # index와 커밋 이력의 읽기 전용 검사
│       └── tests/test_preflight.py        # 임시 로컬 저장소 기반 회귀 검사
└── docs/ai/github-publishing.md           # 이 안내와 설치 상태
```

실제 스킬: [publish-unity-pr/SKILL.md](../../.agents/skills/publish-unity-pr/SKILL.md).
프로젝트 루트에서 Codex를 열고 다음처럼 요청한다.

```text
$publish-unity-pr 현재 변경을 검증해서 게시 후보를 준비해 줘. Push와 Draft PR 생성 전에 승인받아 줘.
```

프로젝트 밖에서 시작한 세션이면 위 파일 경로를 명시해 읽도록 요청한다.
Skill 작성 지침에 따라 재사용 절차와 읽기 전용 검사기를 묶었다. 전역 Skill·인증·PATH·Git hook 설정은 바꾸지 않았다.
Skill은 에이전트의 실행 절차이며 GitHub 서버의 branch protection이나 CI를 설치한 것은 아니다.

## 실행 순서와 중단 조건

1. 변경/브랜치/remote/CLI 인증을 분석하고 포함 범위와 PR base를 정한다.
2. 한 차례의 Unity 컴파일과 필요한 테스트를 수행한다. 온라인 2클라이언트 검사도 실제 결과로 판정한다.
3. Quantum 정적 검사와 결정론 경계의 코드 검토를 수행한다.
4. 명시적 파일 후보와 업로드할 커밋 이력에서 비밀정보·생성물·미검사 파일을 확인한다.
5. 모두 통과하면 작업 브랜치에 로컬 커밋한다. 테스트와 커밋 내용의 일치도 확인한다.
6. 정확한 대상·커밋·검증 결과·PR 초안을 제시해 **Push와 Draft PR 생성 승인**을 받는다.
7. 승인된 작업 브랜치에만 Push하고 기존 저장소에 Draft PR을 생성·확인한다.

`FAIL`, `NOT RUN`, 필수 `SKIP`, 0 tests, timeout, 수정 후 미검증, 미해결 검사 항목은 자동 게시를 막는다.
이전의 '권한 묻지 말기'는 일반 개발에 적용하며, GitHub 원격 쓰기는 이번에 명시한 승인 규칙을 따른다.
Push만 승인받은 경우 PR 생성은 별도 승인이 필요하다. 승인 후 대상 커밋이 바뀌면 기존 승인을 재사용하지 않는다.

검사기는 정규식 기반 보조 도구다. 비밀정보 부재·게임 정상 동작·결정론을 증명하지 않는다.
큰 파일, 바이너리, LFS 원본, 제외 에셋 폴더는 자동 PASS하지 않는다. 파일을 임의로 삭제해 해결하지 않는다.
자세한 범위와 한계: [preflight 안내](../../.agents/skills/publish-unity-pr/references/preflight.md).

## 현재 게임과 설치 검증의 구분

최근 [4차 리팩토링 결과](refactor-phase4-result.md)는 단위/에셋 32건 PASS, 온라인 1건 FAIL이며
온라인 실행기 후속 수정은 미검증이다. 이 기록만으로 현재 리팩토링 변경을 게시할 수 없다.
이번에는 워크플로 파일과 검사기를 구성했으며 Unity 컴파일/게임 테스트를 새로 실행하지 않았다.
다음 게시 요청에서 정확한 후보에 대해 한 차례 검증하고, 필수 검사를 통과해야 커밋 이후로 진행한다.

설치 검증은 Skill 형식·문서 링크·추가 diff와 검사기 fixture를 대상으로 한다.
이 검증 결과는 Unity 테스트 성공으로 간주하지 않는다.

최종 한 차례 설치 검증 결과:

| 검사 | 결과 |
|---|---|
| skill-creator `quick_validate.py` | PASS — Skill is valid |
| 검사기 임시 저장소 테스트 | PASS — 13건, 실패 0건 |
| 관련 문서 6개 상대 링크·공백 | PASS |
| 기존 문서 3개의 `git diff --check` | PASS — CRLF 변환 안내만 존재 |
| 실제 프로젝트 index | staged 변경 없음, 보존 |
| 이번 Unity 컴파일·게임 테스트 | NOT RUN — 워크플로 구축 범위 |
| 실제 커밋·Push·Draft PR | NOT RUN — 게시 실행 요청/승인 없음 |

13건에는 index/작업 트리 구별, 중간 커밋에서 삭제한 토큰, 커밋 메시지 토큰,
비밀값 출력 차단, 제외 폴더 blob 미열람, 생성물/인증 파일, 바이너리/LFS/대형 파일,
CP949/UTF-16, CLI 종료 코드, 읽기 전용 보존 동작이 포함된다.
검증 이후에는 이 결과 기록만 추가했으며 검사기와 Skill의 실행 내용은 바꾸지 않았다.

근거: [Codex 프로젝트 Skills](https://learn.chatgpt.com/docs/build-skills),
[GitHub CLI 인증 상태](https://cli.github.com/manual/gh_auth_status), [Draft PR 생성](https://cli.github.com/manual/gh_pr_create).
