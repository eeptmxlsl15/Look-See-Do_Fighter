# 사용자별 기술 조합과 접속 종료 조사 — 2026-10-17

## 요청과 확인된 사실

사용자 보고: 같은 커맨드에 서로 다른 기술을 배합한 유저끼리 대전할 때 Photon 오류로 접속이 종료된다. 정확한 서버 메시지와 재현 조합은 확보되지 않았다. 세팅 불일치는 사용자 가설이며 확정된 원인으로 기록하지 않는다.

현재 소스에서는 메뉴의 `PrepareLSDFPlayers`가 로컬 저장값을 복사해 각 클라이언트의 RuntimePlayer.CommandSkills에 넣는다. Simulation은 `GetPlayerData(PlayerRef)`에서 수신한 값을 해당 엔티티의 28칸 CommandSkillMap으로 복사한다. CommandSystem은 그 엔티티의 맵을 사용한다. 로컬 PlayerPrefs를 Simulation에서 직접 읽거나 다른 플레이어와 기술 배열을 공유하는 경로는 이번 조사에서 찾지 못했다.

작업 시작 시 활성 씬은 LSDF_Menu, 미저장 변경은 없었다. 당시 콘솔에는 Input Manager 경고만 있었고 사용자가 경험한 접속 종료 오류는 남아 있지 않았다. 게임 결과에 관계되는 로직을 추측으로 바꾸지 않았다.

## 이번 변경

- `LSDFConnectionDiagnostics`: 메뉴 접속 완료 후에도 게임별 Photon plugin 종료·transport 종료·Quantum checksum 오류를 콘솔에 남긴다. startup 직후 기존 임시 listener가 해제된 뒤의 진단 공백을 보완한다. 연결 정리 시 listener를 해제한다. 로그 식별자는 `[LSDF Network]`이며 인증정보·AppId·플레이어 이름을 별도로 추가하지 않는다.
- `LSDFMenuLoadoutTests`: 실제 메뉴 설정 준비 메서드와 설치 SDK의 RuntimePlayer 직렬화를 거치는 72가지 선택 조합, 28개 그래프 경로, 독립적인 두 payload, 같은 matchmaking version, 재접속 시 제출한 세팅 보존을 검사한다. 테스트가 잠시 변경한 Skill_ 키의 존재 여부와 값을 finally에서 원복한다.
- `LSDFOnlineLoadoutTests`: 실제 Photon 두 연결이 동일 tick에 동일 커맨드를 입력한다. 서로 다르게 설정한 Neutral LP, Back LP, Down-forward RP, Forward RP, Down-back RK의 10개 공격 경로를 양쪽 세션에서 관측하고, 28칸 payload/Frame 상태와 verified checksum, 두 기록 재생·Frame 복원을 비교한다.
- 기존 5판 3선승 온라인 검사와 대기 버퍼 복원 assertion은 유지한다. 새 시나리오의 Frame 복원은 대기 버퍼가 있다는 조건 대신 서로 다른 로드아웃을 가진 경기 상태를 복원한다.

Simulation·QTN·공격 에셋·밸런스·시스템 등록 순서는 변경하지 않았다. 기존 IDE 생성물과 포트폴리오 문서는 보존했다.

## 최초 최종 검증

| 검사 | 결과 | 범위 |
|---|---|---|
| quantum_guard | PASS | Simulation C# 91개, 기존·신규 적중 0. 의미적 결정론 증명 아님 |
| 변경 diff / 새 스크립트 공백·meta 짝 | PASS | 당시 존재 여부 확인. GUID 길이 검사는 포함하지 못함 |
| Unity 컴파일 | FAIL | 새 테스트 두 파일의 meta GUID가 33자리여서 Unity가 파일을 인식하지 못함. 기존 테스트에서 새 fixture/partial 메서드를 찾지 못하는 CS0103 7건 |
| meta 수정 | 수정 후 미검증 | 두 신규 GUID를 32자리로 수정. 기존 에셋 GUID 변경 없음 |
| 신규·기존 Unity 테스트 / Photon / 재생 | NOT RUN | 컴파일 실패로 테스트를 시작하지 않음 |

실패 후 자동 재실행하지 않고 당시에는 사용자 답변 대기 상태로 중단했다. 최초 실패 이력은 후속 결과와 구분해 보존한다.

## 후속 게시 요청의 검증

이후 체력바 연출과 사용자 강도 조정까지 포함한 새 Push 요청에 따라 후보 전체를 한 차례 검증했다. job `537a3260b7b340fb82c01bab7c1e94e7`, **235 PASS / 0 FAIL / 0 SKIP**다.

- 메뉴·직렬화·그래프 검사 74개 통과: 72가지 선택 조합, 두 독립 payload, 재접속 제출 설정 유지.
- 서로 다른 로드아웃의 실제 Photon 2클라이언트 검사 통과: 다섯 커맨드를 같은 tick에 입력하고 서로 다른 10개 공격 경로를 양쪽 세션에서 관측했다.
- 양쪽 관측 tick은 Neutral LP 199, Back LP 559, Down-forward RP 919, Forward RP 1279, Down-back RK 1639로 일치했다. 공통 verified tick 67개의 checksum 비교가 통과했다.
- 두 입력 기록 재생과 Frame 복원·재시뮬레이션 assertion도 포함된 온라인 검사 전체가 통과했다. 기존 5판 3선승 온라인 검사도 별도로 통과했다.
- Unity 컴파일 오류 0, Quantum 정적 정책 91개 파일·신규 0. Simulation/QTN/시스템 순서는 변경하지 않았다.

[이번 게시 후보 검증](evidence/health-shake-publish-validation.json)에 모든 테스트 이름·상태·실행기 시간과 핵심 관측 줄을 남겼다. 반복적인 게임 Debug.Log 전체는 복사하지 않았다. 테스트가 로컬 스킬 키를 임시 설정한 뒤 기존 값과 키 존재 여부를 복원한다.

이 시나리오에서 사용자별 세팅 차이로 인한 종료는 재현되지 않았다. 사용자 환경에서 경험한 최초 Photon 오류의 원인까지 확정하거나 해결했다고 표현하지 않는다. 진단 코드는 향후 실제 오류 이유를 남기기 위한 보완이다.

## 한계

현재는 **사용자 환경의 Photon 접속 종료 원인과 해결 여부를 확정하지 못했다**. 새 검사 작성·진단 보완을 근본 원인 해결로 표현하지 않는다. 같은 Editor 프로세스의 두 Photon 연결은 독립 빌드·다른 컴퓨터의 에셋/바이너리 차이까지 검증하지 않는다. 실제 메뉴 UI와 씬 전환 전체도 새 payload 준비 검사와 구분한다.

후속 실행에서 오류가 발생하면 `[LSDF Network]`의 PluginDisconnect/TransportDisconnect/ChecksumError, tick과 reason을 기준으로 원인을 좁힐 수 있다. 사용자별 기술 선택은 다를 수 있지만 각 클라이언트의 게임 코드·Quantum SDK·공유 판정 에셋은 같은 기준이어야 한다.
