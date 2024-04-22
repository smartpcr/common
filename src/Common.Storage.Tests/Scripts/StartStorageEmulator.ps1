$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

function Invoke-WebRequestWithRetry($uri, $outfile)
{
    $try = 1
    while($try -lt 4)
    {
        Try
        {
            Invoke-WebRequest -Uri $uri -OutFile $outfile
            return $true
        }
        Catch [System.IO.IOException]
        {
            Write-Host $_ $try
            $try += 1
        }
        Catch [System.Net.Sockets.SocketException]
        {
            Write-Host $_ $try
            $try += 1
        }
        Catch [System.Net.WebException]
        {
            Write-Host $_ $try
            $try += 1
        }
        Start-Sleep -Seconds 5
    }
    return $false
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

function DownloadAzureStorageEmulator {

    $TmpFolder = $env:TEMP | Resolve-Path
    $TempMsiPath = "$TmpFolder\AzureStorageEmulator"
    $InstallerUri = 'https://download.visualstudio.microsoft.com/download/pr/87453e3b-79ac-4d29-a70e-2a37d39f2b12/f0e339a0a189a0d315f75a72f0c9bd5e/microsoftazurestorageemulator.msi'
    New-Item -ItemType Directory $TempMsiPath -ErrorAction Ignore | Out-Null
    Write-Host "Downloading AzureStorageEmulator to $TempMsiPath\MicrosoftAzureStorageEmulator.msi from $InstallerUri"

    if (Invoke-WebRequestWithRetry -Uri $InstallerUri -OutFile "$TempMsiPath\MicrosoftAzureStorageEmulator.msi")
    {
        Write-Host "Installing $TempMsiPath\MicrosoftAzureStorageEmulator.msi"
        $MsiArgs = "/i `"$TempMsiPath\MicrosoftAzureStorageEmulator.msi`" /quiet"
        Start-Process -FilePath "$env:systemroot\System32\msiexec.exe" -ArgumentList $MsiArgs -Wait -LoadUserProfile -PassThru
    }
    else
    {
        throw "Error occured during download of Azure Storage Emulator"
    }

    Write-Verbose 'Done: Downloading Azure Storage Emulator' -Verbose
}

function StartAzureStorageEmulator {
    Write-Verbose 'Starting Azure Storage Emulator...' -Verbose

    $process = Get-Process | Where-Object { $_.ProcessName -eq 'AzureStorageEmulator' }
    if ($process) {
        Write-Verbose 'The Azure Storage Emulator is already running.' -Verbose
        return
    }

    Write-Verbose 'Initializing Azure Storage Emulator' -Verbose
    $storageEmulatorToolPath="${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
    & "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" init -server '(localdb)\MSSQLLocalDB' -inprocess
    & "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start

    Write-Verbose 'Done: Starting Azure Storage Emulator' -Verbose
}

# Download and install Azure Storage Emulator.
DownloadAzureStorageEmulator

# Start Azure Storage Emulator.
StartAzureStorageEmulator