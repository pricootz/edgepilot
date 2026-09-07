$ErrorActionPreference = 'Stop'
$process = Start-Process -FilePath (Join-Path $PSScriptRoot 'EdgePilot.exe') -ArgumentList '--install' -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw 'EdgePilot installation failed.' }
Write-Host 'EdgePilot installed. Open it from the Start menu.'
