# 훅 구성과 운영

`Hook.md` 자체는 실행 파일이 아니다. 실제 Codex 훅 정의는 [.codex/hooks.json](.codex/hooks.json),
실행기는 [tools/ai/quantum_guard.py](tools/ai/quantum_guard.py)다.
이 프로젝트에 파일을 배치했으며 전역 설정이나 Git `core.hooksPath`는 변경하지 않는다.

## 등록한 동작

| 이벤트 | 동작 | 변경 여부 |
|---|---|---|
| SessionStart | AGENTS → 현재 작업 상태 → 관련 기록/스킬 읽기, 기존 변경 보존과 종료 시 요약 갱신을 알림 | 읽기 전용 |
| Stop | 등록 해제. 자동 검사·자동 이어가기 없음 | 실행 없음 |

사용자 방침에 따라 검증은 구현 완료 후 한 차례만 한다. Stop 훅에 의한 중복 검사와 후속 자동 실행을 제거했다.
변경에 필요한 검사만 한 묶음으로 실행한다. 문서 작업에 전투 검사를 실행하지 않는다.
실패 시 원인이 분명한 범위만 수정하고 재실행하지 않으며 `수정 후 미검증`으로 보고한다.
새 문제가 사용자 소유 변경이거나 현재 요청 밖이면 수정하지 않고 보고한다.
실행 환경/기준선 오류는 검사 통과로 표현하지 않는다.
훅은 코드 수정, CodeGen, Unity 실행/종료, 씬 저장, 네트워크 전송을 수행하지 않는다.

## 사용 조건과 활성화

로컬 Codex CLI `0.162.1`, Python `3.10`, Git이 확인된 환경이다.
명령은 현재 폴더부터 상위 경로로 `AGENTS.md`와 `tools/ai/quantum_guard.py`가 함께 있는 가장 가까운 루트를 찾는다.
Git의 루트 조회에 의존하지 않으므로 프로젝트 하위 디렉터리와 소유권 오류 환경에서도 실행기를 찾는다.
`startup|resume|clear|compact|fork`에 같은 안내를 전달한다.

Git 상태 조회가 소유권 오류·실행 파일 누락·시간 초과 등으로 실패하면 `UNKNOWN`과 오류 첫 줄을 표시하고 안내를 계속 전달한다.
변경이 없다고 가정하거나 `safe.directory`/전역 설정을 자동으로 바꾸지 않는다. Git 소유권 설정 자체를 복구하는 기능은 아니다.
요약이 없으면 기존 기록·증거·소스에서 상태를 복원하고 모르는 항목을 표시하도록 안내한다.
요약 본문과 과거 대화를 자동으로 주입하지 않으며, 에이전트가 [현재 작업 상태](docs/ai/current-state.md)를 실제로 읽어야 한다.

1. Codex에서 이 프로젝트 폴더를 작업 폴더로 연다.
2. 로컬 CLI가 신뢰 검토를 요구할 때 `/hooks`에서 이 프로젝트의 SessionStart 정의를 관리한다. Stop 정의는 없다.
3. 새 세션에서 SessionStart 메시지를 확인한다. 변경된 훅은 다시 신뢰 검토가 필요할 수 있다.

공식 Codex 동작상 프로젝트 레이어와 훅 정의에 대한 신뢰가 필요하다. 파일 생성만으로 현재 앱 세션에서 이미 활성화됐다고 보지 않는다.
클라우드 오케스트레이션 환경은 로컬 command 훅을 지원하지 않을 수 있다. 이 경우 아래 명령과 AGENTS의 검증 절차를 사용한다.
일반 개발 단계에서 별도 허가 질문을 반복하지 않는다. 도구 자체의 신뢰/권한 정책은 문서가 변경하지 않는다.
실제 lifecycle 자동 호출은 확인 전까지 미검증 상태다.
2026-10-19 조사 세션에서 변경 전 시작 안내 수신을 확인했다. 이번에 변경한 명령의 직접 실행과
새 세션에서의 신뢰/자동 호출은 별개이며, [보완 작업 기록](docs/ai/context-resume.md)에 구분한다.

최종 검증에서 필요한 명령만 선택한다. 아래는 매번 모두 실행하라는 목록이 아니다.

- 시뮬레이션 정책: `python tools/ai/quantum_guard.py check`
- 검사기/훅 구현을 수정했을 때: `python -B -m unittest discover -s tools/ai/tests -v`

스크립트에는 이전 Stop 입력 처리와 그 단위 테스트가 호환용으로 남아 있지만 `.codex/hooks.json`에는 등록되지 않는다.
수동 Stop 호출을 최종 검사에 덧붙이지 않는다.

## 정책과 한계

| 규칙 | 검사 대상 |
|---|---|
| Q001 | 시뮬레이션의 PlayerPrefs 접근 |
| Q002 | FP/float 변환 API |
| Q003 | float/double/Mathf 및 float 리터럴 |
| Q004 | MonoBehaviour/GameObject/Unity 2D 물리 타입 |
| Q005 | 시간/난수/일부 외부 IO·스레드 API |
| Q006 | 초기 감사에서 발견한 공유 실행 상태 필드명 |

기준선은 기존 부채를 유지한 채 신규 패턴을 막는 점진적 검사다. 초기 156건은 서로 겹치는 규칙 적중 수이며 고유 버그 수가 아니다.
`pass`는 신규 어휘 패턴이 없다는 의미다. C# 의미 분석, 직렬화 안전성, 롤백/체크섬, 게임 테스트를 대체하지 않는다.
기준선 변경/새 스캔 범위는 사람이 diff를 검토한다. 자세한 갱신 절차는 [검증 문서](docs/ai/validation.md)에 있다.

문제 시 `/hooks`에서 이 프로젝트의 훅만 비활성화할 수 있다. 전역 훅/보안 정책을 일괄 끄지 않는다.
CI/Git 커밋 훅은 이번에 활성화하지 않았다. 향후 CI에서 같은 `check` 명령을 사용할 수 있지만 Unity 게임 테스트는 별도 환경이 필요하다.

근거: [Codex Hooks](https://learn.chatgpt.com/docs/hooks), [AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md),
[프로젝트 스킬 탐색](https://learn.chatgpt.com/docs/build-skills).
