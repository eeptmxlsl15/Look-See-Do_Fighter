# 김석진 AI Native·클라이언트 포트폴리오 제작

작성: 2026-10-22, Asia/Seoul. 기준 브랜치 `dev/Prototype`, HEAD `b571a8f15e617788a0c86fe6507e992146f1800c`.

## 문제와 목표

1년 전 중단한 Look-See-Do Fighter를 AI와 6일간 재개발한 경험을 채용 제출용 PPTX/PDF로 설명한다.
사용자 제공 디자인 시스템을 적용하고, 현재 소스·작업 기록에 확인되는 기여와 실행 증거만 사용한다.
기존 게임 전체나 SDK·애드온·원본 아트 제작을 이번 AI 협업의 성과로 산정하지 않는다.

## 사람과 AI의 역할

- 사용자: 이름 김석진, GitHub 제외, AI Native·클라이언트 지원 목적, 디자인 시스템을 지정했다.
- 사용자: 정확한 날짜별 배치는 중요하지 않으므로 작업 흐름을 러프하게 재구성하도록 허용했다.
- AI: 관련 소스·이력·기존 검증을 조사하고, 실제 기록을 여섯 단계와 20장으로 구성했다.
- AI: 원본 아트 배치, 편집 가능한 표·도표·다이어그램 제작, PowerPoint/PDF 내보내기와 시각 검토를 수행했다.
- 적용: `lsdf-portfolio-evidence`, `presentations`, `pdf` 스킬. 게임 구현·재검증·커밋·게시 요청으로 확대하지 않았다.

## 설계와 산출물

- [편집용 PPTX](../../../output/portfolio/김석진_LSDF_AI-Native_클라이언트_포트폴리오.pptx)
- [제출용 PDF](../../../output/portfolio/김석진_LSDF_AI-Native_클라이언트_포트폴리오.pdf)
- 20장, 16:9, 맑은 고딕 단일 계열, 검정·흰색·파랑, 로고·워터마크·GitHub 링크 없음.
- 일반 슬라이드 18장의 chapter/title/subtitle 좌표와 크기를 공통 함수로 고정했다.
- 네이티브 표 5개, 일반 텍스트 도형 299개, 원본 아트 이미지 7개를 사용했다. 슬라이드 전체를 이미지로 평탄화하지 않았다.
- 여섯 단계는 실제 날짜별 작업량을 의미하지 않음을 표시했다. 원본 아트를 조합한 장면은 실제 실행 캡처와 구분했다.
- 사용자가 지정한 선입력 20tick·공중 피해 70/50/30%, 사람이 한 플레이 확인, AI가 수행한 조사·구현을 구분했다.
- 최신 109 PASS/1 FAIL 및 fixture 수정 후 미검증, KazumiHoming의 빈 공격 이벤트, 강제 네트워크 rollback 등 NOT RUN을 보존했다.

공통 artifact 런타임 도구가 제공되지 않았고 해당 패키지도 확인되지 않아, 설치된 Microsoft PowerPoint를 COM으로 자동화했다.
새 의존성을 설치하거나 python-pptx로 대체하지 않았다. 이 대체 경로를 사용자에게 알렸다.
Windows의 `malgun.ttf`·`malgunbd.ttf`가 존재하며 폰트 메타데이터의 임베드 허용 값을 확인했다.

## 근거 자료

- [기여·구조 정리](2026-10-17-portfolio-summary.md)
- [로드아웃](../loadout-isolation.md), [선입력](../input-buffer.md), [5판 3선승](../best-of-five.md)
- [재피격 낙하](../natural-rehit-fall.md), [연출 회귀 조사](../hit-effects-investigation.md)
- [공중 피해](../air-combo-damage-scaling.md), [사용자 직접 확인](../user-validation-air-combo-2026-10-22.md)
- [과거 로드아웃 검사](health-shake-publish-validation.json), [과거 낙하 검사](natural-rehit-fall-validation.json), [최신 자동 검사](air-combo-damage-scaling-validation.json)

과거 235/235, 106/106 PASS를 현재 전체 게임의 PASS로 재사용하지 않았다.
같은 Editor의 두 Photon 연결·Quantum 세션, 입력 재생·Frame 복원과 강제 네트워크 rollback을 구분했다.
최신 자동 검사와 사용자 직접 테스트 보고를 합쳐 전체 자동 PASS로 표현하지 않았다.

## 문서 검증

사용자가 명시한 렌더링·수정 반복 요구에 따라 문서 레이아웃은 수정 후 다시 내보냈다. 게임 검사는 실행하지 않았다.

| 검사 | 결과 |
|---|---|
| PPTX 생성·저장 | PASS, 실제 Microsoft PowerPoint 사용 |
| PowerPoint 렌더 | PASS, 20장 모두 1600×900 PNG 생성 |
| PDF 내보내기·래스터화 | PASS, 20쪽 모두 생성·1600px 너비로 래스터화 |
| 최종 PDF 개별 시각 검토 | PASS, 20쪽의 잘림·겹침·줄바꿈·도표·로고 여부 검토 |
| PPTX/PDF 비교 | 전 페이지 평균 픽셀 차이 계산, 주로 안티앨리어싱 차이. PDF 시각 검토와 병행 |
| 캔버스 | PASS, PPTX 12192000×6858000 EMU, PDF 960×540pt |
| 헤더 위치 | PASS, 일반 슬라이드 18장에 동일한 좌표·크기 |
| 본문 글꼴 | PASS, PPTX 사용 텍스트의 Latin/East Asian 지정은 Malgun Gothic만 사용 |
| 폰트 임베드·PDF 문자 | PASS, PPTX 임베드 항목 2개, PDF 전 페이지 Malgun Gothic 계열 TTF 포함·한글 추출·대체 문자 0 |
| 텍스트 크기·범위 | PASS, PowerPoint 실제 BoundWidth/BoundHeight 기준 초과 의심 0, 슬라이드 밖 텍스트 0 |
| 네이티브 구조·패키지 | PASS, 네이티브 표 5개 및 텍스트·그림 보존, ZIP 무결성 확인 |
| 다른 앱·운영체제 표시 | NOT RUN |
| 게임 컴파일·회귀·정적 정책 | NOT RUN, 이번 문서 작업으로 게임 소스 변경 없음 |

첫 제작 중 표 셀의 미지원 속성·COM 자료형·PDF API 호출을 수정했다.
시각 검토에서 단어 중간 줄바꿈과 U+2212의 Cambria Math 대체를 발견해 문구 및 ASCII 하이픈으로 수정했다.
최종 PDF에는 다른 글꼴이 남지 않았다. 최종 텍스트 박스 크기와 다이어그램 연결선도 수정·확인했다.

생성 스크립트·빌드 명세·실측 텍스트·자동 감사 JSON·검토 PNG는 `tmp/portfolio-build/`에 보존한다.
공통 artifact finalizer는 런타임 부재로 실행하지 않았고, PowerPoint 실측·OOXML 패키지·PyMuPDF 기반 검사를 사용했다.

## 결과와 인계

PPTX와 PDF만 제출용 산출물이다. 발표자 노트에는 근거 문서와 해석 범위를 넣었다.
이번 작업은 Simulation/QTN/에셋을 수정하지 않았고 기존 IDE 9개, 경기 씬 변경, 미추적 배경 PNG·메타 4개, csproj 4개, `pptstyle.md`를 보존했다.
시작 시 정상 권한 Git 조회에서 현재 씬·배경·스타일 파일을 확인했으며 이전 요약보다 추가된 사용자 변경으로 취급했다.
새 산출물·`tmp/`·이 증거 문서·현재 상태는 미커밋이다. staging/commit/push/PR은 수행하지 않았다.
기존 게임 FAIL/NOT RUN/수정 후 미검증 상태는 바뀌지 않는다. 후속 수정·검사는 별도 사용자 요청 범위에서 진행한다.
