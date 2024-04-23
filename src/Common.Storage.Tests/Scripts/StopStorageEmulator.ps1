Write-Verbose 'Stopping Azure Storage Emulator...' -Verbose

$process = Get-Process | Where-Object { $_.ProcessName -eq 'AzureStorageEmulator' }
if (!$process) {
    Write-Verbose 'The Azure Storage Emulator is already stopped.' -Verbose
    return
}


function Is64Bit()
{
    return ([IntPtr]::Size -eq 8)
}

function Get-ProgramFilesDirectory()
{
    if (Is64Bit -eq $true)
    {
        (Get-Item "Env:ProgramFiles(x86)").Value
    }
    else
    {
        (Get-Item "Env:ProgramFiles").Value
    }
}

function Get-StorageEmulatorPath()
{
    $programFilesDirectory = Get-ProgramFilesDirectory

    $storageEmulatorPath = Join-Path $programFilesDirectory "Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"

    if(Test-Path $storageEmulatorPath)
    {
        return $storageEmulatorPath
    }

    throw "Cannot find Storage Emulator"
}


Write-Verbose 'Initializing Azure Storage Emulator' -Verbose
$storageEmulatorToolPath = Get-StorageEmulatorPath
& "$storageEmulatorToolPath" stop

Write-Verbose 'Done: Azure Storage Emulator Stopped' -Verbose