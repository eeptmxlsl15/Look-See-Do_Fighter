# One-shot mechanical migration of four jab variants, preserving legacy CP949 bytes.
param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$encoding = [System.Text.Encoding]::GetEncoding(28591)
$pending = @{}
$variants = @(
  @{ Name='Jap'; Frame=10; Reset=$true },
  @{ Name='JapJap'; Frame=10; Reset=$false },
  @{ Name='JapTwo'; Frame=8; Reset=$true },
  @{ Name='JapJapTwo'; Frame=5; Reset=$true }
)
foreach ($variant in $variants) {
  $path = Join-Path $ProjectRoot "Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/$($variant.Name)WindowEvent.cs"
  $source = $encoding.GetString([System.IO.File]::ReadAllBytes($path))
  $anchor = "private const int HitFrame = $($variant.Frame);"
  if (-not $source.Contains($anchor)) { throw "Already migrated/unexpected source: $path" }
  $source = $source.Replace($anchor, "public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFSingleHitDefinition.Create$($variant.Name)();")
  $source = $source.Replace('animationFrame < HitFrame', 'animationFrame < Hit.StartupFrames')
  $source = $source.Replace('animationFrame == HitFrame - 1', 'Hit.IsSpawnFrame(animationFrame)')
  $start = $source.IndexOf('            EntityRef hitbox = f.Create();')
  $end = $source.IndexOf('            if (player->canCounter == true)', $start)
  if ($start -lt 0 -or $end -lt 0) { throw "Missing hitbox extraction anchors: $path" }
  $replacement = ''
  if ($variant.Reset) {
    $replacement += "            player->isJump = false;`r`n            player->isDodgeHigh = false;`r`n"
  }
  $replacement += "            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, Hit);`r`n"
  $source = $source.Substring(0,$start) + $replacement + $source.Substring($end)
  $pending[$path] = $source
}
foreach ($entry in $pending.GetEnumerator()) {
  [System.IO.File]::WriteAllBytes($entry.Key, $encoding.GetBytes($entry.Value))
}
Write-Output 'Migrated four jab variants; legacy encoding and combo control flow preserved.'
