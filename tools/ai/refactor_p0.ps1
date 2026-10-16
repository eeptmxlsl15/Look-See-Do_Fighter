# One-shot, byte-preserving mechanical migration of legacy CP949 source.
# New implementation is in LSDFLoadout/LSDFCombatMath/LSDFSpawnPolicy and the menu partial.
param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$encoding = [System.Text.Encoding]::GetEncoding(28591)
$pending = @{}
function Read-Legacy([string]$relative) {
  return $encoding.GetString([System.IO.File]::ReadAllBytes((Join-Path $ProjectRoot $relative)))
}
function Replace-Required([string]$text, [string]$before, [string]$after) {
  if (-not $text.Contains($before)) { throw "Migration anchor missing: $before" }
  return $text.Replace($before, $after)
}

$eventRoot = 'Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event'
$eventFiles = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot "$eventRoot/Attack") -Recurse -Filter '*.cs')
$eventFiles += Get-Item -LiteralPath (Join-Path $ProjectRoot "$eventRoot/Move/AirWindowEvent.cs")
foreach ($file in $eventFiles) {
  $source = $encoding.GetString([System.IO.File]::ReadAllBytes($file.FullName))
  $source = Replace-Required $source '    private int currentFrame;' ''
  $source = Replace-Required $source 'currentFrame = (int)(layerData->Time.AsFloat * 60.0f);' 'int animationFrame = Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time);'
  # Remaining references in OnEnter are logging only; compute from this layer, never a shared field.
  $source = $source.Replace('{currentFrame}', '{Quantum.LSDF.LSDFCombatMath.AnimationFrame(layerData->Time)}')
  $source = [regex]::Replace($source, '\bcurrentFrame\b', 'animationFrame')
  if ($file.Name -in @('JapWindowEvent.cs','JapJapWindowEvent.cs')) {
    $source = Replace-Required $source '    bool bufferedNextAttack;' ''
    $source = $source.Replace('bufferedNextAttack', 'player->BufferedNextAttack')
  } else {
    # These copies only reset an otherwise unused buffer; commented-out prototypes are removed too.
    $source = [regex]::Replace($source, '(?m)^.*\bbufferedNextAttack\b.*\r?\n', '')
  }
  $source = $source.Replace('        f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player);', '        if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;')
  $pending[$file.FullName] = $source
}

$path = 'Assets/QuantumUser/Simulation/LSDF_Player.qtn'
$source = Read-Legacy $path
$source = Replace-Required $source 'array<int>[28] CommandSkillMap;' "array<int>[28] CommandSkillMap;`r`n    bool BufferedNextAttack; // Per-fighter rollback-owned combo reservation."
$pending[(Join-Path $ProjectRoot $path)] = $source

$path = 'Assets/QuantumUser/Simulation/LSDF_PlayerSpawnSystem.cs'
$source = Read-Legacy $path
$source = Replace-Required $source '            RuntimePlayer data = f.GetPlayerData(player);' "            if (!LSDFSpawnPolicy.CanSpawn(f, player)) return;`r`n            RuntimePlayer data = f.GetPlayerData(player);`r`n            if (data == null || !data.PlayerAvatar.Id.IsValid) return;"
$source = Replace-Required $source '            var playerEntity = f.Create(entityPrototypeAsset);' "            if (entityPrototypeAsset == null) return;`r`n            var playerEntity = f.Create(entityPrototypeAsset);`r`n            if (!f.Has<LSDF_Player>(playerEntity) || !f.Has<AnimatorComponent>(playerEntity) || !f.Has<PhysicsBody2D>(playerEntity)) {`r`n                f.Destroy(playerEntity);`r`n                return;`r`n            }"
$source = Replace-Required $source 'PlayerPrefs.GetInt($"Skill_{i}", 0)' 'LSDFLoadout.ReadSkill(data.CommandSkills, i)'
$source = $source.Replace('i < 28', 'i < LSDFLoadout.SlotCount')
$source = [regex]::Replace($source, '(?m)^.*PlayerPrefs\.SetInt.*\r?\n', '')
# Global round clock is not owned by an individual player joining the session.
$source = Replace-Required $source '            f.Global->Time = 60;' "            if (firstTime && player == (PlayerRef)0) f.Global->Time = 60;"
$pending[(Join-Path $ProjectRoot $path)] = $source

$path = 'Assets/QuantumUser/Simulation/LSDF_PlayerSystem.cs'
$source = Read-Legacy $path
$source = Replace-Required $source '(int)(hitbox->attackDamage * 1.2f)' 'LSDFCombatMath.CounterDamage(hitbox->attackDamage)'
$pending[(Join-Path $ProjectRoot $path)] = $source

$path = 'Assets/QuantumUser/Simulation/LSDF_CollisionSystem.cs'
$source = Read-Legacy $path
$source = Replace-Required $source '        static public bool isGuard;' ''
$source = Replace-Required $source 'isGuard = ShouldGuard(' 'ShouldGuard('
$pending[(Join-Path $ProjectRoot $path)] = $source

# All anchors are prepared before writing. Latin-1 is a reversible byte mapping, NOT a re-encoding.
foreach ($entry in $pending.GetEnumerator()) {
  [System.IO.File]::WriteAllBytes($entry.Key, $encoding.GetBytes($entry.Value))
}
Write-Output "Migrated $($pending.Count) files without changing legacy comment bytes."
