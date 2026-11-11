$ErrorActionPreference = 'Stop'
$backgroundRoot = 'C:/Users/Admin/Look-See-Do_Fighter'
$backgroundBuild = Join-Path $backgroundRoot 'tmp/infinite-stage-background'
$backgroundCompiler = 'C:/Program Files/dotnet/sdk/9.0.100/Roslyn/bincore/csc.dll'
[void][IO.Directory]::CreateDirectory($backgroundBuild)
$backgroundReports = [Collections.Generic.List[object]]::new()
function Compile-BackgroundAssembly([string]$projectFile, [string[]]$selectedSources) {
  [xml]$backgroundProject = Get-Content -Raw -LiteralPath (Join-Path $backgroundRoot $projectFile)
  $backgroundAssembly = [IO.Path]::GetFileNameWithoutExtension($projectFile)
  $backgroundReferences = @{}
  foreach ($reference in $backgroundProject.Project.ItemGroup.Reference) {
    if (-not $reference.HintPath) { continue }
    $referencePath = [string]$reference.HintPath
    if (-not [IO.Path]::IsPathRooted($referencePath)) { $referencePath = Join-Path $backgroundRoot $referencePath }
    $backgroundReferences[[IO.Path]::GetFileNameWithoutExtension($referencePath)] = $referencePath
  }
  foreach ($projectReference in $backgroundProject.Project.ItemGroup.ProjectReference) {
    if (-not $projectReference.Name) { continue }
    $referenceName = [string]$projectReference.Name
    $referencePath = Join-Path $backgroundBuild ($referenceName + '.dll')
    if (-not (Test-Path -LiteralPath $referencePath)) { $referencePath = Join-Path $backgroundRoot ('Library/ScriptAssemblies/' + $referenceName + '.dll') }
    $backgroundReferences[$referenceName] = $referencePath
  }
  foreach ($referencePath in $backgroundReferences.Values) {
    if (-not (Test-Path -LiteralPath $referencePath)) { throw ('Missing binary reference: ' + $referencePath) }
  }
  $backgroundSources = if ($selectedSources.Count) { $selectedSources } else {
    @($backgroundProject.Project.ItemGroup.Compile | ForEach-Object { if ($_.Include) { [string]$_.Include } })
  }
  $backgroundDefinitions = @($backgroundProject.Project.PropertyGroup | ForEach-Object { if ($_.DefineConstants) { [string]$_.DefineConstants } }) -join ';'
  $backgroundOutput = Join-Path $backgroundBuild ($backgroundAssembly + '.dll')
  $backgroundArguments = @('/nostdlib+', '/target:library', '/unsafe+', '/langversion:9.0', '/deterministic+', ('/out:"' + $backgroundOutput + '"'), ('/define:' + $backgroundDefinitions))
  $backgroundArguments += @($backgroundReferences.Values | Sort-Object | ForEach-Object { '/reference:"' + $_ + '"' })
  $backgroundArguments += @($backgroundSources | ForEach-Object { '"' + (Join-Path $backgroundRoot $_) + '"' })
  $backgroundResponse = Join-Path $backgroundBuild ($backgroundAssembly + '.rsp')
  [IO.File]::WriteAllLines($backgroundResponse, $backgroundArguments, [Text.UTF8Encoding]::new($false))
  $backgroundCompilerOutput = & dotnet $backgroundCompiler /noconfig ('@' + $backgroundResponse) 2>&1
  $backgroundExit = $LASTEXITCODE
  $backgroundCompilerOutput | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $backgroundBuild ($backgroundAssembly + '.log'))
  $backgroundReports.Add([pscustomobject]@{assembly=$backgroundAssembly;sourceCount=$backgroundSources.Count;exitCode=$backgroundExit;status=$(if($backgroundExit -eq 0){'PASS'}else{'FAIL'});output=$backgroundOutput})
  Write-Output ($backgroundAssembly + ': ' + $backgroundExit + ', sources=' + $backgroundSources.Count)
  if ($backgroundExit -ne 0) { $backgroundCompilerOutput | Select-Object -First 25 | Write-Output; throw 'Managed compilation failed; no automatic retry' }
}
try {
  Compile-BackgroundAssembly 'LSDF.View.csproj' @()
  Compile-BackgroundAssembly 'LSDF.View.Editor.csproj' @('Assets/QuantumUser/View/Editor/LSDFStageBackgroundEditor.cs')
  Compile-BackgroundAssembly 'LSDF.Tests.csproj' @('Assets/Tests/Editor/LSDFStageBackgroundTests.cs')
} finally {
  $backgroundReports | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $backgroundBuild 'managed-compilation.json')
}
