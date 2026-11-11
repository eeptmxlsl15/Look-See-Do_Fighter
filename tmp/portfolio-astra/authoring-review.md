# Astra 포트폴리오 재구성 인계

2026-10-22, Asia/Seoul. 본 작업은 문서 제작만 수행했다. 게임 실행·수정·검사, Git staging·commit·push는 하지 않았다.

## 구성

14장으로 전면 재구성했다. 도입과 게임 맥락, 6단계 작업 흐름, 플레이어 데이터와 Frame 소유권, 37종 공격의 판정 보존, 선입력, 공중 피해 원인과 수정, VFX 수명 회귀, 매치 종료, 사람과 AI 역할, 검증 범위, 마무리 순서다.

실제 실행 캡처가 없어 원본 대기·어퍼 동작 PNG 2개를 사용했고 실행 화면이 아님을 표시했다. 가상 HUD나 가짜 대전 장면을 만들지 않았다. 다이어그램과 피해 막대는 네이티브 PowerPoint 도형이다. Excel 데이터 시리즈형 차트는 아니지만 모든 수치·막대·라벨을 개별 편집할 수 있다. 슬라이드 전체를 이미지로 평탄화하지 않았다.

## 사실과 기여

- 김석진: 선입력20tick·공중 피해70/50/30% 요구, 플레이 증상 보고, 직접 테스트 완료 확인.
- AI: 코드·씬 조사, Frame/View 경계에 맞춘 구현과 테스트 작성, 실패·한계 기록.
- 기존 SDK·애드온·원본 아트 제작을 이번 협업의 기여로 산정하지 않는다.
- 로드아웃 체크섬67회와 낙하136회는 과거 서로 다른 검사 시점이다. 같은 Editor의 독립 Quantum 세션으로 한정했다.
- 선입력 press384/dispatch392의 정확한 출처는 evidence/fighter-publish-validation.json이다.
- HP170 유지 사례는 실제 피해 함수 프로브다. 온라인 매치의 특정 3~4타 재현을 주장하지 않았다.
- 최신 공중 피해 검사는109PASS/1FAIL, fixture 보정 후 미검증으로 본문13장에 명시했다. 강제 네트워크rollback·교차플랫폼·독립빌드·성능측정은 남은 범위다.
- KazumiHoming의 빈 공격 이벤트와 다른 후속 미검증 이력은13장 발표자 노트에서 current-state 근거로 보존했다.

## 제작과 확인

공통 artifact 런타임이 제공되지 않아 설치된 PowerPoint COM으로 제작했다. python-pptx와 대체 패키지 설치는 사용하지 않았다. 새 작업의 edit marker는 PPTX/PDF 각각 정확히1회 실행했다.

최종 PowerPoint14장과 PDF14쪽을 내보내고, 전 페이지를1600px 너비로 렌더했다. 개별 육안 검토 후 번호/제목 간격·단어 고립·긴 문장·사실 표현을 수정했다. 최종3장의 열 사이 간격을 추가 조정한 뒤3장을 다시 개별 확인했다.9장 반복 헤더가 배치 이미지 표시에서 생략된 듯 보였지만 단독 PNG와 PDF span을 확인해 실제 파일은 정상임을 확인했다.

artifact-audit.json에 최종 출력 해시·14장·16:9·고정헤더·맑은고딕단일·PPTX폰트임베드2·PDF한글/폰트·텍스트실측초과0·슬라이드밖0을 기록했다. 다른 앱/OS 표시와 공통 artifact finalizer는 실행하지 않았다.

사용자에게 전달할 파일:
- output/portfolio/김석진_LSDF_포트폴리오_Astra_v2.pptx
- output/portfolio/김석진_LSDF_포트폴리오_Astra_v2.pdf

작업용 자료:
- tmp/portfolio-astra/build-astra.ps1, helpers.ps1
- tmp/portfolio-astra/audit-astra.py, artifact-audit.json, text-metrics.json, build-manifest.json
- tmp/portfolio-astra/powerpoint/slide-01.png~14.png
- tmp/portfolio-astra/pdf/page-01.png~14.png

current-state와 정식 evidence 문서는 부모 에이전트가 갱신한다. 이 작업에서는 두 문서를 수정하지 않았다.
