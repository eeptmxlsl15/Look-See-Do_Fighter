import hashlib
import importlib.util
import json
import re
import struct
import subprocess
import zlib
from pathlib import Path
from PIL import Image

root = Path.cwd()
report = {"binary_review": [], "scene_whitespace_only_cleanup": {}}
for suffix in (25, 50):
    path = root / f"Assets/Sprites/Background/산속 목재소 격투장 풍경_{suffix}_export.png"
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    offset = 8
    chunks = []
    while offset < len(data):
        size = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + size]
        crc = struct.unpack_from(">I", data, offset + 8 + size)[0]
        assert zlib.crc32(kind + payload) & 0xffffffff == crc
        chunks.append(kind.decode("ascii"))
        offset += size + 12
        if kind == b"IEND":
            break
    assert offset == len(data), "Trailing PNG data"
    assert set(chunks) <= {"IHDR", "IDAT", "IEND", "sRGB", "gAMA", "pHYs", "PLTE", "tRNS", "cHRM"}, "Unexpected metadata chunk"
    with Image.open(path) as image:
        dimensions = list(image.size)
        mode = image.mode
        image.verify()
    meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
    guid = re.search(r"^guid: (\w+)$", meta, re.M)[1]
    mesh = int(re.search(r"^  spriteMeshType: (\d+)$", meta, re.M)[1])
    report["binary_review"].append({"path": path.relative_to(root).as_posix(), "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest(), "dimensions": dimensions, "mode": mode, "chunks": chunks, "crc_and_decode": "PASS", "no_trailing_data": True, "no_text_or_custom_metadata": True, "guid": guid, "spriteMeshType": mesh})

scene = root / "Assets/Scenes/LSDFGameplay.unity"
before = scene.read_bytes()
diff = subprocess.run(["git", "-c", f"safe.directory={root.as_posix()}", "diff", "--unified=0", "--", "Assets/Scenes/LSDFGameplay.unity"], capture_output=True, check=True).stdout.decode("utf-8")
changed = []
new_line = 0
lines = before.splitlines(keepends=True)
for line in diff.splitlines():
    if line.startswith("@@"):
        new_line = int(re.search(r"\+(\d+)", line)[1])
    elif line.startswith("+++") or line.startswith("---"):
        continue
    elif line.startswith("+"):
        if re.fullmatch(r"\+\s+(?:m_Name|m_EditorClassIdentifier|CommandSkills): +", line):
            original = lines[new_line - 1]
            ending = b"\r\n" if original.endswith(b"\r\n") else b"\n"
            lines[new_line - 1] = original.rstrip(b"\r\n").rstrip(b" ") + ending
            changed.append(new_line)
        new_line += 1
    elif not line.startswith("-") and not line.startswith("\\"):
        new_line += 1
after = b"".join(lines)
assert len(changed) == 6, changed
assert [x.rstrip() for x in before.splitlines()] == [x.rstrip() for x in after.splitlines()]
scene.write_bytes(after)
report["scene_whitespace_only_cleanup"] = {"lines": changed, "removed_spaces": len(before)-len(after), "before_sha256": hashlib.sha256(before).hexdigest(), "after_sha256": hashlib.sha256(after).hexdigest(), "all_non_whitespace_bytes_preserved": True, "reason": "Six empty YAML values gained trailing spaces during existing Unity serialization; values, references and geometry unchanged."}
scene_text = after.decode("utf-8")
assert report["binary_review"][1]["guid"] == "4d6e4726a819aba4a9d8a5aa95ee16dc"
assert scene_text.count("guid: " + report["binary_review"][1]["guid"]) == 7
report["scene_sprite_guid_references"] = 7
report["source_sha256"] = {}
sources = [
    "Assets/QuantumUser/Resources/LSDF_GameConfig.asset",
    "Assets/QuantumUser/Simulation/LSDFJuggle.cs",
    "Assets/QuantumUser/Simulation/LSDFJuggleSettings.cs",
    "Assets/QuantumUser/Simulation/LSDFLoadout.cs",
    "Assets/QuantumUser/View/Editor/LSDFStageBackgroundEditor.cs",
    "Assets/QuantumUser/View/LSDFStageBackground.cs",
    "Assets/Scenes/LSDFGameplay.unity",
    "Assets/Tests/Editor/LSDFComboEnderTests.cs",
    "Assets/Tests/Editor/LSDFHitReactionTests.cs",
    "Assets/Tests/Editor/LSDFJuggleTests.cs",
    "Assets/Tests/Editor/LSDFOnlineComboEnderTests.cs",
    "Assets/Tests/Editor/LSDFStageBackgroundTests.cs",
]
for name in sources:
    report["source_sha256"][name] = hashlib.sha256((root / name).read_bytes()).hexdigest()
report["binary_visual_review"] = "PENDING"
out = root / "docs/ai/evidence/2026-10-23-publication-review.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=True))
