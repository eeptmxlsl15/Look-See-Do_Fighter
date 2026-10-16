# AI 작업 환경 구성 안내

이 프로젝트의 Quantum 3 리팩토링, 기능 추가, 버그 수정, 게임 완성과 AI-native 취업 포트폴리오를 위한 구성이다.
소스 조사 결과로 규칙·스킬·검증 도구를 설치했다. 실제 게임 개발은 요청받은 작업부터 이어간다.
최신 방침은 **일반 개발 작업은 재확인 없이 진행하고, 검증은 구현 완료 후 한 차례만 실행**하는 것이다.
실패 후 자동 재실행하지 않으며, 수정한 부분은 `수정 후 미검증`으로 보고한다.

## 시작하기

이 파일이 있는 `Look-See-Do_Fighter`를 Codex 작업 폴더로 열고 [AGENTS.md](AGENTS.md)를 기준으로 작업한다.
상위 사용자 홈에서 시작한 세션에는 프로젝트 지침/스킬이 자동 적용됐다고 가정하지 않는다.

작업 폴더: `C:\Users\Admin\Look-See-Do_Fighter`.
필요한 검사 선택은 [validation.md](docs/ai/validation.md)를 따른다. 시작할 때 모든 검사를 실행하지 않는다.

작업 예시: `$lsdf-quantum-refactor P0-1 로드아웃 전달부터 수정해 줘. 기존 스킬 선택 UI 동작은 유지해.`
개발 예시: `$lsdf-game-development 라운드 종료 후 재대전과 메뉴 복귀를 구현해 줘.`
문서만 먼저 검토하려면: `docs/ai/architecture-audit.md의 위험 우선순위를 현재 코드와 대조해 설명해 줘.`

## 이번에 만든 구조

```text
Look-See-Do_Fighter/
├── AGENTS.md                         # 기존 빈 파일에 프로젝트 공통 규칙 작성
├── Skill.md                          # 사람용 스킬 색인
├── Hook.md                           # 실제 훅 연결·신뢰·운영·한계
├── AI_SETUP.md                       # 이 안내
├── .agents/
│   └── skills/
│       ├── lsdf-game-development/SKILL.md
│       ├── lsdf-quantum-refactor/SKILL.md
│       ├── lsdf-quantum-verify/SKILL.md
│       └── lsdf-portfolio-evidence/SKILL.md
├── .codex/
│   └── hooks.json                    # SessionStart만 등록, Stop 자동 검증 해제
├── tools/
│   └── ai/
│       ├── quantum_guard.py          # 읽기 전용 정적 검사와 훅 실행기
│       ├── quantum-baseline.json     # 기존 부채 기준선
│       └── tests/test_quantum_guard.py
└── docs/
    └── ai/
        ├── architecture-audit.md     # 조사 범위·실제 구조·소스 근거
        ├── refactor-roadmap.md       # P0/P1/P2 및 단계별 완료 조건
        ├── refactoring-backlog.md    # 소스 근거·작업 ID·멀티플레이 완료 기준
        ├── development-workflow.md   # 기능·버그·밸런스·게임 완성 운영
        ├── validation.md            # 정적/컴파일/전투/결정론 검증
        ├── evidence-template.md      # 사람이 검토한 AI 작업 증거 형식
        └── setup-verification.md     # 이번 세팅의 실제 검증 결과
```

`Assets` 안에는 새 파일을 만들지 않아 문서 때문에 Unity import/메타파일을 추가하지 않는다.
게임 소스, 씬, 프리팹, Quantum 설정 에셋, 패키지와 기존 `.claude`는 이번 세팅에서 변경하지 않는다.
로드맵의 미래 게임 폴더 구조와 위의 실제 생성 구조는 서로 다르다.

## 파일별 읽는 순서

| 목적 | 파일 |
|---|---|
| 현재 문제 이해 | [architecture-audit.md](docs/ai/architecture-audit.md) |
| 다음 리팩토링 선택 | [refactor-roadmap.md](docs/ai/refactor-roadmap.md) |
| 상세 리팩토링 후보와 멀티 검증 | [refactoring-backlog.md](docs/ai/refactoring-backlog.md) |
| 기능 개발·버그 수정·게임 완성 | [development-workflow.md](docs/ai/development-workflow.md) |
| AI 작업 규칙 | [AGENTS.md](AGENTS.md), [Skill.md](Skill.md) |
| 자동 검사 실행/활성화 | [Hook.md](Hook.md), [validation.md](docs/ai/validation.md) |
| 포트폴리오 기록 | [evidence-template.md](docs/ai/evidence-template.md) |
| 세팅 검증 상태 | [setup-verification.md](docs/ai/setup-verification.md) |

## 이번 구성의 핵심

시뮬레이션의 `PlayerPrefs`, float 판정, 공유 이벤트 에셋의 실행 상태를 가장 먼저 해결하도록 순서를 정했다.
스킬 작성 지침에 따라 전역 설정에 규칙을 몰아넣지 않고 개발·리팩토링·검증·포트폴리오의 작업 절차를 나눴다.
실제 자동 탐색 파일명은 `SKILL.md`다. 루트 `Skill.md`와 `Hook.md`는 각각 색인과 운영 문서다.

기존 문제를 모두 즉시 수정하도록 강제하지 않고 기준선으로 기록했다. 신규 위험은 검사가 찾아내되,
`pass`를 '결정론 보장'으로 표현하지 않는다. Quantum의 Frame 상태·불변 에셋·고정소수·직렬화 경계를 사람이 함께 검토한다.

훅 설정은 프로젝트에 배치되어 있고 시작 안내만 등록되어 있다. Stop 자동 검증과 자동 이어가기는 해제했다.
로컬 Codex의 훅 신뢰 조건에 따라 시작 안내를 사용할 수 있으며,
현재 앱 세션에서 실제 자동 호출됐는지는 별도로 확인해야 한다. 상세 조건은 Hook.md에 있다.

## 포트폴리오로 발전시키는 방법

P0 하나를 골라 재현 조건 → 사람의 설계 결정 → AI 패치 검토 → 자동/게임 검증 → 결과 증거를 남긴다.
이후 기능 추가와 버그 수정에도 같은 흐름을 적용한다. 검증은 한 차례 결과를 재사용하고 문서화를 위해 반복하지 않는다.
성능 수치와 체크섬 검증은 실제 측정 후 추가한다. 이 구성 자체의 성과는 코드 조사에 근거한 작업 규칙,
재사용 가능한 스킬, 실행 가능한 정책 검사, 검증 한계가 명시된 운영 체계를 마련한 것이다.
