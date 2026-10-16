# One-shot, byte-preserving extraction of NarakTwo/LarsCutKick hit materialization.
param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$encoding = [System.Text.Encoding]::GetEncoding(28591)
$pending = @{}
$variants = @(
  @{Path='Rk/NarakTwo'; Startup=18; Second=35; Prefix='Narak'},
  @{Path='Lk/LarsCutKick'; Startup=16; Second=25; Prefix='Lars'}
)
foreach ($variant in $variants) {
  $path=Join-Path $ProjectRoot "Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/$($variant.Path)WindowEvent.cs"
  $source=$encoding.GetString([System.IO.File]::ReadAllBytes($path))
  $anchor="private const int HitFrame = $($variant.Startup);"
  if(-not $source.Contains($anchor)){throw "Already migrated or unexpected source: $path"}
  $replacement="public Quantum.LSDF.LSDFTimedHitDefinition FirstHit = Quantum.LSDF.LSDFTimedHitDefinition.Create$($variant.Prefix)First();`r`n    public Quantum.LSDF.LSDFTimedHitDefinition SecondHit = Quantum.LSDF.LSDFTimedHitDefinition.Create$($variant.Prefix)Second();"
  $source=$source.Replace($anchor,$replacement)
  $source=$source.Replace('animationFrame == HitFrame - 1','FirstHit.IsSpawnFrame(animationFrame)')
  $source=$source.Replace('animationFrame == HitFrame-1','FirstHit.IsSpawnFrame(animationFrame)')
  $source=$source.Replace("animationFrame == $($variant.Second)",'SecondHit.IsSpawnFrame(animationFrame)')
  $source=$source.Replace('animationFrame < HitFrame - 1','animationFrame < FirstHit.Hit.StartupFrames - 1')
  # Work backwards so the first replacement cannot invalidate the second block's index.
  for($hitIndex=1;$hitIndex -ge 0;$hitIndex--){
    $positions=[regex]::Matches($source,'            EntityRef hitbox = f.Create\(\);')
    if($positions.Count -ne ($hitIndex+1)){throw "Unexpected hitbox count: $path"}
    $start=$positions[$hitIndex].Index
    $end=$source.IndexOf('            if (player->canCounter == true)',$start)
    if($end -lt 0){throw "Missing counter-reset anchor: $path"}
    $field=if($hitIndex -eq 0){'FirstHit'}else{'SecondHit'}
    $override=if($variant.Prefix -eq 'Lars' -and $hitIndex -eq 0){', effectPositionOverride: FPVector2.Zero'}else{''}
    $body="            player->isJump = false;`r`n            player->isDodgeHigh = false;`r`n            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, $field.Hit$override);`r`n"
    $source=$source.Substring(0,$start)+$body+$source.Substring($end)
  }
  $pending[$path]=$source
}
foreach($entry in $pending.GetEnumerator()){
  [System.IO.File]::WriteAllBytes($entry.Key,$encoding.GetBytes($entry.Value))
}
Write-Output 'Migrated NarakTwo and LarsCutKick hit definitions; scheduling, movement and legacy effect placement preserved.'
