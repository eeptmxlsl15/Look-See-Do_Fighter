# 양쪽 카메라용 긴 도트 배경 — 2026-10-21

기준: `dev/Prototype`, HEAD `debf379c006e40a9a4e00884db3d27bfd111a4d9` (`Assets`).
이전 상태 문서의 ee3628ba 이후 사용자가 효과 시트들을 커밋한 상태임을 Git으로 확인했다.
요청: 사용자가 받은 PNG를 직접 연결할 수 있도록 게임 씬에 배경 표시를 구현한다.

2026-10-22 수정: [평면 배경 줌 동기화](stage-background-flat-zoom.md).
카메라가 멀어질 때 배경도 캐릭터와 같은 비율로 축소하도록 변경했다.
아래 초기 검증 결과는 과거 기록이며 수정된 코드의 통과 근거가 아니다.

## 연결 방법

1. PNG Import Settings에서 Texture Type=`Sprite (2D and UI)`, Sprite Mode=`Single`,
   Filter Mode=`Point (no filter)`, Compression=`None`으로 설정하고 Apply한다.
2. `Assets/Scenes/LSDFGameplay.unity`를 열고 루트 `StageBackground`를 선택한다.
3. `LSDF Stage Background → Background Sprite`에 스프라이트 하나를 넣는다.
   최초 구현은 슬롯을 비워 두었고 이후 사용자가 PNG를 연결했다. 새 PNG나 기존 이미지의 Import Settings를 자동 수정하지 않는다.
4. `Offset Y`로 그림 속 바닥과 캐릭터 발의 높이를 맞춘다. `Minimum Height`로 배경을 확대하고
   `Fit Reference View`는 기준 거리에서의 구도를 맞춘다. 이후 줌아웃 때는 배경도 작아진다.
   원본 비율을 유지하므로 일부 영역은 화면 밖으로 잘린다. 이미지 끝이 드러나면 기준 크기를 키운다.
5. `Mirror Player2` 기본 켬은 같은 랜드마크가 월드 좌우를 유지하도록 2P 화면에서 반전한다.
   양쪽에서 그림의 글씨 등을 같은 방향으로 보이게 하려면 끈다.
6. 바닥이 포함된 PNG는 `Hide Ground Visual` 기본 켬을 사용한다. 바닥 없는 PNG라면 끄면 된다.
   기존 Ground의 그림만 숨기고 GameObject/Quantum 충돌 컴포넌트는 유지한다.

## 구현과 보존 동작

- [LSDFStageBackground.cs](../../Assets/QuantumUser/View/LSDFStageBackground.cs): 명시적 두 카메라와 두 SpriteRenderer.
  스테이지 중심의 Z ±4에 각 카메라를 향하는 배경을 배치하고 하나의 Sprite를 공유한다.
- [커스텀 Inspector](../../Assets/QuantumUser/View/Editor/LSDFStageBackgroundEditor.cs): 이미지·설정 변경 시 편집 모드 갱신과 연결 안내.
  카메라·렌더러·기본 머티리얼 참조는 씬에서 미리 연결한다.
- 최초 구현은 각 뷰포트 모서리의 ray와 배경 평면을 교차해 필요한 크기를 매번 계산했다.
  현재는 [기준 거리의 구도만 포착하고 평면 투영을 보정](stage-background-flat-zoom.md)한다.
  동일한 가로/세로 배율과 설정된 여유를 사용한다.
  카메라 추적/줌/흔들림 뒤 실행하며 카메라 위치·렌즈·활성 상태·입력을 덮어쓰지 않는다.
  월드 중심은 고정하므로 좌우 이동에 따라 그림의 보이는 부분이 달라진다. 픽셀 단위 카메라 스냅/정수배 확대 기능은 추가하지 않는다.
- [게임 씬](../../Assets/Scenes/LSDFGameplay.unity)에 루트 `StageBackground`와 자식 두 개를 추가했다.
  기존 `Backgound_1`(6)/`Backgound_2`(7) 레이어를 사용한다. 1P는 기존 mask를 유지하고
  2P는 1P 전용 배경 레이어 6만 제외한다. 다른 렌더링/충돌 레이어는 바꾸지 않는다.
- 설치된 URP `Sprite-Unlit-Default` 머티리얼을 공유한다. 별도 머티리얼 생성이나 조명 의존을 추가하지 않는다.
- 이미지/머티리얼/카메라가 없으면 해당 배경을 숨긴다. 이미지 제거·컴포넌트 비활성화 시 Ground의 이전 표시 상태를 복원한다.
  편집 모드에서 숨긴 Ground의 복원 정보는 씬 저장·도메인 리로드를 위해 직렬화한다. 매 프레임 인스턴스를 생성하지 않는다.
- Simulation/QTN/Generated/SDK/시스템 순서/캐릭터·입력·HUD/기존 카운터 줌·피격 흔들림 코드는 변경하지 않는다.
  이는 로컬 View 기능이며 게임 판정·동기화 상태를 저장하지 않는다.

API 근거: Unity 6000.3의 [뷰포트 좌표](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.ViewportToWorldPoint.html),
[카메라 레이어 필터](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-cullingMask.html),
[편집 모드 실행](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ExecuteAlways.html).
설치 에디터 리플렉션과 에셋 조회로 Camera API 및 Unlit 머티리얼 GUID/셰이더를 확인했다.

## 최종 한 차례 검증

계획: Unity 임포트/컴파일 및 콘솔 1회, [새 fixture](../../Assets/Tests/Editor/LSDFStageBackgroundTests.cs)의 9개 사례,
정적 정책, 관련 diff 공백·문서 링크·새 파일/메타를 하나의 최종 묶음으로 실행한다.
새 fixture는 4:3/16:9/21:9 양쪽 화면의 실제 투영 좌표, 피벗/비율·반전, 이동/줌/흔들림에 해당하는 카메라 자세,
카메라 비활성 전환·직교 투영, Ground 복원·재진입·다른 스테이지 격리·씬 참조, Unlit GPU 픽셀을 검증한다.
카메라 효과 이벤트의 재발생이나 실제 Photon 대전/네트워크 rollback 증거와는 구분한다.
실제 사용자 PNG 연결/바닥 높이 육안 조정과 실제 대전은 사용자가 이미지 연결 후 확인해야 한다.
실패 시 반복하지 않으며 원인이 명확한 범위의 수정 후 미검증으로 남긴다.

## 기존 사용자 변경

기존 IDE 생성 파일 9개/미추적 csproj 4개, 배경 PNG 두 개와 메타의 삭제, 새 사용자 PNG/메타,
이전 current-state 미커밋 변경을 보존한다. 제외 폴더는 탐색하지 않는다.
새 스크립트/Editor/테스트와 메타, 이 문서와 후속 증거는 미추적이며 커밋·푸시는 요청받지 않았다.

## 최종 한 차례 결과

근거: [검증 JSON](evidence/stage-background-validation.json).

| 검사 | 결과 |
|---|---|
| Quantum 정적 정책 | PASS — Simulation 94개, 기존/신규 적중 0. Simulation diff 없음. 게임 결정론 증거는 아님 |
| 관련 diff 공백 | PASS — 씬/current-state. LF→CRLF 안내만 있음 |
| 문서/메타/새 파일 공백 | PASS — 결과 기록 전 상대 링크 30개, 새 C# 메타 짝 3개 및 소스 해시 기록 |
| Unity 임포트/컴파일 준비 대기 | **TIMEOUT** — refresh/compile 요청은 전달됐으나 에디터 준비 대기가 60초 초과 |
| 컴파일 결과/콘솔 | **확인 불가 / NOT RUN** — 상태·콘솔 도구가 `Unity session not available; please retry` / `no_unity_session`을 반환 |
| 새 표현 사례 9개 | **NOT RUN** — job 시작 안 함. 새 컴포넌트 컴파일을 확인하지 못해 네이티브 테스트/씬 재로드는 실행하지 않음 |
| 사용자 PNG 연결/실제 양쪽 화면 육안/Photon 대전 | **NOT RUN** — 이미지 슬롯을 의도적으로 비워 둠 |

Unity 인스턴스 목록이 0개인 것을 확인했다. 프로세스 조회에서는 에디터가 실행 중이며 응답한다고 표시됐다.
별도의 Player 2 프로세스도 실행 중이었으며 어느 프로세스도 종료·재시작하지 않았다.
기존 컴파일 요청의 연결 복구 상태만 확인했고 마지막 조회에서도 인스턴스 0개였다.
컴파일/콘솔/게임 검사를 자동 재실행하거나 다른 Unity 인스턴스를 열어 우회하지 않았다.
컴파일 성공·테스트 통과를 주장하지 않는다. Unity가 준비되면 게임 씬을 다시 열어 `StageBackground` 슬롯에 이미지를 연결할 수 있다.
새 자동 검증 요청 시 컴파일 확인과 9개 fixture를 실행한다. 이 다음 작업은 재검증의 자동 승인이 아니다.
결과 문서/JSON/current-state의 후속 기록은 위 문서 검사 이후 추가했으며 반복 검사하지 않았다.
