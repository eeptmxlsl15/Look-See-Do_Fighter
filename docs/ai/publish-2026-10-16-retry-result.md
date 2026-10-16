# 실패한 온라인 검사 한 차례 재실행

2026-10-16. 사용자 지시: 실패한 테스트만 한 번 더 실행하고 통과하면 Push.
이 지시를 기존 `origin/dev/Prototype`의 현재 변경 게시 승인으로 적용한다. PR 생성은 요청 범위에 포함하지 않는다.

## 결과

- `LSDFOnlineSmokeTests.TwoClientsAgreeOnLoadoutCombatAndRepeatedAddPlayer` 한 건만 실행했다.
- job `5849f5d45562426b92bd6fa4d9f36292`: **1 PASS / 0 FAIL / 0 SKIP**, 테스트 결과 duration 30.3435037초.
- 게임/테스트 소스를 수정하지 않았다. 앞선 컴파일·단위/에셋 32건·Quantum 정적 검사 성공 결과를 재사용했다.
- 테스트는 실제 Photon 2클라이언트, 서로 다른 로드아웃, 피해·잽 연계·LeftUpper·Narak/Lars 다단히트,
  반복 AddPlayer와 공통 verified-frame checksum을 검사한다. 강제 rollback/실제 재접속/UI/독립 빌드는 검사하지 않는다.
- Unity는 Play Mode 종료 후 LSDF_Menu로 복귀했다.

[재실행 원본 결과](evidence/publish-2026-10-16-retry-tests.json).
[앞선 실패 기록](publish-2026-10-16-result.md)과 원본 결과를 그대로 보존했다.
이번 한 번의 성공으로 이전 종료 정리 취소 오류의 근본 원인이 해결됐다고 주장하지 않는다.
33개 전체를 다시 실행한 결과가 아니라, 기존 32건 PASS와 실패했던 1건의 새 PASS를 결합한 근거다.

## 게시 후보

P0~4차 Quantum 결정론/공격 정의 리팩토링, 관련 Unity 테스트·LUT 구성, AI 운영 문서,
publish-unity-pr Skill과 읽기 전용 검사기, 작업/검증 근거를 포함한다.
루트의 자동 생성 `.csproj`/`.sln` 변경은 로컬에 보존하고 커밋에서 제외한다.
기존 이력과 origin을 유지하고 main으로 직접 Push하지 않는다.

이 문서는 온라인 결과와 게시 범위를 기록한다. 실제 커밋 SHA·Push 성공 여부는 Git 결과로 확인한다.

게시 직전 읽기 전용 비밀정보 검사: `origin/dev/Prototype` 대비 기존 미업로드 커밋 0개,
index 108개 blob 검사, 알려진 패턴/미검사 항목 0건 (`NO_KNOWN_FINDINGS`).
이는 정규식 기반 보조 결과이며 비밀정보 부재의 보증으로 표현하지 않는다.

`git diff --cached --check`는 Unity 생성 YAML 빈 필드의 후행 공백 13건을 보고해 종료 코드 2를 반환했다.
대상은 QuantumLookupTables의 빈 m_EditorClassIdentifier 및 새 meta의 빈 userData/assetBundle 필드다.
Unity 기본 직렬화 표기이며 값·참조·실행 코드에 영향을 주지 않는 서식 항목으로 검토해 보존했다.
이 서식 검사를 PASS로 바꾸어 기록하지 않는다. 컴파일/전투/결정론/비밀정보 검사 실패는 아니다.
기존 소스 또는 테스트 로직은 변경하지 않았고 추가 테스트를 실행하지 않았다.
