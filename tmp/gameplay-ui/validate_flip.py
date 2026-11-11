import hashlib
import json
import re
from pathlib import Path
names = [
 'Assets/QuantumUser/View/LSDFUIImageHorizontalFlip.cs',
 'Assets/QuantumUser/View/LSDFUIImageHorizontalFlip.cs.meta',
 'Assets/QuantumUser/View/LSDFSceneBindings.GameplayUI.cs',
 'Assets/QuantumUser/View/Editor/LSDFSceneBindingsEditor.cs',
 'Assets/Tests/Editor/LSDFHealthSpriteFlipTests.cs',
 'Assets/Tests/Editor/LSDFHealthSpriteFlipTests.cs.meta',
 'Assets/Tests/Editor/LSDFGameplayUIImageTests.cs',
 'docs/ai/health-bar-sprite-flip.md',
 'docs/ai/evidence/health-bar-sprite-flip-validation.json'
]
report = {'files':len(names),'links':0,'meta_pairs':0,'sha256':{},'findings':[]}
for name in names:
 p=Path(name); data=p.read_bytes(); text=data.decode('utf-8-sig'); report['sha256'][name]=hashlib.sha256(data).hexdigest()
 for line,content in enumerate(text.splitlines(),1):
  if content != content.rstrip(): report['findings'].append({'path':name,'line':line,'rule':'TRAILING_SPACE'})
 if text.endswith('\n\n'): report['findings'].append({'path':name,'rule':'BLANK_EOF'})
 if name.endswith('.meta'):
  assert p.with_suffix('').exists() and re.search(r'^guid: [0-9a-f]{32}$',text,re.M); report['meta_pairs']+=1
 if name.endswith('.json'): json.loads(text)
 if name.endswith('.md'):
  for link in re.findall(r'\]\(([^)]+)\)',text):
   assert (p.parent/link).exists(), link; report['links']+=1
report['status']='FAIL' if report['findings'] else 'PASS'
print(json.dumps(report))
raise SystemExit(bool(report['findings']))
