$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dest = Join-Path $env:LOCALAPPDATA 'Programs\4UP'
New-Item -ItemType Directory -Force -Path $dest | Out-Null

# 1) runtime files
Copy-Item (Join-Path $here '4up.exe') (Join-Path $dest '4UP.exe') -Force
if (Test-Path (Join-Path $here 'copy.txt')) { Copy-Item (Join-Path $here 'copy.txt') $dest -Force }

# 2) watchdog bound to install dir
$watch = "if (-not (Get-Process -Name '4up' -ErrorAction SilentlyContinue)) { Start-Process -FilePath '$dest\4UP.exe' }"
[IO.File]::WriteAllText((Join-Path $dest 'watch.ps1'), $watch, [System.Text.UTF8Encoding]::new($false))

# 3) shortcuts (desktop + start menu)
$ws = New-Object -ComObject WScript.Shell
$links = @(
  (Join-Path ([Environment]::GetFolderPath('Desktop')) '4UP.lnk'),
  (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\4UP.lnk')
)
foreach ($lnk in $links) {
  $s = $ws.CreateShortcut($lnk)
  $s.TargetPath = Join-Path $dest '4UP.exe'
  $s.WorkingDirectory = $dest
  $s.Save()
}

# 4) scheduled task retarget (logon + 5min watchdog)
$act = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$dest\watch.ps1`""
$t1 = New-ScheduledTaskTrigger -AtLogOn
$t2 = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 5) -RepetitionDuration (New-TimeSpan -Days 3650)
$set = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::FromMinutes(5))
Register-ScheduledTask -TaskName '4UP' -Action $act -Trigger @($t1,$t2) -Settings $set -Force | Out-Null

# 5) restart as task child (independent of any terminal)
Get-Process -Name '4up' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
Start-ScheduledTask -TaskName '4UP'
Start-Sleep -Seconds 3

# 6) promote tray icon for the new exe path
Get-ChildItem 'HKCU:\Control Panel\NotifyIconSettings' -ErrorAction SilentlyContinue | ForEach-Object {
  $p = Get-ItemProperty $_.PSPath
  if ($p.ExecutablePath -like "$dest*") { Set-ItemProperty $_.PSPath -Name IsPromoted -Value 1 }
}

Write-Host "INSTALLED -> $dest"
Write-Host ("proc alive: " + [bool](Get-Process -Name '4up' -ErrorAction SilentlyContinue))