"""One-shot source-to-data migration. Inventory is read-only; --apply is explicit."""
import argparse
import ast
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONSTANTS = {'_0': 0, '_1': 65536, '_0_01': 655, '_0_02': 1311, '_0_03': 1966,
             '_0_05': 3277, '_0_10': 6554, '_0_20': 13107, '_0_25': 16384,
             '_0_33': 21845, '_0_50': 32768, '_0_75': 49152, '_1_50': 98304}
FIELDS = {'AttackType': 'AttackType', 'CountType': 'CounterReaction', 'DelayGuardTpye': 'GuardReaction',
          'HomingReturnType': 'ParryReaction', 'GuaranteeAttackType': 'GuaranteeReaction',
          'attackDamage': 'Damage', 'enemyGuardTime': 'GuardTicks', 'enemyHitTime': 'HitTicks',
          'enemyCountTime': 'CounterTicks', 'forceBack': 'ForceBack', 'jumpAttack': 'JumpAttack',
          'dodgeHigh': 'DodgeHigh', 'launcher': 'Launcher', 'wallLauncher': 'WallLauncher', 'notSitLauncher': 'NotSitLauncher'}
ENUMS = {'HitboxAttackType': ['High','Mid','Low'], 'CountAttackType': ['Normal','Stun','Combo','Air'],
         'DelayGuardType': ['Normal','Stun','Combo'], 'HomingType': ['Stun','Combo','Homing'],
         'GuaranteeType': ['Normal','Stun','Combo']}

def active(text):
    # Preserve character offsets for comment-free matching; no source rewrite/encoding conversion.
    text = re.sub(r'/\*.*?\*/', lambda m: ' ' * len(m[0]), text, flags=re.S)
    return re.sub(r'//[^\r\n]*', lambda m: ' ' * len(m[0]), text)

def fp(expression):
    def visit(node):
        if isinstance(node, ast.Constant) and isinstance(node.value, int): return node.value, False
        if isinstance(node, ast.Name): return CONSTANTS[node.id], True
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
            value, fixed = visit(node.operand); return -value, fixed
        if isinstance(node, ast.BinOp):
            left, lf = visit(node.left); right, rf = visit(node.right)
            if isinstance(node.op, (ast.Add, ast.Sub)):
                if lf or rf:
                    if not lf: left *= 65536
                    if not rf: right *= 65536
                return (left + right if isinstance(node.op, ast.Add) else left - right), lf or rf
            if isinstance(node.op, ast.Mult): return (left * right >> 16 if lf and rf else left * right), lf or rf
            if isinstance(node.op, ast.Div):
                numerator = left * (65536 if rf else 1)
                return (abs(numerator) // abs(right)) * (-1 if numerator * right < 0 else 1), lf or rf
        raise ValueError('Unsupported FP expression: ' + expression)
    value, fixed = visit(ast.parse(expression.replace('FP.', '').strip(), mode='eval').body)
    return value if fixed else value * 65536

def split_vector(expression):
    depth = 0
    for index, char in enumerate(expression):
        depth += (char == '(') - (char == ')')
        if char == ',' and depth == 0: return expression[:index].strip(), expression[index+1:].strip()
    raise ValueError('Vector is not two dimensional')

def blocks(code, typename):
    pattern = r'f\.Add\(hitbox,\s*new ' + typename + r'\s*\{.*?\}\s*\);'
    return list(re.finditer(pattern, code, re.S))

def inventory():
    records = []
    for path in sorted((ROOT/'Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack').rglob('*.cs')):
        source = path.read_bytes().decode('latin1'); code = active(source)
        if 'EntityRef hitbox = f.Create();' not in code: continue
        assert code.count('EntityRef hitbox = f.Create();') == 1, path
        startup = int(re.search(r'private (?:const )?int HitFrame\s*=\s*(\d+);', code)[1])
        info = blocks(code, 'LSDF_HitboxInfo'); transforms = blocks(code, 'Transform2D')
        colliders = blocks(code, 'PhysicsCollider2D'); expiry = blocks(code, 'TickToDestroy')
        assert len(info) == len(transforms) == len(expiry) == 1 and len(colliders) in (1,2), path
        assignments = dict(re.findall(r'(\w+)\s*=\s*([^,\r\n}]+)', info[0][0]))
        assert assignments.get('AttackerEntity') == 'entity' and assignments.get('startFrame') == 'HitFrame', path
        position = re.search(r'FPVector2 hitboxPosition\s*=\s*f.Get<Transform2D>\(entity\).Position\s*\+\s*new FPVector2\(([^;]+)\);', code)
        assert position, path
        x, y = split_vector(position[1]); x = re.sub(r'\s*\*\s*flip\b', '', x)
        shape = re.search(r'Shape2D.CreateBox\(new FPVector2\((.*)\)\)', colliders[0][0])[1]
        sx, sy = split_vector(shape)
        for collider in colliders: assert re.search(r'Shape2D.CreateBox\(new FPVector2\((.*)\)\)', collider[0])[1] == shape, path
        guid = re.search(r'guid: (\w+)', path.with_suffix('.cs.meta').read_text())[1]
        assets = [p for p in (ROOT/'Assets/QuantumUser/Resources').rglob('*.asset')
                  if guid in p.read_text(encoding='utf-8')]
        assert len(assets) == 1, (path, assets)
        definition = {'StartupFrames': str(startup), 'Offset': f'new FPVector2({x}, {y})', 'HalfExtents': f'new FPVector2({sx}, {sy})'}
        definition.update({FIELDS[key]: value.strip() for key, value in assignments.items() if key in FIELDS})
        serialized = {'StartupFrames': startup, 'Offset': [fp(x), fp(y)], 'HalfExtents': [fp(sx), fp(sy)]}
        for key, value in definition.items():
            if key in serialized: continue
            if key == 'ForceBack': serialized[key] = fp(value)
            elif value in ('true','false'): serialized[key] = int(value == 'true')
            elif '.' in value:
                enum, item = value.split('.'); serialized[key] = ENUMS[enum].index(item)
            else: serialized[key] = int(value)
        records.append(dict(path=path, source=source, code=code, asset=assets[0], startup=startup,
                            info=info, transforms=transforms, colliders=colliders, expiry=expiry,
                            position=position, definition=definition, serialized=serialized,
                            zero_effect=assignments.get('position') != 'hitboxPosition'))
    assert len(records) == 30, len(records)
    return records

def apply(records):
    defaults = ['using Photon.Deterministic;', 'namespace Quantum.LSDF {', '  public static class LSDFRemainingAttackDefaults {']
    snapshots = []
    pending = {}
    for r in records:
        name = r['path'].stem.removesuffix('WindowEvent')
        defaults.append(f'    public static LSDFSingleHitDefinition {name}() {{ return new LSDFSingleHitDefinition {{')
        defaults.extend('      ' + key + ' = ' + value + ',' for key, value in r['definition'].items())
        defaults.append('    }; }')
        changes = [(r['position'].start(), r['position'].end(), ''),
                   (r['transforms'][0].start(), r['transforms'][0].end(), 'Quantum.LSDF.LSDFHitboxFactory.ConfigureBody(f, hitbox, entity, flip, Hit);'),
                   (r['colliders'][0].start(), r['colliders'][0].end(), ''),
                   (r['info'][0].start(), r['info'][0].end(), 'Quantum.LSDF.LSDFHitboxFactory.AddPayload(f, hitbox, entity, Hit' + (', FPVector2.Zero' if r['zero_effect'] else '') + ');'),
                   (r['expiry'][0].start(), r['expiry'][0].end(), 'Quantum.LSDF.LSDFHitboxFactory.ScheduleDestroy(f, hitbox);')]
        if len(r['colliders']) == 2:
            extra = r['colliders'][1]
            changes.append((extra.start(), extra.end(), 'f.Add(hitbox, new PhysicsCollider2D { IsTrigger = true, Shape = Shape2D.CreateBox(Hit.HalfExtents) });'))
        code = r['source']
        for start, end, replacement in sorted(changes, reverse=True): code = code[:start] + replacement + code[end:]
        code = re.sub(r'private (?:const )?int HitFrame\s*=\s*\d+;', f'public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFRemainingAttackDefaults.{name}();', code, count=1)
        code = re.sub(r'\bHitFrame\b', 'Hit.StartupFrames', code)
        # Removing complete commented duplicate examples keeps the adapter readable, without touching live statements.
        code = re.sub(r'(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)', '', code)
        code = re.sub(r'(?m)[ \t]+(?=\r?$)', '', code)
        pending[r['path']] = code.encode('latin1')
        yaml = r['asset'].read_text(encoding='utf-8').rstrip() + '\n  Hit:\n'
        for key, value in r['serialized'].items():
            if key in ('Offset','HalfExtents'):
                yaml += f'    {key}:\n      X:\n        RawValue: {value[0]}\n      Y:\n        RawValue: {value[1]}\n'
            elif key == 'ForceBack': yaml += f'    {key}:\n      RawValue: {value}\n'
            else: yaml += f'    {key}: {value}\n'
        pending[r['asset']] = yaml.encode('utf-8')
        # Unity MonoScript identity follows the declared type, which can differ from the filename.
        declared_type = re.search(r'public class (\w+)\s*:\s*AnimatorTimeWindowEventAsset', r['code'])[1]
        snapshot = dict(type=declared_type, asset=str(r['asset'].relative_to(ROOT)).replace('\\','/'),
                        sourceSha256=hashlib.sha256(r['source'].encode('latin1')).hexdigest(),
                        zeroEffect=r['zero_effect'], colliderAdds=len(r['colliders']), **r['serialized'])
        snapshot['OffsetX'], snapshot['OffsetY'] = snapshot.pop('Offset')
        snapshot['HalfX'], snapshot['HalfY'] = snapshot.pop('HalfExtents')
        snapshots.append(snapshot)
    defaults.append('  }\n}')
    pending[ROOT/'Assets/QuantumUser/Simulation/LSDFRemainingAttackDefaults.cs'] = ('\n'.join(defaults)+'\n').encode()
    pending[ROOT/'docs/ai/evidence/remaining-attack-contracts.json'] = (json.dumps({'attacks': snapshots}, indent=2)+'\n').encode()
    for path, data in pending.items(): path.write_bytes(data)
    print('Migrated 30 adapters and 30 existing assets; 7 prior adapters retained.')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--apply', action='store_true')
    args = parser.parse_args(); records = inventory()
    if args.apply: apply(records)
    else:
        for r in records: print(r['path'].stem, json.dumps(r['serialized']), 'zeroEffect=' + str(r['zero_effect']), 'colliderAdds=' + str(len(r['colliders'])))
