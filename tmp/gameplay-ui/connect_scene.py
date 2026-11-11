import hashlib
import re
from pathlib import Path

path = Path('Assets/Scenes/LSDFGameplay.unity')
original = path.read_bytes()
text = original.decode('utf-8')
assert '\r' not in text
blocks = {int(m[2]): m[0] for m in re.finditer(r'--- !u!(\d+) &(\d+)\n.*?(?=--- !u!|\Z)', text, re.S)}
image_guid = 'fe87c0e1cc204ed48ad3b37840f39efc'
def ref(block, key):
    return int(re.search(r'^\s*' + key + r': \{fileID: (\d+)\}', block, re.M)[1])
def components(go):
    return [int(x) for x in re.findall(r'component: \{fileID: (\d+)\}', blocks[go])]
def component(go, kind):
    return next(i for i in components(go) if (kind == 'rect' and blocks[i].startswith('--- !u!224')) or (kind == 'image' and image_guid in blocks[i]))
def parent_image(mark):
    rect = component(ref(blocks[mark], 'm_GameObject'), 'rect')
    parent = ref(blocks[rect], 'm_Father')
    return component(ref(blocks[parent], 'm_GameObject'), 'image')
def health_background(fill):
    fill_go = ref(blocks[fill], 'm_GameObject')
    name = re.search(r'^  m_Name: (.*)', blocks[fill_go], re.M)[1].replace('Current','Background')
    parent = ref(blocks[component(fill_go,'rect')], 'm_Father')
    for i, block in blocks.items():
        if block.startswith('--- !u!1 ') and f'  m_Name: {name}\n' in block and ref(blocks[component(i,'rect')], 'm_Father') == parent:
            return component(i,'image')
    raise ValueError(name)

binding = blocks[2000941248]
new_binding = binding
additions = []
manifest = []
for index, key in enumerate(('Player1Hud', 'Player2Hud')):
    start = binding.index(f'  {key}:\n')
    end = binding.index('  Player2Hud:\n') if index == 0 else len(binding)
    hud = binding[start:end]
    left = ref(hud,'LeftHealth')
    right = ref(hud,'RightHealth')
    left_wins = [int(x) for x in re.findall(r'fileID: (\d+)', hud.split('    LeftWins:\n')[1].split('    RightWins:\n')[0])]
    right_wins = [int(x) for x in re.findall(r'fileID: (\d+)', hud.split('    RightWins:\n')[1])]
    root_go = ref(hud,'RoundRoot')
    root_rect = component(root_go,'rect')
    base = 2001200000 + index*10
    go, rect, canvas, image = base, base+1, base+2, base+3
    assert all(i not in blocks for i in (go,rect,canvas,image))
    template_go = blocks[325277]
    template_rect = blocks[325278]
    template_image = blocks[325279]
    template_canvas = blocks[325280]
    mapping = {325277:go,325278:rect,325279:image,325280:canvas}
    def clone(block):
        return re.sub(r'(?<=&)\d+|(?<=fileID: )\d+', lambda m:str(mapping.get(int(m[0]),int(m[0]))), block)
    new_go = clone(template_go).replace('  m_Name: Win\n','  m_Name: Announcement Image\n')
    new_rect = re.sub(r'm_Father: \{fileID: \d+\}', f'm_Father: {{fileID: {root_rect}}}',clone(template_rect)).replace('m_SizeDelta: {x: 50, y: 50}', 'm_SizeDelta: {x: 640, y: 180}')
    new_image = clone(template_image).replace('  m_Enabled: 1\n','  m_Enabled: 0\n').replace('m_Color: {r: 1, g: 0, b: 0, a: 1}','m_Color: {r: 1, g: 1, b: 1, a: 1}').replace('m_RaycastTarget: 1','m_RaycastTarget: 0').replace('m_PreserveAspect: 0','m_PreserveAspect: 1')
    # New objects have no semantically meaningful trailing-space values.
    additions.extend('\n'.join(line.rstrip() for line in block.splitlines())+'\n' for block in [new_go,new_rect,clone(template_canvas),new_image])
    previous = blocks[root_rect]
    children = re.search(r'  m_Children:\n(?:  - \{fileID: \d+\}\n)+',previous)[0]
    text = text.replace(previous,previous.replace(children,children+f'  - {{fileID: {rect}}}\n'))
    extra = f'    LeftHealthBackground: {{fileID: {health_background(left)}}}\n    RightHealthBackground: {{fileID: {health_background(right)}}}\n    LeftWinBackgrounds:\n'
    extra += ''.join(f'    - {{fileID: {parent_image(mark)}}}\n' for mark in left_wins)
    extra += '    RightWinBackgrounds:\n'+''.join(f'    - {{fileID: {parent_image(mark)}}}\n' for mark in right_wins)
    extra += f'    Announcement: {{fileID: {image}}}\n'
    new_binding = new_binding.replace(hud,hud+extra)
    manifest.append({'hud':key,'announcement':image,'left_background':health_background(left),'right_background':health_background(right)})
new_binding += '  GameplayUIImages:\n'+''.join(f'    {key}: {{fileID: 0}}\n' for key in ['HealthBackground','HealthFill','WinBackground','WinMark','Fight','Round1','Round2','Round3','Round4','Round5','RoundWin','RoundLose','Draw'])+'    AnnouncementSize: {x: 640, y: 180}\n'
text = text.replace(binding,new_binding)
anchor = text.index('--- !u!1660057539')
text = text[:anchor]+''.join(additions)+text[anchor:]
path.write_bytes(text.encode('utf-8'))
print({'before_sha256': hashlib.sha256(original).hexdigest(),'after_sha256':hashlib.sha256(text.encode('utf-8')).hexdigest(),'connections':manifest})
