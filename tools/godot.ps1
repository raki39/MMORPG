<#
.SYNOPSIS
  Executa o Godot 4.7.2 .NET para o GodotClient com o ambiente correto (sem abrir o editor).

.EXAMPLE
  ./tools/godot.ps1 run                      # abre o jogo
  ./tools/godot.ps1 run -- --population=2000 # argumentos do jogo vao depois de --
  ./tools/godot.ps1 import                   # importa recursos (headless)
  ./tools/godot.ps1 export                   # gera build/MMOTycoon.exe
  ./tools/godot.ps1 shot saida.png -- --select=mina_velha --sim-minutes=90

.NOTES
  - Caminho do Godot: variavel GODOT_EXE ou o padrao abaixo.
  - O PATH do sistema tem o dotnet x86 (sem SDK) antes do x64; o Godot precisa achar o SDK 10.
  - Remove ELECTRON_RUN_AS_NODE herdada do VS Code.
#>
param(
    [Parameter(Position = 0)][ValidateSet('run', 'import', 'export', 'shot', 'build')][string]$Command = 'run',
    [Parameter(Position = 1, ValueFromRemainingArguments = $true)][string[]]$Rest
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'GodotClient'
$godot = if ($env:GODOT_EXE) { $env:GODOT_EXE } else { 'C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' }
if (-not (Test-Path $godot)) { throw "Godot nao encontrado em '$godot'. Defina GODOT_EXE." }

$env:Path = 'C:\Program Files\dotnet;' + $env:Path
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Remove-Item Env:ELECTRON_RUN_AS_NODE -ErrorAction SilentlyContinue

# Argumentos que comecam com "--" vao para o jogo; os demais sao do script.
# (O PowerShell consome um "--" isolado, entao ele e opcional.)
$gameArgs = @()
$scriptArgs = @()
foreach ($a in $Rest) {
    if ($a -eq '--') { continue }
    if ($a.StartsWith('--')) { $gameArgs += $a } else { $scriptArgs += $a }
}

function Build-Client {
    & 'C:\Program Files\dotnet\dotnet.exe' build (Join-Path $project 'GodotClient.csproj') -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o GodotClient.' }
    # Importa recursos novos (fontes, texturas); rapido quando nada mudou.
    # O Godot escreve logs em stderr: no PowerShell 5.1 isso nao pode ser tratado como erro.
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $godot --headless --path $project --import 2>&1 | Out-Null
    $ErrorActionPreference = $prev
}

$ErrorActionPreference = 'Continue'
switch ($Command) {
    'build' { Build-Client }
    'import' { & $godot --headless --path $project --import }
    'run' {
        Build-Client
        & $godot --path $project -- @gameArgs
    }
    'shot' {
        $out = if ($scriptArgs.Count -gt 0) { [IO.Path]::GetFullPath($scriptArgs[0]) } else { Join-Path $root 'build\captura.png' }
        Build-Client
        & $godot --path $project --resolution 1920x1080 -- "--screenshot=$out" @gameArgs
    }
    'export' {
        Build-Client
        $buildDir = Join-Path $root 'build'
        New-Item -ItemType Directory -Force $buildDir | Out-Null
        & $godot --headless --path $project --export-release 'Windows Desktop' (Join-Path $buildDir 'MMOTycoon.exe')
        # O conteudo do mundo acompanha o executavel (lido de Content/ ao lado do .exe).
        $dest = Join-Path $buildDir 'Content\presets'
        New-Item -ItemType Directory -Force $dest | Out-Null
        Copy-Item (Join-Path $root 'Content\presets\*') $dest -Recurse -Force
        Get-ChildItem $buildDir | Select-Object Name, Length | Format-Table -AutoSize
    }
}
