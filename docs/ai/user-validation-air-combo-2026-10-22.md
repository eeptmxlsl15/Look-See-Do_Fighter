# 공중 피해·사용자 재베이크 테스트 완료와 로컬 커밋

확인 시각: 2026-10-22T17:11:00+09:00. 기준 HEAD `97b6acb020a672193d44053edc955d109e8759b7`, 브랜치 `dev/Prototype`.
사용자가 **“오케이 테스트는 내가 다 했으니 커밋만해줘”**라고 확인했다.
사용자의 최신 직접 테스트 완료 보고를 근거로 현재 관련 변경을 로컬 커밋한다.
자동 게임 검사·실패한 통합 검사를 재실행하지 않고 과거 결과를 보존한다. Push/PR은 요청받지 않았다.

## 커밋 범위

- 공중 후속타 70/50/30%의 Health/Combat/프로토콜 C# 3개와 기존 테스트 2개.
- 사용자가 수정한 Enemy Controller, Rp_Enemy의 KazumiHoming_Enemy clip, Player/Enemy Quantum 그래프.
- 이미 삭제되어 있던 중복 Rk_Enemy/KazumiHoming_Enemy의 폴더 메타·clip/controller·PNG와 메타 75개.
  기존 삭제를 Git에 기록하며 추가 파일 삭제를 수행하지 않는다.
- 선행 원인 조사/구현 문서와 각각의 자동 증거, 사용자 확인 기록, 현재 상태.

IDE 생성 파일 수정 9개와 미추적 csproj 4개는 제외·보존한다. 기존 staged 항목은 없었다.
사용자 게임 코드/에셋의 동작 값은 이 확인 이후 수정하지 않는다. 새로 추가된 clip의 빈 path 줄 끝 공백 1곳만 제거했다.

## 현재 소스와 사용자 재베이크 검토

피해 C# 3개와 체력 테스트는 앞선 최종 검사 해시와 같고, Frame 테스트는 보정 후 해시
`6551809b5f6af82ef894e0675e0464d97ae3f3d4a478bee7b237427b5e96058f`와 같다.
실제 테스트한 자동 후보와 이후 사용자가 확인한 후보를 구분한다.

사용자 clip은 12→60 FPS, 키 간격 1/60초, 길이 약 0.5833초로 변경됐다.
Enemy Controller/그래프의 clip GUID는 현재 Rp_Enemy clip `765337c05fdbc284bb3d5b81e3d4fe2a`로 바뀌었다.
Player 그래프는 RID 정규화 비교에서 의미 차이가 없으며 재베이크 직렬화 ID만 바뀌었다.
Enemy 그래프는 clip 참조 외에 KazumiHoming의 LoopTime 0→1과 기존 공격 이벤트 제거가 있다.
현재 clip의 m_Events 및 베이크된 KazumiHoming 이벤트 목록은 비어 있다.
사용자 확인 후 이를 자동 복구하지 않는다. 따라서 공격 이벤트 복구 완료로 기록하지 않으며 이 상태를 후속 주의점으로 남긴다.
새 자동 물리 접촉/기술 피해/전체 매치 재현은 수행하지 않았다.

## 검증 근거 구분

| 근거 | 상태 |
|---|---|
| 사용자 직접 테스트 | 완료 보고 수신, 현재 상태의 로컬 커밋 명시 요청 |
| 앞선 정적 정책 | PASS — 94개, 신규 0. 이후 Simulation C# 변경 없음 |
| 앞선 자동 컴파일/회귀 | 컴파일 PASS, 110개 중 109 PASS/1 FAIL. 테스트 보정 후 미검증 이력 유지 |
| 사용자 재베이크 에셋의 자동 게임 검사 | NOT RUN — 사용자 완료 보고와 구분 |
| 이번 자동 게임 재검사 | 실행하지 않음 |
| 전체 replay/강제 온라인 rollback | 기존 NOT RUN 유지 |

테스트 횟수·명령·상세 로그는 제공되지 않았다. 사용자 보고를 자동 PASS나 결정론 측정으로 바꾸지 않는다.
[원인 조사](homing-air-damage-investigation-2026-10-22.md), [구현](air-combo-damage-scaling.md),
[자동 원본](evidence/air-combo-damage-scaling-validation.json)을 함께 보존한다.

로컬 커밋만 요청되어 원격 인증/fetch/Push/PR은 수행하지 않는다.
origin fetch/push 대상이 기존 eeptmxlsl15/Look-See-Do_Fighter임을 읽어 확인했다.
preflight 이력 기준은 로컬 origin/dev/Prototype이며 원격 최신화 근거로 표현하지 않는다.
커밋 후보의 비밀 패턴/불필요 파일·공백과 대형 그래프 별도 검토 결과를 아래에 기록한다.
실제 커밋 SHA는 이 문서를 포함하는 Git 로그로 확인한다.

## 로컬 후보 검사 결과

- 명시적 staging 90개 경로: 새/수정 15개와 기존 삭제 75개. IDE 항목 없음, 기존 staged 항목 없음.
- staged 공백 PASS(종료 0), 문서 상대 링크 63개 누락 0, 증거 JSON 2개 파싱 PASS.
- preflight 원본은 종료 1/BLOCKED: 새·수정 15개 중 blob 13개 검사, 기존 로컬 base와 HEAD가 같아 미게시 이력 0개.
  발견은 두 그래프의 LARGE_FILE_NOT_SCANNED뿐이다. 원본 판정을 PASS로 바꾸거나 검사기를 재실행하지 않았다.
- 두 그래프는 UTF-8 Unity YAML이다. 전체 텍스트를 별도 읽어 알려진 비밀 패턴 적중 0을 확인했고,
  RID 정규화 후 의미 diff를 검토했다. 제한값/검사기를 변경하지 않는다.
  Enemy 2,161,877바이트/SHA256 `83a3b0ab0207843f892f3cbb8d86efb547362f32e70bf467ed2dc840660d6d37`,
  Player 2,162,820바이트/SHA256 `9e085614ea1cecc28c80a276a700567bb7ed82f668497e2e7bf23c5cc042796a`.
- 사용자 테스트 완료와 명시적 로컬 커밋 요청, 별도 대형 텍스트 검토를 근거로 해당 사용자 재베이크를 포함한다.
  자동 게임 FAIL/NOT RUN/테스트 보정 후 미검증은 그대로 남긴다. 원격 게시 승인으로 사용하지 않는다.
- 검사 후 제품 코드/에셋은 변경하지 않고 이 결과 설명과 요약만 기록했다. 게임 검사·preflight·공백 검사를 재실행하지 않는다.
