from pathlib import Path
from datetime import datetime, timezone, timedelta

p = Path(__file__).resolve().parents[2] / 'docs/ai/current-state.md'
raw = p.read_bytes()
line_end = raw.index(b'\n') + 1
insert_start = raw.index(b'## ')
original_start = raw.index(b'## ', insert_start + 3)
stamp = datetime.now(timezone(timedelta(hours=9))).isoformat(timespec='seconds')
section = f'''## 最新 인계 — 사용자 원고 16장 PowerPoint 제작

갱신: {stamp}. 기준 브랜치 dev/Prototype, HEAD b40aea428812a98bb9d516e32434d5db5329fd51.
[제작·최종 검사 기록](evidence/2026-10-23-portfolio-16-slides.md). 제공 원고 16개 주제의 PPTX/PDF와 캡처 삽입 안내를 output/portfolio에 작성했다.
맑은 고딕 단일 패밀리·16:9·고정 헤더·편집 가능한 표/도식을 적용했다. 원문 전체는 각 장 노트에 보존했다. 개인 GitHub URL 없음.
3·8·11·12장의 그림 자리 표시자는 실제 캡처를 넣을 수 있다. 현재 PDF에는 회색 영역이 남으며, 삽입 후 PDF 재출력 방법은 안내에 기록했다.
렌더 검토·수정 후 최종 산출물 검사 한 차례 12항목 PASS: 16장·노트 원문·고정 좌표·글꼴/한글·네이티브 표/이미지 영역·경계·검증 한계 표시. PowerPoint 전 장 및 PDF 16페이지 렌더를 검토했다.
게임 컴파일·CodeGen·guard·테스트·Photon/rollback은 NOT RUN(프레젠테이션 작업). 게임 코드·씬·설정·아트·원천 문서는 수정하지 않았으며, 과거 FAIL/NOT RUN·KazumiHoming 빈 이벤트 등은 유지한다.
기존 로컬 UI/테스트/PNG·렌더 설정·IDE·이전 포트폴리오/output/tmp/pptstyle을 보존했다. 이번 Git 조회는 성공했으며 기존 HEAD를 유지한다.
새 출력·안내·제작 스크립트/렌더/검사 JSON·이 기록은 로컬 미추적이다. staging/commit/push/PR 없음. 기존 인계 본문 바이트를 유지하며 이 요약만 앞에 추가한다.
다음은 사용자 캡처 삽입과 최종 PDF 내보내기이며 게임 검사·게시를 자동 실행할 승인이 아니다.
'''.replace('最新', '최신')
p.write_bytes(raw[:line_end] + b'\r\n' + section.replace('\n', '\r\n').encode('utf-8') + b'\r\n' + raw[original_start:])
print(stamp)
