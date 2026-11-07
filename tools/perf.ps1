<#
.SYNOPSIS
  Verificacao de desempenho do cliente (D-032): mede vistas fixas do mapa sem vsync e falha
  se o p95 do quadro passar do orcamento (padrao: 6,9 ms = 144 Hz).

.EXAMPLE
  ./tools/perf.ps1 -Quick          # por etapa: abre o jogo uma vez, na vista mais pesada
  ./tools/perf.ps1                 # completa: todas as vistas
  ./tools/perf.ps1 -BudgetMs 8.3 -Seconds 8

.NOTES
  - Os numeros dependem da maquina; o orcamento vale para a maquina do criador.
  - Compila o cliente uma vez e mede cada vista com o Godot direto (sem recompilar a cada vista).
  - Mede com o mouse real ignorado e o vsync desligado (--measure-seconds).
  - "redesenho" forca o pior caso (cache do mapa redesenhado a cada quadro); fica fora do orcamento.
  - Modo rapido: abre o jogo uma unica vez (seed padrao, 4000 pessoas, no pico do 3o dia as 21 h, 5 s) e
    passa se o p95 ficar dentro do orcamento. Perto do limite ou bem pior que a ultima vez: so avisa.
  - Todas as vistas usam a seed padrao no pico (D-069): e o que o criador ve ao abrir o jogo.
#>
param(
    [double]$BudgetMs = 6.9,
    [int]$Seconds = 6,
    [switch]$Quick
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'GodotClient'
$godot = if ($env:GODOT_EXE) { $env:GODOT_EXE } else { 'C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' }
$env:Path = 'C:\Program Files\dotnet;' + $env:Path
Remove-Item Env:ELECTRON_RUN_AS_NODE -ErrorAction SilentlyContinue
$log = Join-Path $env:TEMP 'mmotycoon_perf'
New-Item -ItemType Directory -Force $log | Out-Null

# Compila e importa uma unica vez.
& (Join-Path $PSScriptRoot 'godot.ps1') build *> (Join-Path $log 'build.log')
if ($LASTEXITCODE -ne 0) { "FALHOU: o cliente nao compilou (veja $log\build.log)."; exit 1 }

function Measure-View($name, $viewArgs, [int]$secs) {
    $file = Join-Path $log ("perf_" + ($name -replace '[^a-zA-Z0-9]', '_') + ".log")
    # 3o dia as 21 h (pico de conectados da abertura padrao).
    $gameArgs = @("--measure-seconds=$secs", '--sim-minutes=4140') + $viewArgs
    & $godot --path $project -- @gameArgs *> $file
    $line = (Select-String -Path $file -Pattern 'FPS_MEDIO' | Select-Object -Last 1).Line
    if (-not $line) { return $null }
    return @{
        Fps = [double]([regex]::Match($line, 'FPS_MEDIO ([0-9.]+)').Groups[1].Value)
        Frame = [double]([regex]::Match($line, 'quadro_ms=([0-9.]+)').Groups[1].Value)
        P95 = [double]([regex]::Match($line, 'p95_ms=([0-9.]+)').Groups[1].Value)
        Gpu = [double]([regex]::Match($line, 'gpu_ms=([0-9.]+)').Groups[1].Value)
        File = $file
    }
}

$header = "{0,-26} {1,8} {2,9} {3,8} {4,8} {5,10}" -f 'Vista', 'FPS', 'quadro', 'p95', 'gpu', 'orcamento'

if ($Quick) {
    # Uma unica abertura do jogo: a vista mais pesada. Nunca abre o jogo de novo sozinho.
    $lastFile = Join-Path $log 'quick_last_padrao.txt'
    $last = if (Test-Path $lastFile) { [double](Get-Content $lastFile -Raw) } else { 0 }
    $m = Measure-View 'rapido seed padrao' @() 5
    $header
    if ($null -eq $m) { "FALHOU (rapido): sem medicao (veja o log em $log)."; exit 1 }
    "{0,-26} {1,8:N0} {2,7:N2}ms {3,6:N2}ms {4,6:N2}ms {5,10}" -f 'pico seed padrao', $m.Fps, $m.Frame, $m.P95, $m.Gpu, 'rapido'
    Set-Content -Path $lastFile -Value $m.P95.ToString([Globalization.CultureInfo]::InvariantCulture) -Encoding ascii
    if ($m.P95 -gt $BudgetMs) { "FALHOU (rapido): p95 {0:N2} ms acima do orcamento de {1} ms. Rode a verificacao completa (sem -Quick)." -f $m.P95, $BudgetMs; exit 1 }
    $notes = @()
    if ($m.P95 -gt $BudgetMs * 0.85) { $notes += "perto do limite" }
    if ($last -gt 0 -and $m.P95 -gt $last * 1.3) { $notes += ("30% acima da ultima medicao ({0:N2} ms)" -f $last) }
    if ($notes.Count -gt 0) { "ATENCAO (rapido): p95 {0:N2} ms, {1}. Se repetir, rode a verificacao completa." -f $m.P95, ($notes -join '; '); exit 0 }
    "OK (rapido): p95 {0:N2} ms dentro de {1} ms{2}." -f $m.P95, $BudgetMs, $(if ($last -gt 0) { " (anterior {0:N2} ms)" -f $last } else { '' })
    exit 0
}

$cases = @(
    @{ Name = 'mapa inteiro';           Args = @() ;                                              Budget = $true },
    @{ Name = 'perto (Forte)';          Args = @('--focus=forte_do_vau', '--zoom=2.6') ;          Budget = $true },
    @{ Name = 'longe (ilha)';           Args = @('--zoom=0.3') ;                                  Budget = $true },
    @{ Name = 'mapa inteiro 5000 pess.'; Args = @('--population=5000') ;                          Budget = $false },
    @{ Name = 'pior caso: redesenho';   Args = @('--focus=forte_do_vau', '--zoom=2.6', '--measure-redraw') ; Budget = $false }
)

$failed = $false
$header
foreach ($c in $cases) {
    $m = Measure-View $c.Name $c.Args $Seconds
    if ($null -eq $m) { "{0,-26} sem medicao (veja o log em {1})" -f $c.Name, $log; $failed = $true; continue }
    $ok = (-not $c.Budget) -or ($m.P95 -le $BudgetMs)
    if (-not $ok) { $failed = $true }
    $status = if (-not $c.Budget) { 'info' } elseif ($ok) { 'ok' } else { 'ESTOUROU' }
    "{0,-26} {1,8:N0} {2,7:N2}ms {3,6:N2}ms {4,6:N2}ms {5,10}" -f $c.Name, $m.Fps, $m.Frame, $m.P95, $m.Gpu, $status
}
if ($failed) { "FALHOU: p95 acima de $BudgetMs ms em alguma vista."; exit 1 }
"OK: todas as vistas dentro de $BudgetMs ms (p95)."
