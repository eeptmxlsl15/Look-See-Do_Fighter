"""One-shot mechanical CommandSystem delegation; preserves the legacy file's original bytes."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/QuantumUser/Simulation/LSDF_CommandSystem.cs'
source = path.read_bytes().decode('latin1')
start = source.index('        public override void Update(')
end = source.index('    public enum CommandDirection', start)
assert 'private void TryTriggerSkill' in source[start:end], 'Already delegated or source changed'
replacement = '''        public override void Update(Frame f, ref Filter filter)
        {
            LSDFCommandProcessor.Update(f, ref filter);
        }
    }

'''.replace('\n', '\r\n')
path.write_bytes((source[:start] + replacement + source[end:]).encode('latin1'))
print('Delegated CommandSystem; existing class, Filter, enums, meta and system order retained.')
