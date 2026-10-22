# 3타일 재사용 방식의 좌우 무한 배경

작성: 2026-10-22, Asia/Seoul. 기준 `dev/Prototype`, HEAD `b571a8f15e617788a0c86fe6507e992146f1800c`.

## 요청과 완료 조건

기존 맵을 가운데·왼쪽·오른쪽의 3장으로 이어 놓고, 이동하면서 화면 밖 배경을 반대쪽으로 넘겨 무한히 이어지는 느낌을 만든다.
멀어지는 두 캐릭터를 따라 카메라가 줌아웃해도 가로 배경의 끝이 노출되지 않아야 한다.
이는 배경 View 작업이며 Quantum 지형 충돌·벽·판정의 크기나 경기 규칙은 바꾸지 않는다.

## 구현

- [LSDFStageBackground](../../Assets/QuantumUser/View/LSDFStageBackground.cs)의 `Repeat Horizontally`를 기본으로 켰다.
  카메라별 중심·좌·우 3개, 두 카메라 합계 6개 렌더러만 재사용한다.
- 사용자가 이미 만든 [경기 씬](../../Assets/Scenes/LSDFGameplay.unity)의 복제 배경 4개를 새 필드에 연결했다.
  사용자 중심 이미지·높이·스케일·카메라·레이어와 기존 참조는 유지한다. 열린 씬이 코드 리로드를 거친 경우에도
  같은 Sprite·레이어·이름의 자식 복제본을 먼저 채택하므로 새 오브젝트가 중복 생성되지 않는다.
- 카메라 뷰포트를 캐릭터 평면으로 역투영해 가로 범위를 구하고, 타일의 실제 이미지 폭으로 위치를 결정한다.
  논리 인덱스를 3개 렌더러에 순환 배정한다. 경계를 넘을 때 아직 보이는 두 타일은 같은 월드 위치에 남고,
  화면 밖 한 타일만 반대쪽으로 이동한다. 큰 이동·역방향 전환도 반복 생성이나 긴 이동 루프 없이 처리한다.
- 화면 폭이 타일 2장보다 넓으면 한 렌더러 내부를 `SpriteDrawMode.Tiled / Continuous`로 가로 반복한다.
  원본 이미지의 월드 크기와 기존 줌 비율을 유지하면서 3개 렌더러의 범위를 확장한다.
  반복 수는 홀수로 맞춰 확대/축소 전후 같은 지점에 같은 이미지 부분이 남게 한다.
- [기존 평면 줌 투영](stage-background-flat-zoom.md), 피벗 보정, 2P 반전, 6/7번 레이어 격리와 Ground 그림 복원을 유지한다.
  이미지 제거·카메라 누락·비활성화 시 해당 타일을 숨기고 필요한 Ground 표시를 복원한다.
- 설정이 없는 다른 씬에서만 부족한 좌우 렌더러를 `DontSave` 자식으로 만든다. 재활성화 때 재사용하며,
  컴포넌트 제거 때 생성한 자식만 정리한다. 사용자가 배치한 타일은 삭제하지 않는다.
- 연결된 `산속 목재소 격투장 풍경_50_export.png.meta`의 `spriteMeshType` 한 항목만 Full Rect로 변경했다.
  이는 Tiled 모드의 직사각형 반복을 위한 설정이다. PNG 픽셀·필터·PPU·압축·GUID는 바꾸지 않았다.
  [Inspector 안내](../../Assets/QuantumUser/View/Editor/LSDFStageBackgroundEditor.cs)에도 반복과 Full Rect 설정을 설명했다.

무한 반복은 가로 방향이다. 위아래로 맵을 반복하거나 줌아웃을 상쇄하도록 이미지를 확대하지 않는다.
원본 그림 양 끝의 색·형상이 서로 맞는 정도는 원본 아트에 따른다.

## 사용

`LSDFGameplay → StageBackground → Repeat Horizontally`가 켜진 상태로 사용한다.
`Background Sprite` 하나를 바꾸면 모든 타일에 반영된다. 새 이미지의 Mesh Type은 Full Rect를 사용한다.
기존 `Minimum Height`, `Offset`, `Fit Reference View`, `Mirror Player2` 설정을 계속 사용할 수 있다.

## 한 차례 최종 검증

[배경 fixture](../../Assets/Tests/Editor/LSDFStageBackgroundTests.cs)의 기존 13개와 새 16개를 함께 실행한다.
새 사례는 양쪽 시점 × 원근/직교의 연속 이동·역방향·1000타일 이동, 경계에서 화면 밖 한 타일만 재배치,
큰 줌아웃의 이미지 크기 보존, 기존 복제본 채택, 삭제/재진입 수명, 양쪽 타일 경계의 실제 GPU 픽셀 순서를 검사한다.
Unity 컴파일/콘솔·정적 정책·변경 공백·문서 경로·기존 Map/PNG 보존 확인을 같은 최종 묶음에 포함한다.
실제 Photon 대전·사용자 아트의 이음새 육안·강제 rollback은 이 fixture의 통과와 구분한다.

결과는 [이번 검증 기록](evidence/infinite-stage-background-validation.json)에 추가한다.
이전 공중 피해 109 PASS/1 FAIL·보정 후 미검증 및 KazumiHoming 빈 공격 이벤트 상태는 이번 작업으로 바뀌지 않는다.

### 실행 결과

| 항목 | 결과 |
|---|---|
| 정적 정책 | PASS — Simulation 94개, 신규 적중 0. 결정론·런타임 증명은 아님 |
| 관련 코드 공백 | PASS — View/Inspector/테스트와 연결 이미지 메타에 한 차례 실행 |
| 별도 C# 컴파일 | PASS — 설치된 Unity 6000.3.26f1 참조 DLL과 Roslyn 4.12 사용, View 20소스/Inspector 1소스/배경 테스트 1소스 모두 종료 0 |
| Unity refresh | 실행 FAIL — `no_unity_session`, 연결된 인스턴스 0개 |
| Unity 최종 컴파일·콘솔 | NOT RUN — read_console도 `no_unity_session`. 현재 Editor 로그와 DLL은 최종 수정 전 자료라 통과 근거에서 제외 |
| 배경 29개 실행 사례 | NOT RUN — 유효한 에디터 세션이 없어 job을 시작하지 않음. 코드가 컴파일됐다는 사실과 실행 성공은 다름 |
| 실제 대전·PNG 이음새·강제 rollback | NOT RUN |

에디터 프로세스가 이미 실행 중임을 확인했으며 종료하거나 같은 프로젝트로 새 batchmode를 열지 않았다.
대신 기존 프로젝트 파일을 읽기만 하여 별도 `tmp/infinite-stage-background/`에 컴파일했다.
새 패키지를 설치하거나 게임 DLL·IDE 프로젝트·시스템 설정을 덮어쓰지 않았다.
한 번 성공한 검사와 실패한 MCP 동작을 재실행하지 않았다. 원격 게시와 자동 커밋은 하지 않는다.

## 기존 변경과 범위

시작 당시 씬 SHA256: `322e43706087eb30fcb9de3a7997a844de23e6e838e8613a832b2599395de0db`.
실제 사용자 씬에는 앞선 문서 작업 이후 배경 복제 4개 및 Sprite GUID `4d6e4726a819aba4a9d8a5aa95ee16dc`가 추가되어 있었다.
이 상태를 기준으로 새 반복 설정과 기존 타일 참조 5개만 추가하며 씬 전체를 재저장하지 않는다.
기존 IDE 9개·미추적 배경 PNG/메타 4개·csproj 4개·포트폴리오/빌드/기록·`pptstyle.md`는 보존한다.
Simulation/QTN/Generated/SDK/시스템 등록/카메라 추적과 줌·흔들림 코드는 변경하지 않는다. 커밋·게시 요청은 없다.
