# 배경·카메라 사용자 테스트 완료 및 푸시 — 2026-10-22

확인 시각: 2026-10-22T03:00:26+09:00. 기준 HEAD `debf379c006e40a9a4e00884db3d27bfd111a4d9`, 브랜치 `dev/Prototype`.
사용자가 **“지금까지 한 내용 내가 다 테스트 했으니까 푸쉬해줘”**라고 확인했다.
현재 작업물의 사용자 테스트 완료 보고와 기존 `origin/dev/Prototype`으로의 커밋·푸시 승인을 기록한다.
게임 자동 검사는 재실행하지 않는다. PR 생성은 요청받지 않았다. 사용자 테스트의 상세 횟수·로그는 제공되지 않았다.
이전 자동 FAIL/NOT RUN/수정 후 미검증을 자동 PASS로 바꾸지 않는다.

## 게시 범위

- 양쪽 카메라용 배경, 캐릭터와 같은 비율의 평면 줌, Inspector 및 관련 테스트·메타·작업 기록/자동 근거.
- `LSDFSceneBindings.CameraHeight`와 사용자 씬의 값 0.09.
- 사용자 배경 PNG/메타, 스테이지 위치 Y=0.32·배율 2, 기존 배경 PNG 2개/메타 삭제.
- 사용자 게임 씬·Map의 카메라/바닥 위치 조정과 메뉴 효과 불투명도 직렬화.
- 사용자 추가 `JumpUpper_Enemy`/`KazumiHoming_Enemy`의 폴더 메타, PNG 72개와 각 메타, clip/controller 2개씩과 메타.
- IDE 생성 파일 9개와 미추적 csproj 4개는 로컬에 보존하고 포함하지 않는다.

시뮬레이션 코드/QTN/Generated/SDK/시스템 순서/프로토콜 변경은 없다.
사용자 Map의 고정소수 위치 값 수정은 그대로 포함한다. 기존 GUID·애니메이션 값·참조와 씬 튜닝 값은 유지한다.
새 Unity YAML/추가된 씬 줄의 끝 공백만 게시 준비 과정에서 정리한다. 값이나 소스 인코딩 전체를 변경하지 않는다.

## 게시 준비 중 누락된 배경 참조 복구

게임 씬의 배경 슬롯과 두 SpriteRenderer가 GUID `d34df5a03409f1d449314555563daf55`를 가리켰다.
Unity AssetDatabase가 반환한 경로는 `Assets/Sprites/Background/목재 로봇 폐기장 아레나_export.png`였지만 PNG/메타가 실제 디스크에 없었다.
파일 조회와 제외 규칙 확인에서도 게시할 원본 파일이 없음을 확인했다.
남아 있는 사용자 원본 PNG의 GUID `52f731fa27a04924483e7eb333936d74`로 씬 참조 세 곳만 복구했다.
원본 메타는 Sprite/Single이며 해당 에셋의 GUID와 import 설정은 유지한다. 사용자 위치·배율·카메라 높이는 바꾸지 않는다.
이 참조 복구는 사용자 테스트 완료 보고 이후의 변경이다. 정적 참조 확인 3곳은 통과했고 복구 후 실제 게임 재실행은 NOT RUN이다.

## 게임 자동 검사 이력

[배경 자동 근거](evidence/stage-background-flat-zoom-validation.json): 13개 중 11 PASS / 2 FAIL 이후 측정점만 보정하고 미검증.
[카메라 높이 기록](camera-height-inspector.md): 컴파일/콘솔과 기존 카메라 회귀 8개 PASS,
별도 프리뷰 JSON 왕복 후 참조 검사 FAIL, 공백 검사 개별 종료 코드 UNKNOWN.
이번 사용자 확인은 별도 근거이며 이 결과를 덮어쓰지 않는다. 온라인/강제 네트워크 rollback의 새 자동 근거도 없다.
제품 C# 해시는 이전 구현 종료 시점과 일치하며 테스트 소스도 보정 후 해시와 같다.

## 에셋과 게시 검사

[PNG별 구조·해시 근거](evidence/stage-camera-publish-assets.json): 73개 PNG/2,593,884바이트.
배경은 1933×814/2,391,961바이트, 새 캐릭터 프레임은 57×98 37개와 58×64 35개다.
모든 PNG signature/chunk CRC/IDAT 압축 해제/scanline 길이·filter/메타 짝/IEND 뒤 데이터 0을 확인했다.
배경 그림과 각 캐릭터 동작의 첫 프레임은 직접 열어 확인했다. 전체 게임 재생이나 모든 프레임의 육안 검사는 아니다.
배경에는 콘텐츠 출처 정보인 caBX chunk 21,881바이트가 있다. PNG를 수정하거나 이 메타데이터를 제거하지 않는다.
바이너리·2 MiB 초과 PNG는 자동 텍스트 비밀정보 검사 PASS로 취급하지 않고 위의 별도 검토 범위/한계를 기록한다.

origin의 fetch/push URL은 모두 기존 `eeptmxlsl15/Look-See-Do_Fighter`다. GitHub 인증/ADMIN 권한 확인,
fetch 후 원격 작업 브랜치와 0/0, 기본 브랜치 main, URL rewrite/별도 push 설정 없음, 해당 head의 열린 PR 없음.
게시 메시지: `Add flat stage background and inspector camera height`.
게시 검사는 정적 정책·staged 공백·후보와 미게시 이력 preflight를 한 차례 실행하고 결과를 아래에 기록한다.
승인된 대상 `HEAD:refs/heads/dev/Prototype`으로 푸시 후 원격 SHA를 확인한다.

게시 준비 최종 한 차례 결과:

- 정적 정책 PASS: Simulation 94개/기존·신규 적중 0. staged Simulation 코드 diff 없음.
- staged 공백 PASS: 개별 종료 코드 0. 앞선 카메라 작업의 UNKNOWN을 과거 기록에서 바꾸지 않는다.
- 상대 링크 51개, 새 캐릭터 에셋/메타 짝 76개와 clip/controller 참조는 PASS.
- preflight 종료 코드 1, 원본 판정 `BLOCKED`: staged 항목 174개/blob 173개/미게시 커밋 0개.
  발견은 `LARGE_FILE_NOT_SCANNED` 1개(배경)와 `BINARY_NOT_SCANNED` 72개(새 캐릭터 PNG)뿐이다.
  알려진 텍스트 비밀 패턴/불필요 경로/제외 폴더 적중은 없다. 경로 변경 수 176개와 검사기의 rename 처리 항목 수는 구분한다.
- 위 PNG 73개는 사용자 제공 게임 에셋이며 별도 구조·메타 검토와 표본 육안 확인을 완료했다.
  그 근거와 사용자의 테스트 완료·푸시 승인을 기록하고 포함한다. 검사기 판정을 PASS로 바꾸거나 검사기를 재실행하지 않는다.
- 검사 후 게임 코드/에셋은 변경하지 않고 결과 기록만 수동 검토해 갱신한다.

최종 tree·커밋·원격 SHA와 실행 결과는 푸시 후 [현재 상태](current-state.md)에 기록한다.
