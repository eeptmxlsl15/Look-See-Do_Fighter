from pathlib import Path
import hashlib
import json
import re

root = Path(__file__).resolve().parents[2]
build = root / 'tmp/infinite-stage-background'
record_path = root / 'docs/ai/evidence/infinite-stage-background-validation.json'
scene = root / 'Assets/Scenes/LSDFGameplay.unity'
scene_raw = scene.read_bytes()
new_fields = (
    '  RepeatHorizontally: 1',
    '  Player1LeftBackdrop: {fileID: 955306877}',
    '  Player1RightBackdrop: {fileID: 543184132}',
    '  Player2LeftBackdrop: {fileID: 682345977}',
    '  Player2RightBackdrop: {fileID: 1495885912}',
)
scene_lines = scene_raw.splitlines(keepends=True)
new_bytes = {line.encode() for line in new_fields}
restored = b''.join(line for line in scene_lines if line.rstrip(b'\r\n') not in new_bytes)
sha = lambda data: hashlib.sha256(data).hexdigest()
expected_original = '322e43706087eb30fcb9de3a7997a844de23e6e838e8613a832b2599395de0db'
scene_pass = sha(restored) == expected_original and all(
    sum(line.rstrip(b'\r\n') == field for line in scene_lines) == 1 for field in new_bytes)
map_path = root / 'Assets/Resources/LSDF_Map.asset'
map_hash = sha(map_path.read_bytes())
expected_map = 'b18bcc38fab7b51c5e59b82e56a6279cd657b8e3fb02514622d44ad95480d49f'
managed = json.loads((build / 'managed-compilation.json').read_text(encoding='utf-8-sig'))
sources = ['Assets/QuantumUser/View/LSDFStageBackground.cs',
           'Assets/QuantumUser/View/Editor/LSDFStageBackgroundEditor.cs',
           'Assets/Tests/Editor/LSDFStageBackgroundTests.cs']
test_text = (root / sources[-1]).read_text(encoding='utf-8-sig')
case_count = len(re.findall(r'\[TestCase\(', test_text)) + len(re.findall(r'\[Test\]', test_text))
record = {
    'timestamp': '2026-10-22T23:42:29+09:00',
    'branch': 'dev/Prototype',
    'head': 'b571a8f15e617788a0c86fe6507e992146f1800c',
    'scope': 'Horizontal background presentation only; Quantum simulation/colliders unchanged',
    'quantum_guard': {'status': 'PASS', 'files_scanned': 94, 'new_findings': 0,
                      'limits': 'Lexical policy check, not determinism proof'},
    'code_diff_whitespace': {'status': 'PASS', 'runs': 1},
    'managed_compile': {'status': 'PASS' if len(managed) == 3 and all(x['exitCode'] == 0 for x in managed) else 'FAIL',
                       'runs_per_assembly': 1, 'results': managed,
                       'limits': 'Roslyn compile against installed Unity and previously compiled unchanged dependencies; no Unity native execution'},
    'unity_refresh': {'status': 'FAIL', 'reason': 'no_unity_session', 'runs': 1},
    'unity_console': {'status': 'NOT RUN', 'tool_error': 'no_unity_session', 'read_attempts': 1},
    'unity_final_compile': {'status': 'NOT RUN', 'reason': 'Current editor log and ScriptAssemblies predate final source edits'},
    'unity_tests': {'status': 'NOT RUN', 'planned_source_case_count': case_count, 'job_id': None,
                    'reason': 'Zero connected Unity instances; no test job started'},
    'actual_game_art_seams_network_rollback': {'status': 'NOT RUN'},
    'scene_user_changes_preserved': {'status': 'PASS' if scene_pass else 'FAIL',
        'start_sha256': expected_original, 'end_sha256': sha(scene_raw),
        'sha256_after_removing_only_five_added_fields': sha(restored), 'added_fields': list(new_fields)},
    'quantum_map_preserved': {'status': 'PASS' if map_hash == expected_map else 'FAIL', 'sha256': map_hash},
    'source_sha256': {p: sha((root / p).read_bytes()) for p in sources},
    'png_sha256': sha((root / 'Assets/Sprites/Background/산속 목재소 격투장 풍경_50_export.png').read_bytes()),
    'png_import_change': 'spriteMeshType 1 -> 0 (Full Rect); PNG pixels were not edited',
    'commit_push_pr': 'NOT REQUESTED / NOT RUN',
}
record_path.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
doc = root / 'docs/ai/infinite-stage-background.md'
doc_text = doc.read_text(encoding='utf-8-sig')
links = re.findall(r'\]\(([^)]+)\)', doc_text)
missing = [link for link in links if not (doc.parent / link).exists()]
record['documentation_links'] = {'status': 'PASS' if not missing else 'FAIL', 'count': len(links), 'missing': missing}
record['documentation_trailing_spaces'] = {'status': 'PASS' if not any(line.rstrip() != line for line in doc_text.splitlines()) else 'FAIL'}
record_path.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: record[key] for key in ['managed_compile', 'scene_user_changes_preserved', 'quantum_map_preserved', 'documentation_links', 'documentation_trailing_spaces', 'unity_tests']}, ensure_ascii=False, indent=2))
assert scene_pass, 'The scene differs from user baseline beyond the five added fields'
assert map_hash == expected_map, 'Quantum map changed'
assert not missing, 'Missing documentation links'
assert record['managed_compile']['status'] == 'PASS'
assert case_count == 29, 'Unexpected planned fixture count'
