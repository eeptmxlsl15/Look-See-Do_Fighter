# One-shot mechanical split. Keep legacy CP949 bytes and the existing system identity.
param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$byteEncoding = [System.Text.Encoding]::GetEncoding(28591)
$mainPath = Join-Path $ProjectRoot 'Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs'
$combatPath = Join-Path $ProjectRoot 'Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs'
$spawnPath = Join-Path $ProjectRoot 'Assets/QuantumUser/Simulation/LSDF_PlayerSpawnSystem.cs'
if (Test-Path -LiteralPath $combatPath) { throw 'Already migrated; combat partial exists.' }
$source = $byteEncoding.GetString([System.IO.File]::ReadAllBytes($mainPath))
$declaration = 'public unsafe class LSDF_PlayerSystem :'
if ([regex]::Matches($source, [regex]::Escape($declaration)).Count -ne 1) { throw 'Unexpected class declaration.' }
$start = $source.IndexOf('        public void OnTriggerNormalHit(')
$end = $source.IndexOf('        #region ', $start)
if ($start -lt 0 -or $end -lt $start) { throw 'Combat method boundary missing.' }
$combat = $source.Substring($start, $end - $start)
$source = $source.Substring(0, $start) + $source.Substring($end)
$source = $source.Replace($declaration, 'public unsafe partial class LSDF_PlayerSystem :')
$source = [regex]::Replace($source, '(?m)^(    public unsafe partial class LSDF_PlayerSystem :[^\r\n]*?)[ \t]+(?=\r?$)', '$1')

$recoveryStart = $source.IndexOf('            if(player->playerHp < 0)')
$lockStart = $source.IndexOf('            if (f.Number < filter.LSDF_Player->tickToEnableMove)', $recoveryStart)
$recoveryEnd = $source.LastIndexOf('            //', $lockStart)
if ($recoveryStart -lt 0 -or $recoveryEnd -le $recoveryStart) { throw 'Health update boundary missing.' }
$source = $source.Substring(0, $recoveryStart) + "            LSDFPlayerHealth.UpdateRecovery(ref *player);`r`n" + $source.Substring($recoveryEnd)

$damageStart = $combat.IndexOf('            if (player->hitCount == 0)')
if ($damageStart -lt 0) { throw 'Normal damage boundary missing.' }
$combat = $combat.Insert($damageStart, "            LSDFPlayerHealth.ApplyNormalHit(ref *player, hitbox->attackDamage);`r`n")
$normalAssignments = @(
  'player->playerHp -= hitbox->attackDamage;',
  'player->playerHp -=(int)(hitbox->attackDamage*FP._0_75);',
  'player->playerHp -= (int)(hitbox->attackDamage * FP._0_50);',
  'player->playerHp -= (int)(hitbox->attackDamage * FP._0_33);'
)
foreach ($assignment in $normalAssignments) {
  if ([regex]::Matches($combat, [regex]::Escape($assignment)).Count -ne 1) { throw 'Unexpected normal damage assignment.' }
  $combat = $combat.Replace($assignment, '')
}
$counterAssignment = 'player->playerHp -= LSDFCombatMath.CounterDamage(hitbox->attackDamage);'
if ([regex]::Matches($combat, [regex]::Escape($counterAssignment)).Count -ne 1) { throw 'Counter damage anchor missing.' }
$combat = $combat.Replace($counterAssignment, 'LSDFPlayerHealth.ApplyCounterHit(ref *player, hitbox->attackDamage);')
# Only trailing horizontal whitespace is normalized in the newly moved file.
$combat = [regex]::Replace($combat, '(?m)[ \t]+(?=\r?$)', '')
$combatSource = "using Photon.Deterministic;`r`nusing UnityEngine;`r`n`r`nnamespace Quantum.LSDF`r`n{`r`n    public unsafe partial class LSDF_PlayerSystem`r`n    {`r`n" + $combat + "    }`r`n}`r`n"

$spawn = $byteEncoding.GetString([System.IO.File]::ReadAllBytes($spawnPath))
$spawnAssignment = 'LSDF_player->playerHp = 170;'
if ([regex]::Matches($spawn, [regex]::Escape($spawnAssignment)).Count -ne 1) { throw 'Spawn HP anchor missing.' }
$spawn = $spawn.Replace($spawnAssignment, 'LSDF_player->playerHp = LSDFPlayerHealth.Maximum;')
# Resolve all anchors before writing any target. SDK, QTN and system configuration stay untouched.
[System.IO.File]::WriteAllBytes($mainPath, $byteEncoding.GetBytes($source))
[System.IO.File]::WriteAllBytes($combatPath, $byteEncoding.GetBytes($combatSource))
[System.IO.File]::WriteAllBytes($spawnPath, $byteEncoding.GetBytes($spawn))
Write-Output 'Split combat signal handlers and health rules; original Frame state and call order preserved.'
