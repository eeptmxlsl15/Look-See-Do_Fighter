# One-shot mechanical extraction; preserve all untouched legacy CP949 bytes.
param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$relative = 'Assets/QuantumUser/Simulation/LSDF_Animator_Window_Event/Attack/Lp/LeftUpperWindowEvent.cs'
$path = Join-Path $ProjectRoot $relative
$encoding = [System.Text.Encoding]::GetEncoding(28591)
$source = $encoding.GetString([System.IO.File]::ReadAllBytes($path))
if (-not $source.Contains('private const int HitFrame = 13;')) { throw 'Already migrated or unexpected source.' }
$source = $source.Replace('private const int HitFrame = 13;', 'public Quantum.LSDF.LSDFSingleHitDefinition Hit = Quantum.LSDF.LSDFSingleHitDefinition.CreateLeftUpper();')
$source = $source.Replace('animationFrame < HitFrame', 'animationFrame < Hit.StartupFrames')
$source = $source.Replace('animationFrame == HitFrame - 1', 'Hit.IsSpawnFrame(animationFrame)')
$start = $source.IndexOf('            EntityRef hitbox = f.Create();')
$end = $source.IndexOf('            if (player->canCounter == true)', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Hitbox extraction anchors missing.' }
$source = $source.Substring(0, $start) + "            Quantum.LSDF.LSDFHitboxFactory.Spawn(f, entity, flip, Hit);`r`n" + $source.Substring($end)
[System.IO.File]::WriteAllBytes($path, $encoding.GetBytes($source))
Write-Output 'Extracted LeftUpper hitbox construction; legacy encoding preserved.'
