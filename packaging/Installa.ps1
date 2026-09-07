$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'EdgePilot.exe'
$process = Start-Process -FilePath $executable -ArgumentList '--install' -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw 'Installazione non riuscita. Chiudi EdgePilot e riprova.' }
Write-Host 'Installazione completata. Apri EdgePilot dal menu Start.'
