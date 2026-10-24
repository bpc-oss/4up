$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $here '4up.exe'

# stop old instance first (state is saved every 30s)
Get-Process -Name '4up' -ErrorAction SilentlyContinue | Stop-Process -Force
$s = 0; while((Get-Process -Name '4up' -ErrorAction SilentlyContinue) -and $s -lt 25){ Start-Sleep -Milliseconds 200; $s++ }
# fallback: exe still locked -> kill again and rename aside
if (Test-Path $exe) {
  $free = $false
  try { $fs = [IO.File]::Open($exe, 'Open', 'ReadWrite', 'None'); $fs.Close(); $free = $true } catch {}
  if (-not $free) {
    Get-Process -Name '4up' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
    try { Rename-Item $exe ($exe + '.old') -Force -ErrorAction Stop } catch {}
  }
}

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { Write-Host 'csc.exe not found'; exit 1 }

$srcs = Get-ChildItem -Path $here -Filter '*.cs' | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /platform:anycpu /optimize+ "/out:$exe" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    "/win32icon:$(Join-Path $here 'app.ico')" `
    $srcs
if ($LASTEXITCODE -ne 0) { Write-Host 'BUILD FAILED'; exit $LASTEXITCODE }
if (Test-Path ($exe + '.old')) { Remove-Item ($exe + '.old') -ErrorAction SilentlyContinue }
Write-Host ("BUILD OK -> " + $exe)
# deploy to installed copy (independent desktop app) and restart via task
$inst = Join-Path $env:LOCALAPPDATA 'Programs\4UP'
if (Test-Path (Join-Path $inst '4UP.exe')) {
  Copy-Item $exe (Join-Path $inst '4UP.exe') -Force
  if (Test-Path (Join-Path $here 'copy.txt')) { Copy-Item (Join-Path $here 'copy.txt') $inst -Force -ErrorAction SilentlyContinue }
  Get-Process -Name '4up' -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Milliseconds 800
  Start-ScheduledTask -TaskName '4UP' -ErrorAction SilentlyContinue
  Start-Sleep -Milliseconds 1500
  if (-not (Get-Process -Name '4up' -ErrorAction SilentlyContinue)) { Start-Process -FilePath $exe }
  Write-Host ("DEPLOYED -> " + $inst)
} else {
  Start-ScheduledTask -TaskName '4UP' -ErrorAction SilentlyContinue
  if (-not (Get-Process -Name '4up' -ErrorAction SilentlyContinue)) { Start-Process -FilePath $exe }
}
Write-Host 'STARTED 4up.exe'