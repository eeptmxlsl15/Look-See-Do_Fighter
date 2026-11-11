import hashlib
import json
from pathlib import Path

root = Path.cwd()
path = root / "docs/ai/evidence/2026-10-23-publication-review.json"
report = json.loads(path.read_text(encoding="utf-8"))
fixes = []
for suffix in (25, 50):
    meta = root / f"Assets/Sprites/Background/산속 목재소 격투장 풍경_{suffix}_export.png.meta"
    before = meta.read_bytes()
    result = []
    removed_lines = []
    for number, line in enumerate(before.splitlines(keepends=True), 1):
        ending = b"\r\n" if line.endswith(b"\r\n") else (b"\n" if line.endswith(b"\n") else b"")
        content = line.rstrip(b"\r\n")
        cleaned = content.rstrip(b" \t")
        if content != cleaned:
            removed_lines.append(number)
        result.append(cleaned + ending)
    after = b"".join(result)
    assert [x.rstrip() for x in before.splitlines()] == [x.rstrip() for x in after.splitlines()]
    meta.write_bytes(after)
    fixes.append({"path": meta.relative_to(root).as_posix(), "lines": removed_lines, "only_trailing_whitespace_removed": True, "before_sha256": hashlib.sha256(before).hexdigest(), "after_sha256": hashlib.sha256(after).hexdigest(), "status": "CORRECTED_NOT_REVALIDATED"})

evidence = root / "docs/ai/evidence/air-hit-push-distance-validation.json"
before = evidence.read_bytes()
after = before.rstrip(b"\r\n") + b"\n"
assert json.loads(before) == json.loads(after)
evidence.write_bytes(after)
fixes.append({"path": evidence.relative_to(root).as_posix(), "only_eof_blank_line_removed": True, "json_value_unchanged": True, "status": "CORRECTED_NOT_REVALIDATED"})
report["staged_whitespace_original"] = {"status": "FAIL", "findings": 18, "details": "17 trailing-space lines in two newly added Unity PNG meta files; one extra EOF blank line in the existing air-hit evidence JSON", "runs": 1, "command_return_code": "Not captured independently: later Git identity commands returned 0. Failure is established by git diff --cached --check diagnostics.", "automatic_rerun": False}
report["whitespace_corrections"] = fixes
report["publication_status"] = "BLOCKED_CORRECTED_NOT_REVALIDATED; no commit or push"
path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"corrected_files": len(fixes), "status": report["publication_status"]}))
