# HP 0 승패와 종료 자세

> 아래는 최초 단일 대전 종료 구현의 이력이다. 현재 동작은 [5판 3선승 및 자연스러운 앉기](best-of-five.md)가 대체한다.
> 지금은 3승 이전 KO 뒤 다음 라운드로 진행하며, 패자는 `Sit Enter` 애니메이션을 거쳐 앉는다.

## 동작

HP가 0 이하가 된 tick의 기존 마지막 `LSDF_IngameSystem`에서 승패를 한 번 확정한다.
치명타는 HP를 즉시 0으로 제한한다. 패자는 Quantum Animator의 `Siting`, 승자는 `Idle`로 전환하고
두 캐릭터의 이동·공격·선입력·추가 피격을 잠근다. 이전의 HP 1 복구/자동 회복/210tick 뒤 재개는 하지 않는다.
공중에서 끝나면 상승을 취소하고 중력으로 내려오게 한다. 수평 이동과 남은 히트박스는 제거한다.

동시 KO는 무승부로 양쪽이 앉고 승수는 올리지 않는다. 단일 KO는 패자의 `loseRound`를 딱 한 번 증가시킨다.
한 번의 대전은 이 결과 상태에 머문다. 재대전 버튼, 자동 다음 라운드, 3선승/시간 종료/이탈 승패는 이번 범위가 아니다.
새 Quantum 게임을 시작하면 스폰 시 HP와 상태가 초기화된다. 같은 게임의 중복 AddPlayer는 재대전 명령이 아니다.

HUD는 `VerifiedFrame` 결과로 기존 Round/Fight 메시지 자리에 `YOU WIN`, `YOU LOSE`, `DRAW`를 표시한다.
Unity Animator나 UI가 판정 권위가 되지 않는다. 연출의 0.10초 전환은 설치된 Quantum Animator SDK의 기본 FadeTo를 사용한다.

## 파일

| 경로 | 역할 |
|---|---|
| `Assets/QuantumUser/Simulation/LSDFPlayerHealth.cs` | HP 0 제한, KO/종료 후 피해·회복 금지. 기존 콤보 보정은 보존 |
| `Assets/QuantumUser/Simulation/LSDFRoundPolicy.cs` | 순수 종료/승패/동시 KO/자세 정책 |
| `Assets/QuantumUser/Simulation/LSDF_IngameSystem.cs` | 기존 순서에서 결과 확정, Quantum FadeTo/IgnoreTransitions, 입력/히트박스 정리 |
| `Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs` | 종료 후 입력·이동 금지, 자동 회복 트리거 제거 |
| `Assets/QuantumUser/Simulation/LSDF_CollisionSystem.cs`, `LSDFContactPolicy.cs` | 종료 캐릭터에 새 피격/가드 반응 금지 |
| `Assets/QuantumUser/Simulation/LSDFInputBuffer.cs` | 종료 후 잠금 시간이 지나도 대기/전달 명령을 폐기 |
| `Assets/QuantumUser/View/LSDF_IngameUI.cs` | 확정된 로컬 승패 표시와 수명 정리 |
| `Assets/Tests/Editor/LSDFRoundResultTests.cs` | 양쪽 KO, 동시 KO, 결과 1회 확정, 자세/입력/회복 불변식 |
| `Assets/Tests/Editor/LSDFPlayerHealthTests.cs`, `LSDFStructuralTests.cs` | 의도적으로 변경된 HP/라운드 계약 회귀 |
| `Assets/Tests/Editor/LSDFOnlineSmokeTests.cs` | 실제 Photon 2클라이언트에서 KO와 240tick 유지, 기록 재생/Frame 복원 |
| `tools/ai/implement_ko_result.py` | CP949 기존 PlayerSystem의 관련 부분만 기계적 수정한 1회성 기록 |

기존 Frame 컴포넌트 `playerHp/isRoundEnd/loseRound/isSit`를 이용하므로 이 기능 자체는 QTN 스키마/CodeGen 변경이 없다.
SDK/그래프 에셋/시스템 등록 순서는 수정하지 않는다. 프레임을 넘는 승패 상태를 static/시스템/공유 에셋에 두지 않는다.
동작이 바뀐 이전 클라이언트와 방이 섞이지 않게 프로토콜 접미사는 `lsdf-loadout-v4-ko-result`다.
기존 회복용 GameConfig 필드는 직렬화 호환을 위해 보존하지만 자동 다음 라운드에는 사용하지 않는다.
사용자가 조정한 선입력 에셋 값 20은 보존했다(새 설정 객체의 초기값은 10).

## 검증 범위

선입력 커밋 검증의 실패와 이 기능의 검증을 분리한다. 선입력 기본값 테스트의 수정 후 재실행은 하지 않는다.
HP 기능 구현을 끝낸 뒤 Unity 컴파일, HP/라운드/구조/수정한 온라인 검사, Quantum 정적 검사와 diff를 한 차례 실행한다.
온라인 선입력 probe는 이제 KO 전에 실행한다. KO 뒤에는 자동 회복이 없으므로 이전처럼 KO 뒤 probe를 시작하면 안 된다.
온라인에서는 실제 입력으로 치명타를 내고, 양쪽의 KO tick/HP/Quantum 자세/체크섬을 비교한다.
대기 입력이 있는 Frame에서 SDK ResetReplay 후 재시뮬레이션하여 KO까지 같은 checksum을 유지하는지 확인한다.

게임 화면/UI 육안 검사, 재접속/지터 강제 주입, 독립 실행 빌드와 교차 플랫폼 검사는 별도로 실행하지 않으면 NOT RUN이다.
검증 결과는 구현 후 기록한다. 선입력 커밋은 실패로 보류되었고 새 HP 기능도 아직 커밋/Push하지 않는다.

## 2026-10-16 최종 결과

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | PASS: 오류 0. 기존 SDK/obsolete/에셋/MCP 경고 8건. 제외 에셋의 경고는 노출됐지만 소스는 열지 않음 |
| Quantum 정적 정책 | PASS: Simulation 91개, 기존 0·신규 0. 의미적 결정론의 증명은 아님 |
| HP/종료 단위 검사 | PASS: Health 24개, RoundResult 11개 |
| 구조 회귀 | PASS: Structural 20개. 실제 그래프 자세 이름/전환 허용 및 씬 바인딩 포함 |
| Photon 온라인/재생 | PASS: 1개. 두 클라이언트에서 KO tick **2885**, PlayerRef 0/1 HP **0/20**, 패자 앉기/승자 서기. 종료 뒤 최소 240 verified tick 유지. 두 기록 재생과 대기 버퍼 Frame 복원 후 KO까지 checksum 비교 통과 |
| 선입력 실패 테스트 수정본 | NOT RUN: 별도 요청 없이 자동 재실행하지 않음. HP 검사 통과로 이 항목을 PASS 처리하지 않음 |
| diff | SDK 생성 코드의 기존 `unchecked { ` 줄 끝 공백 1건. Generated 직접 수정 금지에 따라 보존 |
| UI 육안/독립 빌드/실제 재접속/강제 네트워크 rollback | NOT RUN |
| 커밋/Push | 미실행. 선입력 커밋 검증의 수정 후 미검증 항목 때문에 게시 절차를 재개하지 않음 |

HP 기능 검증은 총 **56 PASS / 0 FAIL / 0 SKIP**, 64.71초다. 선입력 최초 전체 검사 137개 중 1개 실패 이력은
[별도 기록](evidence/input-buffer-commit-validation.json)에 보존했다. 이번 대상은 변경된 HP/라운드/구조/온라인 검사이며 전체 회귀 통과라고 주장하지 않는다.
온라인 선입력도 입력 tick 398 → 전달 tick 406으로 관측했다.
검사 종료 후 활성 씬은 `LSDF_Menu`, `isDirty=false`다. 기존 IDE 생성 파일은 수정/삭제/스테이징하지 않았다.
[검증 증거와 소스 해시](evidence/ko-result-validation.json).
