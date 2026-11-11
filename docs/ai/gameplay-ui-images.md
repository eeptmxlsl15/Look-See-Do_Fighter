# Inspector로 게임플레이 UI 이미지 연결

기준: `dev/Prototype`, `b40aea428812a98bb9d516e32434d5db5329fd51`. 작업 종류: UI/연출 기능 추가.

## 사용 방법

1. `Assets/Scenes/LSDFGameplay.unity`를 연다.
2. Hierarchy에서 `QuantumEntityViewUpdater`를 선택하고 `LSDFSceneBindings` Inspector 맨 위의 **게임플레이 UI 이미지**를 펼친다.
3. 아래 슬롯에 준비한 Sprite를 드래그한다. PNG Import Settings의 Texture Type은 **Sprite (2D and UI)**로 설정한다.
4. **이미지 적용 / UI 연결 / 미리보기**를 누른 뒤 씬을 저장한다. 중앙 알림 미리보기에서 Round 1~5/Fight/승패를 골라 확인할 수 있다.
   실제 대전은 확정 Frame의 기존 라운드 상태를 따르므로 미리보기 선택이 경기 결과에 영향을 주지 않는다.

| 슬롯 | 적용 |
|---|---|
| 체력바 배경 | 양쪽 HUD의 좌·우 체력바 배경 4개에 공통 적용 |
| 체력 채움 (선택) | 기존 체력 비율에 따라 가로로 줄어드는 이미지. 비워 두면 기존 그림 유지 |
| 승리 아이콘 배경 | 양쪽 HUD의 좌·우 각 3칸에 항상 표시 |
| 승리 시 빨간 이미지 | 획득한 승리 수만큼 배경 위에 표시. 원본 색을 유지하도록 흰색 tint 적용 |
| Round1~Round5 | 현재 라운드의 준비 구간에 표시 |
| Fight | 기존과 동일하게 시작 전 마지막 60틱에 표시, 전투 시작 때 숨김 |
| RoundWin / RoundLose | 로컬 플레이어 기준 라운드 결과와 최종 매치 결과에 표시 |
| Draw (선택) | 동시 KO 이미지. 없으면 DRAW 텍스트 사용 |
| 중앙 이미지 표시 크기 | Canvas 단위 최대 영역. 기본 640×180, 원본 비율 유지 |

배경·체력 채움·아이콘 슬롯이 비어 있으면 해당 Image에 이미 연결된 그림/색을 유지한다.
중앙 알림은 각 메시지의 이미지가 없을 때만 기존 텍스트를 표시하며 이미지와 텍스트를 겹쳐 표시하지 않는다.
첫 적용 후 슬롯을 비우더라도 체력바/아이콘에 이미 적용한 그림을 자동 삭제하지 않는다. 완전히 제거하려면 해당 UI Image의 Source Image를 비운다.
알림은 마지막 결과 이미지를 남기지 않고 라운드 시작/전투/재활성화/소유권 해제에 따라 갱신·숨김 처리한다.
준비된 실제 아트는 이번 요청에 파일/경로로 지정되지 않았으므로 임의로 연결하지 않고 슬롯을 비워 두었다.

## 변경과 경계

- [공통 설정과 표시](../../Assets/QuantumUser/View/LSDFSceneBindings.GameplayUI.cs): 양쪽 레이아웃 공유 Sprite 슬롯, 기존 배경 참조 재사용, 중앙 Image 1개씩 재사용.
- [씬 바인딩](../../Assets/QuantumUser/View/LSDFSceneBindings.cs): 기존 Hud에 체력/승리 배경과 중앙 Image 참조를 추가했다.
- [UI 업데이트](../../Assets/QuantumUser/View/LSDF_IngameUI.cs): HP 비율은 기존 predicted 경로, 승리 수·이미지 알림은 verified 경로를 유지한다.
  로컬 플레이어를 첫 인수로 전달하므로 2P도 내 승리는 왼쪽, 상대 승리는 오른쪽으로 표시한다.
- [Inspector](../../Assets/QuantumUser/View/Editor/LSDFSceneBindingsEditor.cs): 이미지 슬롯을 상단에 모으고 양쪽 공통 적용/미리보기/Undo를 연결했다.
  Editor asmdef에 실제 사용한 UI/TMP 참조를 추가했다.
- [경기 씬](../../Assets/Scenes/LSDFGameplay.unity): 기존 배경/아이콘 레이어를 연결하고 `Round Text` 아래 `Announcement Image`를 1개씩 추가했다.
  기존 텍스트·체력 흔들림 부모·레이어·바닥·카메라·배경·Quantum updater 참조는 유지한다.

Simulation/QTN/Generated/SDK/시스템 순서/경기 타이밍/체력·승패 규칙/매칭 프로토콜은 바꾸지 않았다.
Unity View 표현만 변경하므로 새 네트워크 입력이나 Frame 상태는 필요하지 않다.

## 최종 한 차례 검증

[검증 기록](evidence/gameplay-ui-images-validation.json)에 실제 실행 결과를 남긴다.
새 [UI fixture](../../Assets/Tests/Editor/LSDFGameplayUIImageTests.cs)는 Round 1~5→Fight→전투 숨김, 양쪽 로컬 승패와 매치 종료,
부분 미할당/무승부 텍스트 대체, 두 HUD 공통 아트·HP fill/방향 보존, 아이콘 초기화, 재진입/이미지 재사용·크기/비율과 실제 씬 참조를 검사한다.
기존 라운드 결과·체력 흔들림·씬 바인딩 사례를 같은 job에 포함한다.
Unity 컴파일/콘솔·Quantum 정적 정책·관련 diff/새 파일 공백·문서 링크/메타 짝을 한 번 확인한다.
정적 PASS를 결정론 증명으로 표현하지 않는다. 실제 사용자 아트의 화면 품질·Photon 대전·강제 rollback은 이 UI 검사와 구분한다.

기존 IDE 9개·미추적 csproj 4개·포트폴리오 자료/기록·output/tmp/pptstyle과 이전 current-state 결과 갱신을 보존했다.
이전 자동 실패·보정 후 미검증과 KazumiHoming 빈 공격 이벤트는 별도 이력이다. 커밋/Push/PR은 이번 요청 범위에 없다.

### 실행 결과

2026-10-23T00:55:32+09:00. job `dd4e620b64e7426e80e0e63d8cabeac9`: **39 PASS / 0 FAIL / 0 SKIP**, 한 차례 실행.
새 이미지 UI 16개, 기존 라운드 결과 19개, 기존 체력 흔들림/씬 바인딩 4개다.
Unity 컴파일 오류 0/기존 경고 7개, Quantum 정적 94개/신규 0, 관련 diff 공백과 새 파일 12개·문서 링크 7개·메타 3짝 PASS.
씬의 로컬 fileID 중복 없음과 실제 두 HUD의 배경/아이콘/중앙 이미지 참조를 확인했다.
소스 읽기용 최초 MCP scratch snippet은 return 누락으로 실패했으며 프로젝트 컴파일/게임 테스트와 구분한다. 참조 조사는 씬 원문 읽기로 이어갔다.
검사 후 제품 코드/씬/설정/메타는 변경하지 않았고, 문서와 결과 JSON만 갱신했다. 검사 반복은 없다.
열린 씬에 미저장 변경이 없는 것을 확인한 뒤 `LSDFGameplay`를 열고 `QuantumEntityViewUpdater`를 선택해 Inspector를 준비했다. 자동 저장하지 않았다.
실제 준비된 사용자 아트는 미할당이며 해당 아트의 화면 품질·Inspector 버튼 클릭/Undo의 자동 UI 검사·Photon 대전/강제 rollback은 NOT RUN이다.
