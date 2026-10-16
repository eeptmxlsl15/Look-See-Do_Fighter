# 초기 세팅 검증 기록 — 2026-10-16

이 문서는 최초 구성 당시의 기록이다. 이후 사용자 요청으로 검증은 작업당 한 차례로 제한했고
Stop 훅 등록을 해제했다. 현재 운영 규칙은 [개발 운영 지침](development-workflow.md)과 [검증 절차](validation.md)를 따른다.
아래 SessionStart/Stop 검사 결과는 최초 구성의 역사적 기록이며 현재 Stop 활성화를 의미하지 않는다.

## 실제 실행 결과

| 검사 | 결과 | 관측 |
|---|---|---|
| `python tools/ai/quantum_guard.py check` | PASS | 사용자 Simulation C# 78개, 기존 규칙 적중 156건, 신규 0건 |
| `python -B -m unittest discover -s tools/ai/tests -v` | PASS | fixture 및 실제 훅 명령 검증 총 11개 통과 |
| skill-creator `quick_validate.py` | PASS | 프로젝트 스킬 3개 모두 유효 |
| `git diff --check` | PASS | 추적 파일 diff 공백 오류 없음; 새 파일 형식은 별도 확인 |
| 게임 소스 변경 여부 | PASS | Git 상태상 Assets/ProjectSettings 변경 없음 |
| Markdown 상대 링크 검사 | PASS | 문서/스킬 12개 내 로컬 링크 대상 존재 |
| 등록된 SessionStart/Stop 명령 | PASS | 실제 JSON 설정의 명령을 `Assets/QuantumUser`에서 실행, 올바른 응답 확인 |

테스트는 기존 부채 허용/추가 복제 검출, 공백·주석 변화, float/FP 구분,
제외 경로와 View 분리, 해소된 부채, Stop 반복 제한, dirty worktree 안내,
누락 기준선/잘못된 hook JSON 처리, 검사 실행 전후 fixture 파일 불변을 확인했다.
등록 명령 검사는 PowerShell의 정확한 명령 전달을 사용했다. 최초 수동 점검 래퍼에서는 중첩 인용부호가
소실되어 오류가 났고, 명령 원문을 전달하는 통합 테스트로 확인했다. 실제 훅 JSON 변경은 필요하지 않았다.

## 별도 확인이 필요한 항목

- Codex 앱/CLI의 실제 lifecycle 훅 자동 호출: 사용자 프로젝트/훅 신뢰 UI 확인 전에는 활성화 완료로 보지 않는다.
- Unity CodeGen/컴파일/PlayMode/빌드/온라인 대전/체크섬/성능: 이번 세팅에서는 미실행.
  게임 소스를 수정하지 않았으며 정적 검사와 도구 테스트는 게임의 결정론을 증명하지 않는다.
- SDK 전체 및 모든 씬·Animator 에셋 참조: 전수 검증 아님. 범위는 architecture-audit.md 참조.

## 보존한 기존 변경

작업 시작부터 있던 `Assembly-CSharp.csproj`, `Assembly-CSharp.Player.csproj`,
`Packages/manifest.json`, `Packages/packages-lock.json` 변경은 그대로 두었다.
기존 빈 `AGENTS.md`에 규칙을 작성했고 나머지는 프로젝트 내부 자동화/문서 파일로 추가했다.
커밋, 푸시, 전역 설정 변경, Git 훅 설치는 수행하지 않았다.
