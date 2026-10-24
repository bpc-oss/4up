$ErrorActionPreference = 'Continue'
# usage: uninstall.ps1 [-PurgeData]
param([switch]$PurgeData)

Get-ScheduledTask -TaskName '4UP' -ErrorAction SilentlyContinue | Unregister-ScheduledTask -Confirm:$false
Get-Process -Name '4up' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Remove-Item (Join-Path ([Environment]::GetFolderPath('Desktop')) '4UP.lnk') -Force -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\4UP.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:LOCALAPPDATA 'Programs\4UP') -Recurse -Force -ErrorAction SilentlyContinue

if ($PurgeData) { Remove-Item "$env:APPDATA\Life4Up" -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host 'UNINSTALLED (user data kept unless -PurgeData)'