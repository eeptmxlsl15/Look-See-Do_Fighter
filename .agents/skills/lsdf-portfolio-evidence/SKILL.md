---
name: lsdf-portfolio-evidence
description: Turn completed Look-See-Do Fighter engineering work into Korean AI-native portfolio evidence, separating human decisions, AI contributions, measured results, and unverified claims.
---

# AI-native 포트폴리오 증거

[증거 템플릿](../../../docs/ai/evidence-template.md)을 사용한다.
먼저 작업 diff, 실제 로그/테스트 결과, 관련 설계 결정을 읽는다. 문서화 요청만으로 코드를 바꾸거나 외부에 게시하지 않는다.
해당 작업의 한 차례 검증 결과를 재사용한다. 포트폴리오용 증거를 만들기 위해 같은 검사를 재실행하지 않는다.

- 문제를 구체적으로 적는다. 예: 두 클라이언트의 로컬 스킬 저장값 차이가 시뮬레이션 입력 계약을 우회했다.
- 작성자가 선택한 설계와 AI가 제안·구현·검증한 일을 구분한다. 검토해서 버린 AI 제안도 이유가 있으면 기록한다.
- 대안과 선택 근거를 Quantum 3 결정론·롤백·에셋 경계에 연결한다. 사용한 프롬프트 수나 도구 수를 성과로 대체하지 않는다.
- 전후 수치는 동일 조건으로 측정된 것만 쓴다. 정적 패턴 감소를 버그 해결률이나 성능 개선률로 바꾸지 않는다.
- 재현 명령, 환경, commit, 입력/설정/asset DB 식별값, 결과 로그를 연결한다.
- 체크섬/네트워크/성능 미검증은 명시한다. SDK·아트 제작자의 기여와 프로젝트 작성자의 기여를 분리한다.
- App ID, 인증정보, 개인 경로가 들어간 로그를 공개 문서에 그대로 복사하지 않는다.

완료된 작업이 있으면 `docs/ai/evidence/<date>-<topic>.md`를 생성한다.
진행 중 계획은 계획으로 표시한다. 이 초기 세팅만으로 '게임 결정론 검증 완료' 사례를 만들지 않는다.
