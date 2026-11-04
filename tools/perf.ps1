<#
.SYNOPSIS
  Verificacao de desempenho do cliente (D-032): mede vistas fixas do mapa sem vsync e falha
  se o p95 do quadro passar do orcamento (padrao: 6,9 ms = 144 Hz).

.EXAMPLE
  ./tools/perf.ps1
  ./tools/perf.ps1 -BudgetMs 8.3 -Seconds 8

.NOTES
  - Os numeros dependem da maquina; o orcamento vale para a maquina do criador.
  - Mede com o mouse real ignorado e o vsync desligado (--measure-seconds).
  - "redesenho" forca o pior caso (cache do mapa redesenhado a cada quadro); fica fora do orcamento.
#>
param(
    [double]$BudgetMs = 6.9,
    [int]$Seconds = 6
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$log = Join-Path $env:TEMP 'mmotycoon_perf'
New-Item -ItemType Directory -Force $log | Out-Null

$cases = @(
    @{ Name = 'mapa inteiro';           Args = @() ;                                              Budget = $true },
    @{ Name = 'perto (Forte)';          Args = @('--focus=forte_do_vau', '--zoom=2.6') ;          Budget = $true },
    @{ Name = 'longe (ilha)';           Args = @('--zoom=0.3') ;                                  Budget = $true },
    @{ Name = 'mapa inteiro 2000 pess.'; Args = @('--population=2000') ;                          Budget = $true },
    @{ Name = 'pior caso: redesenho';   Args = @('--focus=forte_do_vau', '--zoom=2.6', '--measure-redraw') ; Budget = $false }
)

$failed = $false
"{0,-26} {1,8} {2,9} {3,8} {4,8} {5,10}" -f 'Vista', 'FPS', 'quadro', 'p95', 'gpu', 'orcamento'
foreach ($c in $cases) {
    $file = Join-Path $log ("perf_" + ($c.Name -replace '[^a-zA-Z0-9]', '_') + ".log")
    $gameArgs = @("--measure-seconds=$Seconds", '--hour=20') + $c.Args
    & (Join-Path $PSScriptRoot 'godot.ps1') run -- @gameArgs *> $file
    $line = (Select-String -Path $file -Pattern 'FPS_MEDIO' | Select-Object -Last 1).Line
    if (-not $line) { "{0,-26} sem medicao (veja {1})" -f $c.Name, $file; $failed = $true; continue }
    $fps = [double]([regex]::Match($line, 'FPS_MEDIO ([0-9.]+)').Groups[1].Value)
    $frame = [double]([regex]::Match($line, 'quadro_ms=([0-9.]+)').Groups[1].Value)
    $p95 = [double]([regex]::Match($line, 'p95_ms=([0-9.]+)').Groups[1].Value)
    $gpu = [double]([regex]::Match($line, 'gpu_ms=([0-9.]+)').Groups[1].Value)
    $ok = (-not $c.Budget) -or ($p95 -le $BudgetMs)
    if (-not $ok) { $failed = $true }
    $status = if (-not $c.Budget) { 'info' } elseif ($ok) { 'ok' } else { 'ESTOUROU' }
    "{0,-26} {1,8:N0} {2,7:N2}ms {3,6:N2}ms {4,6:N2}ms {5,10}" -f $c.Name, $fps, $frame, $p95, $gpu, $status
}
if ($failed) { "FALHOU: p95 acima de $BudgetMs ms em alguma vista."; exit 1 }
"OK: todas as vistas dentro de $BudgetMs ms (p95)."
