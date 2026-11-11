import hashlib
import json
import re
from pathlib import Path

paths = [
 'Assets/QuantumUser/View/LSDF_IngameUI.cs',
 'Assets/QuantumUser/View/LSDFSceneBindings.cs',
 'Assets/QuantumUser/View/LSDFSceneBindings.GameplayUI.cs',
 'Assets/QuantumUser/View/LSDFSceneBindings.GameplayUI.cs.meta',
 'Assets/QuantumUser/View/Editor/LSDFSceneBindingsEditor.cs',
 'Assets/QuantumUser/View/Editor/LSDFSceneBindingsEditor.cs.meta',
 'Assets/QuantumUser/View/Editor/LSDF.View.Editor.asmdef',
 'Assets/Tests/Editor/LSDFGameplayUIImageTests.cs',
 'Assets/Tests/Editor/LSDFGameplayUIImageTests.cs.meta',
 'Assets/Scenes/LSDFGameplay.unity',
 'docs/ai/gameplay-ui-images.md',
 'docs/ai/health-bar-image-fit.md',
 'docs/ai/evidence/health-bar-image-fit-validation.json',
 'docs/ai/evidence/gameplay-ui-images-validation.json'
]
report = {'text_files':len(paths),'new_metas':3,'links':0,'findings':[],'sha256':{}}
for name in paths:
    path = Path(name)
    data = path.read_bytes()
    report['sha256'][name] = hashlib.sha256(data).hexdigest()
    text = data.decode('utf-8-sig')
    if name.endswith('.cs') or name.endswith('.cs.meta') or name.startswith('docs/ai/gameplay-ui') or name.endswith('gameplay-ui-images-validation.json'):
        for number,line in enumerate(text.splitlines(),1):
            if line != line.rstrip(): report['findings'].append({'path':name,'rule':'TRAILING_SPACE','line':number})
        if text.endswith('\n\n'): report['findings'].append({'path':name,'rule':'BLANK_EOF'})
    if name.endswith('.meta'):
        assert path.with_suffix('').exists()
        assert re.search(r'^guid: [0-9a-f]{32}$', text, re.M)
    if name.endswith('.md'):
        for target in re.findall(r'\]\(([^)]+)\)',text):
            if '://' in target or target.startswith('#'): continue
            report['links'] += 1
            if not (path.parent / target.split('#')[0]).exists(): report['findings'].append({'path':name,'rule':'MISSING_LINK','target':target})
    if name.endswith(('.json','.asmdef')): json.loads(text)
scene = Path('Assets/Scenes/LSDFGameplay.unity').read_text(encoding='utf-8')
ids = re.findall(r'^--- !u!\d+ &(\d+)$', scene, re.M)
assert len(ids) == len(set(ids)), 'Duplicate local file IDs'
assert scene.count('  m_Name: Announcement Image\n') == 2
report['scene_local_ids_unique'] = True
report['status'] = 'FAIL' if report['findings'] else 'PASS'
Path('tmp/gameplay-ui/file-validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
raise SystemExit(bool(report['findings']))
