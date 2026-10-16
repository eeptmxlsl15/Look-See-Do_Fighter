"""One-shot mechanical extraction/formatting for the remaining refactor, preserving legacy bytes."""
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SIM = ROOT / 'Assets/QuantumUser/Simulation'
path = SIM / 'LSDF_PlayerSystem.cs'
source = path.read_bytes().decode('latin1')
marker = '        #region '
start = source.index(marker)
end = source.rindex('    }')
contacts = source[start:end]
contacts = re.sub(r'        public void InitiateEffect\(LSDF_HitboxInfo hitbox\)\s*\{\s*\}', '', contacts)
new_path = SIM / 'LSDF_PlayerSystem.Contacts.cs'
assert not new_path.exists(), 'Already extracted'
header = 'using Photon.Deterministic;\r\nusing UnityEngine;\r\nnamespace Quantum.LSDF {\r\n    public unsafe partial class LSDF_PlayerSystem {\r\n'
new_path.write_bytes((header + contacts + '    }\r\n}\r\n').encode('latin1'))
source = source[:start] + source[end:]
# Remove obsolete commented-out input experiments, not executable statements or legacy text encoding.
source = re.sub(r'(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)', '', source)
source = re.sub(r'(?m)^[ \t]*else\s*\r?\n[ \t]*\{\s*\}', '', source)
path.write_bytes(source.encode('latin1'))

paths = [path, new_path, SIM / 'LSDFPlayerMotion.cs']
paths += list((SIM / 'LSDF_Animator_Window_Event/Attack').rglob('*.cs'))
for target in paths:
    text = target.read_bytes().decode('latin1')
    text = re.sub(r'(?m)[ \t]+(?=\r?$)', '', text)
    text = re.sub(r'(\r?\n){3,}', '\r\n\r\n', text)
    target.write_bytes(text.encode('latin1'))

for directory in [SIM, ROOT / 'Assets/QuantumUser/View', ROOT / 'Assets/Tests/Editor', ROOT / 'Assets/Photon/QuantumMenu/Runtime']:
    for target in [*directory.glob('*.cs'), *directory.glob('LSDF*.asmdef')]:
        meta = Path(str(target) + '.meta')
        if not meta.exists():
            guid = uuid.uuid5(uuid.NAMESPACE_URL, target.relative_to(ROOT).as_posix()).hex
            meta.write_text('fileFormatVersion: 2\nguid: ' + guid + '\n', encoding='ascii')
print('Extracted ground/wall partial, collapsed blank lines, and authored missing new-file metas.')
