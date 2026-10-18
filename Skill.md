# 프로젝트 스킬 안내

이 파일은 사람이 읽는 색인이다. Codex가 탐색하는 실제 파일 이름은 대문자 `SKILL.md`이며 `.agents/skills/<name>/SKILL.md`에 있다.
프로젝트 루트 또는 하위 경로에서 Codex를 시작한다. 상위 `C:/Users/Admin`에서 시작한 기존 세션은 이 프로젝트의 스킬을 자동 발견한다고 가정하지 않는다.

새 세션이나 컨텍스트 초기화 뒤에는 [AGENTS.md](AGENTS.md) → [현재 작업 상태](docs/ai/current-state.md) →
관련 최근 작업 기록/증거 → 아래 작업별 스킬 순서로 읽는다. 작업 종료 시 현재 작업 상태를 갱신한다.

| 스킬 | 쓰는 작업 | 실제 위치 |
|---|---|---|
| `lsdf-game-development` | 기능 추가·버그 수정·밸런스·UI/연출·게임 완성 | [.agents/skills/lsdf-game-development/SKILL.md](.agents/skills/lsdf-game-development/SKILL.md) |
| `lsdf-quantum-refactor` | 결정론·시뮬레이션/뷰 경계·공격 데이터 리팩토링 | [.agents/skills/lsdf-quantum-refactor/SKILL.md](.agents/skills/lsdf-quantum-refactor/SKILL.md) |
| `lsdf-quantum-verify` | 회귀검사, CodeGen/컴파일, 재생·체크섬 검증 계획과 결과 | [.agents/skills/lsdf-quantum-verify/SKILL.md](.agents/skills/lsdf-quantum-verify/SKILL.md) |
| `lsdf-portfolio-evidence` | 완료한 작업을 취업 포트폴리오용 근거 문서로 작성 | [.agents/skills/lsdf-portfolio-evidence/SKILL.md](.agents/skills/lsdf-portfolio-evidence/SKILL.md) |
| `publish-unity-pr` | 검증된 변경을 기존 GitHub 저장소에 커밋·Push·Draft PR 게시; 원격 쓰기 전 승인 | [.agents/skills/publish-unity-pr/SKILL.md](.agents/skills/publish-unity-pr/SKILL.md) |

예시: `$lsdf-quantum-refactor P0-1의 PlayerPrefs 의존을 RuntimePlayer 전달로 바꿔 줘. 먼저 현재 연결 경로와 보존할 동작을 확인해.`

예시: `$lsdf-quantum-verify 이번 공격 버퍼 수정의 롤백 안전성을 검증하고 실행하지 못한 검사를 분리해 기록해.`

예시: `$lsdf-game-development 라운드 제한시간과 매치 종료를 구현해 줘. Quantum 3 구조를 유지하고 최종 검증은 한 차례만 해.`

예시: `$lsdf-game-development 재대전 후 체력 UI가 갱신되지 않는 버그를 수정해 줘.`

일반 개발 작업은 `lsdf-game-development`, 구조 정리는 `lsdf-quantum-refactor`를 사용한다.
검증 스킬은 최종 검증 한 차례의 실행 기준이며 별도 반복 검사를 뜻하지 않는다.
여러 스킬을 함께 쓰더라도 한 작업의 검증 횟수는 합쳐서 한 차례다.
스킬은 작업 절차이며, 요청받은 범위 안에서는 단계별 허가 질문 없이 진행한다.
GitHub 게시에는 별도 규칙이 있다. `publish-unity-pr`은 로컬 준비 후 Push와 Draft PR 생성에 명시적 승인을 받는다.
예시: `$publish-unity-pr 현재 변경의 게시 후보를 검증하고, Push와 Draft PR 생성 전에 승인받아 줘.`
