# 김석진 포트폴리오 Astra 재구성

작성: 2026-10-22T23:06:29+09:00. 브랜치 `dev/Prototype`, HEAD `b571a8f15e617788a0c86fe6507e992146f1800c`.

## 요청과 작업 범위

사용자가 기존 PPT의 품질에 불만을 표시하고 Astra 모델로 다시 제작하도록 요청했다.
기존 20장 파일을 보존하고, 이름 김석진·GitHub 제외·6일 작업 흐름 재구성 허용 및 사용자 디자인 시스템을 유지했다.
게임 소스·QTN·씬·에셋은 변경하지 않았다. 기존 게임 검사를 재실행하거나 커밋·게시하지 않았다.

## 사람과 AI의 역할

- 사용자: 지원 목적, 디자인 시스템, 개인정보 범위와 Astra 사용을 지정했다.
- `gpt-6-astra` 에이전트: 자료 선정, 14장 구성과 문안, PowerPoint COM 제작 스크립트, 네이티브 도표, PPTX/PDF 생성 및 산출물 감사를 수행했다.
- 부모 에이전트: 기존 기록과 문구를 대조하고, 시안 14장과 PDF 전체를 검토했다. 겹침·단어 고립·출처 경로·실행 관측처럼 읽히는 문구를 수정하도록 피드백하고 최종 3장 수정본까지 확인했다.
- 적용 스킬: `lsdf-portfolio-evidence`, `presentations`, `pdf`. 발표 자료 작성이 게임 구현 성과를 새로 증명하지는 않는다.

## 새 구성과 산출물

- [편집용 PPTX](../../../output/portfolio/김석진_LSDF_포트폴리오_Astra_v2.pptx)
- [제출용 PDF](../../../output/portfolio/김석진_LSDF_포트폴리오_Astra_v2.pdf)
- [앞선 20장 제작 기록](2026-10-22-portfolio-presentation.md)

반복 표를 없애고 상태의 소유권, 공중 피해 카운터 불일치, View 수명과 씬 연결의 세 사례를 중심으로 재구성했다.
37종 공격의 판정 계약, 입력 tick, 후속타 피해 14/10/6/6/6, 경기 종료 흐름을 편집 가능한 PowerPoint 도형과 텍스트로 설명한다.
피해 막대는 Excel 데이터 차트가 아닌 네이티브 도형이며, 수치·라벨·막대를 각각 수정할 수 있다.
6일 일정은 설명을 위한 작업 순서이며 실제 날짜별 작업량과 다르다는 캡션을 넣었다.

맑은 고딕 단일 계열, 16:9, 검정·흰색·파랑, 일반 슬라이드 12장의 고정 헤더를 유지했다.
표지·마무리 외 12장은 흰 바탕이며 246개 텍스트 도형과 원본 동작 에셋 2개를 사용했다. 로고·워터마크·GitHub 링크는 없다.
원본 동작 에셋은 실행 화면과 구분하여 표시했다. 새 Game View 캡처는 편집 모드에서 노란 단색만 나와 제외했고,
Scene View 캡처는 열린 창이 없어 실패했다. 게임 실행이나 씬 변경으로 범위를 넓히지 않았다.

## 근거와 표현의 범위

- [플레이어 데이터 전달](../loadout-isolation.md), [구조 정리](2026-10-17-portfolio-summary.md)
- [선입력](../input-buffer.md), [384/392 tick의 과거 관측](fighter-publish-validation.json)
- [피해 누락 조사](../homing-air-damage-investigation-2026-10-22.md), [70/50/30% 구현](../air-combo-damage-scaling.md)
- [이펙트 조사](../hit-effects-investigation.md), [후속 연결 수정](../hit-effects-prefab-migration.md)
- [라운드](../best-of-five.md), [메뉴 복귀](../match-return-to-main.md)
- [과거 로드아웃 검사](health-shake-publish-validation.json), [과거 낙하 검사](natural-rehit-fall-validation.json)
- [최신 자동 검사](air-combo-damage-scaling-validation.json), [직접 플레이 확인](../user-validation-air-combo-2026-10-22.md)

최신 자동 109 PASS/1 FAIL과 테스트 보정 후 미검증을 13장에 남겼다. 과거 체크섬 비교 67/136회를 합산하거나 현재 전체 PASS로 표현하지 않았다.
같은 Editor의 독립 세션, 입력 재생·Frame 복원과 독립 빌드·교차 플랫폼·강제 네트워크 rollback을 구분했다.
이펙트 사례는 소스의 호출 경로와 씬 연결 조사임을 한정했다. KazumiHoming의 빈 공격 이벤트 및 기존 미검증 이력은 노트와 기존 작업 기록에 유지한다.

## 문서 검증

실제 Microsoft PowerPoint COM으로 저장·PDF 내보내기·14장 PNG 렌더를 수행했다.
공통 artifact 런타임이 제공되지 않아 이전 작업과 같은 COM/OOXML/PyMuPDF 경로를 사용했다. python-pptx와 새 의존성 설치는 사용하지 않았다.
사용자의 문서 렌더링·수정 반복 요구에 따라 시안 결함을 수정한 뒤 다시 내보냈다. 게임 검사 반복은 없다.

| 항목 | 결과 |
|---|---|
| PPTX/PDF 생성 | PASS, 14장/14쪽 |
| 개별 시각 검토 | PASS, PPTX 렌더 및 PDF 전 페이지, 최종 변경된 3장 별도 확인 |
| 화면 크기 | PASS, PPTX 12192000×6858000 EMU / PDF 960×540pt |
| 일반 헤더 | PASS, 12장 동일 좌표·크기 |
| 글꼴 | PASS, 사용 텍스트 Malgun Gothic 계열만, PPTX 임베드 항목 2개 |
| PDF | PASS, 전 페이지 TTF 포함·맑은 고딕만·한글 추출·대체 문자 0 |
| 텍스트 범위 | PASS, PowerPoint 실측 초과 의심 0 / 화면 밖 0 |
| 편집성·패키지 | PASS, 네이티브 텍스트·도형 보존, ZIP 무결성 |
| 다른 앱·운영체제 | NOT RUN |
| 게임 컴파일·회귀·결정론 | NOT RUN, 문서 작업에서 게임 변경 없음 |

최종 PPTX SHA256: `da27fac306178c5f1347656cff5bfe9021090affce4cb0d88b7281d42d1be489`.
최종 PDF SHA256: `48c76699488c5acff5eeaeb65170e8b2f95415045bae91859e1ce3e4eae8dd7d`.
빌드 스크립트, 실측치, 감사 JSON과 렌더는 `tmp/portfolio-astra/`에 보존한다.

## 인계

기존 IDE 수정 9개·경기 씬 수정·미추적 배경 PNG/메타 4개·csproj 4개·`pptstyle.md`를 보존했다.
기존 포트폴리오와 이번 새 산출물, 임시 빌드 파일, 두 제작 증거 문서와 current-state는 로컬 미커밋 상태다.
다음 작업은 사용자의 새 자료·문구·분량 피드백 반영이며, 게임 재검증·커밋·게시의 승인을 뜻하지 않는다.
