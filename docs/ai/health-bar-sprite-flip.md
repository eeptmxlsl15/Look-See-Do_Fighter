# 1P 체력 채움 Sprite만 좌우 반전

기준: `dev/Prototype`, `b40aea428812a98bb9d516e32434d5db5329fd51`.
사용자 요청: 1P 체력 이미지의 좌우 색상만 뒤집고 체력이 줄어드는 방향은 유지한다.

현재 1P(왼쪽) 체력바는 Right-origin=1, 2P(오른쪽)는 Left-origin=0이다.
원본 채움은 왼쪽 노랑/오른쪽 빨강이어서 Right-origin의 1P는 적은 체력이 빨간 부분으로 남았다.

## 적용

`LSDFSceneBindings → 게임플레이 UI 이미지 → 체력바 → 1P (왼쪽) 체력 이미지 좌우 반전`은 기본 켬이다.
기존 양쪽 HUD 레이아웃의 왼쪽 체력 채움에만 적용한다. 오른쪽 체력바와 배경/아이콘/중앙 알림은 그대로다.
현재 로컬 HUD는 왼쪽에 내 HP를 표시하는 기존 구조이므로 2P 카메라 레이아웃도 같은 표시 규칙을 따른다.

[메시 효과](../../Assets/QuantumUser/View/LSDFUIImageHorizontalFlip.cs)는 UGUI Image가 이미 생성한 정점의 위치를 건드리지 않고
Sprite 전체 atlas UV 범위에서 X 좌표만 반사한다. 잘라낸 Sprite가 전체 텍스처의 일부여도 해당 Sprite 밖을 샘플링하지 않는다.
fillAmount로 잘린 UV 범위의 중심을 반사하는 방식은 쓰지 않는다. 낮은 HP에는 원본 왼쪽의 노란 부분이 남는다.
RectTransform의 음수 Scale이나 fillOrigin 교체는 사용하지 않아 HP가 사라지는 방향·막대 위치·크기·흔들림을 유지한다.

[이미지 적용 경로](../../Assets/QuantumUser/View/LSDFSceneBindings.GameplayUI.cs)에서 효과를 한 번 연결하고 재사용한다.
Inspector 적용/미리보기와 런타임 양쪽 경로를 지원하고 끄면 원래 Sprite 샘플링으로 돌아간다.
Sprite/PNG/atlas 절단 메타·체력 수치·Simulation/QTN/SDK/프로토콜은 수정하지 않는다.

## 검증

[검증 JSON](evidence/health-bar-sprite-flip-validation.json).
[새 mesh 검사](../../Assets/Tests/Editor/LSDFHealthSpriteFlipTests.cs)는 atlas 내부 Sprite와 두 fillOrigin × HP 100/50/10%에서
실제 Image의 정점 생성 전후 위치·색·fillAmount/fillOrigin·Transform 불변, UV 반사/원복과 1P 저체력의 노란 UV 구간을 확인한다.
기존 이미지 UI·체력 흔들림/씬 참조를 같은 한 차례 묶음에 포함한다. 컴파일·정적·코드/문서·메타도 한 번만 검사한다.
GPU 픽셀 색/실제 Photon 대전/강제 rollback 측정과 구분한다. 검사 후 제품 수정이나 성공한 검사 반복은 하지 않는다.

결과: 2026-10-23T01:30:07+09:00, job `d5f083112dbd4142b7d048876cba6c8b` **28 PASS / 0 FAIL / 0 SKIP**.
mesh 6개·기존 이미지 UI 18개·흔들림/씬 참조 4개이며 한 번 실행했다. 컴파일 오류 0/기존 경고 7, 정적 94개 신규 0,
관련 파일 9개 공백·링크 4개·새 메타 2짝 PASS. 검사 후 제품 수정/반복 검사 없음.
현재 열린 두 HUD의 왼쪽 Image에 Undo를 기록하고 적용했다. Fill Origin=1/Scale=(1,1,1), 오른쪽 반전 효과 없음 확인.
씬을 자동 저장하지 않았으므로 미리보기 상태를 보존하려면 사용자가 저장한다. Inspector 적용과 런타임도 자동으로 효과를 연결한다.
사용자 PNG/슬라이스 메타 6개·기존 UI/씬/IDE·포트폴리오/임시 자료를 보존했고 staging/커밋/Push/PR은 하지 않았다.
