import re
from pathlib import Path
text = Path('Assets/Scenes/LSDFGameplay.unity').read_text(encoding='utf-8')
blocks = {int(m[2]): m[0] for m in re.finditer(r'--- !u!(\d+) &(\d+)\n.*?(?=--- !u!|\Z)', text, re.S)}
def ref(block, key):
    m = re.search(r'^\s*' + key + r': \{fileID: (\d+)\}', block, re.M)
    return int(m[1]) if m else 0
def name(go):
    m = re.search(r'^  m_Name: (.*)', blocks[go], re.M)
    return m[1] if m else '?'
def component_go(i): return ref(blocks[i], 'm_GameObject')
def transform(i):
    go = component_go(i)
    return next(int(x) for x in re.findall(r'component: \{fileID: (\d+)\}', blocks[go]) if blocks[int(x)].startswith('--- !u!224'))
def describe(i):
    block = blocks[i]
    go = component_go(i)
    tr = transform(i)
    parent = ref(blocks[tr], 'm_Father')
    print(i, name(go), 'transform', tr, 'parent', parent, name(component_go(parent)))
    print('rect', '\n'.join(x.strip() for x in blocks[tr].splitlines() if any(k in x for k in ['m_Anchor', 'm_SizeDelta','m_Children','m_Pivot'])))
    print('parent component refs', re.findall(r'component: \{fileID: (\d+)\}', blocks[component_go(parent)]))
for i in [354113924,69612071,1213412948,1944516709,325279,1135541509,470583023,622582915]: describe(i)
