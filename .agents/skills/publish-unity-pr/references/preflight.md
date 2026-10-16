# 게시 후보 파일 검사

프로젝트 루트에서 실행한다. Python 표준 라이브러리와 Git만 필요하며 파일·index·브랜치·remote를 바꾸지 않는다.

```text
python .agents/skills/publish-unity-pr/scripts/preflight.py --base origin/<검토한-base>
```

`--base`는 먼저 fetch하고 검토한 PR base의 로컬 ref다. 이력 범위를 줄이려고 임의로 `HEAD`를 지정하지 않는다.
이미 게시된 base 밖의 커밋도 보수적으로 포함한다. 다른 base를 선택할 때에는 PR 범위와 함께 설명한다.

검사 범위:

- index의 추가·수정 파일 전체 blob (작업 트리의 다른 버전을 검사하지 않음).
- `base..HEAD` 각 커밋의 변경된 blob과 커밋 메시지. merge는 각 parent와 비교하며 중간 커밋에서 추가 후 삭제된 값도 검사한다.
- 삭제된 파일은 새 내용이 없으므로 과거 해당 커밋에서 검사한다. base 이전 전체 저장소 감사는 아니다.

JSON에는 경로·커밋/INDEX·규칙·행 번호만 출력한다. 발견한 비밀값이나 원문은 출력하지 않는다.
종료 코드: `0`=알려진 패턴 미발견, `1`=발견 또는 검사하지 못한 항목, `2`=Git/입력/실행 오류.
이 결과는 **게시 승인이나 Unity 테스트 PASS를 뜻하지 않는다**.

주요 검사: GitHub/OpenAI/AWS/Slack 토큰 형태, 개인키, 자격증명 할당, credential URL,
인증 파일명, Unity 캐시/빌드/로그/라이선스, 루트 IDE 생성 파일, 제외 에셋 경로.
CP949와 UTF-8/UTF-16 텍스트를 처리한다. 비텍스트 blob, LFS pointer, symlink/submodule, 2 MiB 초과는 미검사로 막는다.
필요한 대형 에셋을 자동 삭제하거나 검사 제한을 늘려 통과시키지 않는다. 별도 검토 및 검증 범위를 마련한다.

정규식 기반 검사이므로 인코딩/분할/암호화된 비밀정보나 모든 서비스 토큰을 찾지 못한다.
파일명·diff·커밋 메시지·공개 설정의 수동 검토가 필요하다. 필요하면 승인된 비밀정보 검사기를 추가로 쓰되
그 실행 결과를 기록하고 원본 토큰을 로그에 남기지 않는다. 오탐도 자동 무시하지 않는다.

새 검사기 자체의 검증은 임시 저장소 fixture에서 아래 명령으로 수행한다. 실제 프로젝트를 staging/commit하지 않는다.

```text
python -B -m unittest discover -s .agents/skills/publish-unity-pr/scripts/tests -v
```
