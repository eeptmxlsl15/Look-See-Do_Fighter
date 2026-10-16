---
name: lsdf-quantum-verify
description: Validate Look-See-Do Fighter Quantum 3 changes with source policy checks, code generation, Unity compilation, targeted combat scenarios, and replay or rollback evidence. Use for verification and regression review, not automatic gameplay fixes.
---

# LSDF 검증

루트 `AGENTS.md`와 [검증 매트릭스](../../../docs/ai/validation.md)를 읽는다.
검증 요청은 발견한 게임 버그의 자동 수정 권한이 아니다.

1. `git diff --stat`와 변경 파일을 확인한다. 사용자가 만든 기존 변경과 이번 작업을 구분한다.
2. 작업 단위별 검증을 구현 완료 후 **한 차례만** 수행한다. 이미 실행했다면 결과를 재사용하고 추가 실행하지 않는다.
   변경에 필요한 검사만 미리 고른다. 시뮬레이션 변경에는 `python tools/ai/quantum_guard.py check`를 한 번 포함한다.
   실패의 파일·행·규칙을 보고하고 기준선을 늘려 통과시키지 않는다. 기존 156건은 초기 스냅샷이지 합격 판정이 아니다.
3. 코드 변경에는 최종 검증에서 unityMCP `read_console(action="get", types=["error","warning"])`로 컴파일 결과를 한 번 확인한다.
   콘솔을 지우지 않는다. `.qtn` 변경은 설치 버전의 생성 메뉴/자동 생성 설정을 확인한 뒤 CodeGen과 컴파일 결과를 확인한다.
4. 관련 최소 시나리오를 실행한다. 프레임 타이밍/공유 상태/전달 데이터 변경은 두 플레이어와 롤백 재현을 포함한다.
5. 동일 빌드·설정·asset DB·seed·입력 스트림에서 verified-frame 체크섬을 비교한다.
   서로 다른 버전/스키마 사이의 raw 체크섬 불일치를 곧바로 버그로 판단하지 않는다.
6. 실패 시 반복 검사하지 않는다. 가능한 범위의 수정 후에는 `수정 후 미검증`으로 보고한다.
   재실행은 사용자의 별도 요청이 있을 때만 한다.
   Unity 미접속, 라이선스/테스트 실행환경 부재, 입력 재생 fixture 부재는 실제 제한으로 기록한다.
   테스트가 발견되지 않았거나 실행되지 않았으면 PASS라고 쓰지 않는다.

보고 형식: `검사 | 조건/명령 | PASS/FAIL/NOT RUN | 로그/증거 위치`.
정적 검사, 컴파일, 기능 회귀, 결정론, 성능을 분리한다. 성능은 실제 Profiler 측정이 있을 때만 비교한다.
새 테스트는 실제 관측 가능한 불변 조건을 검증한다. 단순 코드 문자열 일치 테스트로 전투 테스트를 대체하지 않는다.
