"""Mechanical edits in the legacy CP949 PlayerSystem; preserve all unrelated bytes."""
from pathlib import Path

path = Path(__file__).resolve().parents[2] / 'Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs'
source = path.read_bytes().decode('latin1')
anchor = '            filter.Transform->Rotation = FP._0;\r\n'
assert source.count(anchor) == 1
source = source.replace(anchor, anchor + '''
            if (player->isRoundEnd || player->playerHp <= 0)
            {
                if (player->isRoundEnd) LSDFRoundPolicy.FinishPose(ref *player);
                filter.Body->Velocity.X = 0;
                if (filter.Body->Velocity.Y > 0) filter.Body->Velocity.Y = 0;
                return;
            }
'''.replace('\n', '\r\n'))
start = source.index('                if(filter.LSDF_Player->tickToEnableMove-f.Number == LSDFBalance.HealingLead(f))')
end = source.index('                return;', start)
source = source[:start] + source[end:]
path.write_bytes(source.encode('latin1'))
print('PlayerSystem terminal input/motion lock added; automatic healing trigger removed; legacy bytes preserved.')
