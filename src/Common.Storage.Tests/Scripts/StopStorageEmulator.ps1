Write-Verbose 'Starting Azure Storage Emulator...' -Verbose

$process = Get-Process | Where-Object { $_.ProcessName -eq 'AzureStorageEmulator' }
if (!$process) {
    Write-Verbose 'The Azure Storage Emulator is already stopped.' -Verbose
    return
}

Write-Verbose 'Initializing Azure Storage Emulator' -Verbose
$storageEmulatorToolPath="${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
& "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" stop

Write-Verbose 'Done: Azure Storage Emulator Stopped' -Verbose