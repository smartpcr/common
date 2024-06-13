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

    if (Test-Path $storageEmulatorPath)
    {
        return $storageEmulatorPath
    }

    return $null
}

function Get-SqlLocalDbPath()
{
    # Define potential paths where SqlLocalDB.exe might be located
    $paths = @(
        "$env:ProgramFiles\Microsoft SQL Server\120\Tools\Binn",  # SQL Server 2014 default path
        "$env:ProgramFiles\Microsoft SQL Server\130\Tools\Binn",  # SQL Server 2016 default path
        "$env:ProgramFiles\Microsoft SQL Server\140\Tools\Binn",  # SQL Server 2017 default path
        "$env:ProgramFiles\Microsoft SQL Server\150\Tools\Binn",  # SQL Server 2019 default path
        "$env:ProgramFiles\Microsoft SQL Server\160\Tools\Binn",  # SQL Server 2022 default path
        "$env:ProgramFiles(x86)\Microsoft SQL Server\120\Tools\Binn",  # For 32-bit SQL on 64-bit system
        "$env:ProgramFiles(x86)\Microsoft SQL Server\130\Tools\Binn",
        "$env:ProgramFiles(x86)\Microsoft SQL Server\140\Tools\Binn",
        "$env:ProgramFiles(x86)\Microsoft SQL Server\150\Tools\Binn",
        "$env:ProgramFiles(x86)\Microsoft SQL Server\160\Tools\Binn"
    )

    # Check each path for SqlLocalDB.exe
    foreach ($path in $paths) {
        $exePath = Join-Path -Path $path -ChildPath "SqlLocalDB.exe"
        if (Test-Path $exePath) {
            Write-Host "SQL LocalDB is installed at: $exePath"
            $sqlLocalDbInstallPath = $exePath
            break
        }
    }

    if (-not (Test-Path $exePath)) {
        Write-Host "SQL LocalDB is not installed."
        return $null
    }

    return $exePath
}

function InstallSqlLocalDb()
{
    $isSqlLocalDbInstalled = $false
    try {
        $output = sqllocaldb info
        if ($output) {
            Write-Host "SQL LocalDB is installed. Available instances:"
            Write-Host $output
            $isSqlLocalDbInstalled = $true
        }
    } catch {
        Write-Host "SQL LocalDB is not installed."
    }

    if ($isSqlLocalDbInstalled) {
        return
    }

    Write-Verbose 'Downloading the SqlLocal' -Verbose
    $downloadLink2016 = "https://download.microsoft.com/download/9/0/7/907AD35F-9F9C-43A5-9789-52470555DB90/ENU/SqlLocalDB.msi"
    $downloadLink2017 = "https://download.microsoft.com/download/E/F/2/EF23C21D-7860-4F05-88CE-39AA114B014B/SqlLocalDB.msi"
    $downloadLink2019 = "https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi"
    $downloadLink2022 = "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi"
    $downloadLinks = @(
        @{
            Version="2016"
            Link=$downloadLink2016
            InstallFile="SqlLocalDB-2016.msi"
        },
        @{
            Version="2017"
            Link=$downloadLink2017
            InstallFile="SqlLocalDB-2017.msi"
        },
        @{
            Version="2019"
            Link=$downloadLink2019
            InstallFile="SqlLocalDB-2019.msi"
        },
        @{
            Version="2022"
            Link=$downloadLink2022
            InstallFile="SqlLocalDB-2022.msi"
        }
    )

    $installSuccessful = $false
    foreach ($downloadLink in $downloadLinks)
    {
        Write-Verbose "Installing SqlLocal $($downloadLink.Version)" -Verbose
        Invoke-WebRequest -Uri $downloadLink.Link -OutFile $downloadLink.InstallFile
        try
        {
            $ExitCode = (Start-Process -FilePath "$env:systemroot\System32\msiexec.exe" -ArgumentList "/i $($downloadLink.InstallFile) /quiet /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES /l*v SqlLocalDBInstall.log" -Wait -LoadUserProfile -PassThru).ExitCode
            if ($ExitCode -eq 0)
            {
                $sqlLocalDbToolPath = Get-SqlLocalDbPath
                if ($null -eq $sqlLocalDbToolPath -or (-not (Test-Path $sqlLocalDbToolPath)))
                {
                    throw "Cannot find SqlLocalDB"
                }

                try
                {
                    & "$sqlLocalDbToolPath" create MSSQLLocalDB
                    & "$sqlLocalDbToolPath" start MSSQLLocalDB
                    $installSuccessful = $true
                }
                catch
                {
                    Write-Verbose "Rollback SqlLocal install with version $($downloadLink.Version)" -Verbose
                    Start-Process -FilePath "$env:systemroot\System32\msiexec.exe" -ArgumentList "/x $($downloadLink.InstallFile) /qn /norestart" -Wait
                }

            }
            switch ($ExitCode)
            {
                0       { "{0} - SqlLocal Installation succeeded!" -f (Get-Date) | Write-Verbose -Verbose }
                1603    { "{0} - SqlLocal Error '1603' encountered... invalid version or install as administrator." -f (Get-Date) | Write-Warning -WarningAction Continue }
                1618    { "{0} - SqlLocal Error '1618' encoutnered - an installation is already in-progress. This can also occur if your machine has a pending reboot or updates to install. See '{2}.log' for additional details." -f (Get-Date), $ExitCode, $Msi.FullName | Write-Error -ErrorAction Stop }
                Default { "{0} - SqlLocal Installation failed with exit code '{1}' - see '{2}.log' for details." -f (Get-Date), $ExitCode, $Msi.FullName | Write-Error -ErrorAction Stop }
            }
        }
        catch
        {
            Write-Warning "Failed to install sqllocal with version $($downloadLink.Version)" -WarningAction Continue
        }

        if ($installSuccessful)
        {
            return
        }
    }
}

function InstallAzureStorageEmulator {
    $storageEmulatorToolPath = Get-StorageEmulatorPath
    if ($null -ne $storageEmulatorToolPath -and (Test-Path $storageEmulatorToolPath)) {
        Write-Host 'Azure Storage Emulator is already installed.'
        return
    }

    $TmpFolder = $env:TEMP | Resolve-Path
    $TempMsiPath = "$TmpFolder\AzureStorageEmulator"
    $InstallerUri = 'https://download.visualstudio.microsoft.com/download/pr/87453e3b-79ac-4d29-a70e-2a37d39f2b12/f0e339a0a189a0d315f75a72f0c9bd5e/microsoftazurestorageemulator.msi'
    New-Item -ItemType Directory $TempMsiPath -ErrorAction Ignore | Out-Null
    Write-Host "Downloading AzureStorageEmulator to $TempMsiPath\MicrosoftAzureStorageEmulator.msi from $InstallerUri"

    if (Invoke-WebRequestWithRetry -Uri $InstallerUri -OutFile "$TempMsiPath\MicrosoftAzureStorageEmulator.msi")
    {
        Write-Host "Installing $TempMsiPath\MicrosoftAzureStorageEmulator.msi"
        $MsiArgs = "/i `"$TempMsiPath\MicrosoftAzureStorageEmulator.msi`" /quiet"
        $ExitCode = (Start-Process -FilePath "$env:systemroot\System32\msiexec.exe" -ArgumentList $MsiArgs -Wait -LoadUserProfile -PassThru).ExitCode
        switch ($ExitCode)
        {
            0       { "{0} - SqlLocal Installation succeeded!" -f (Get-Date) | Write-Verbose -Verbose }
            1603    { "{0} - SqlLocal Error '1603' encountered... installation need to be run as administrator." -f (Get-Date) | Write-Error -ErrorAction Stop }
            1618    { "{0} - SqlLocal Error '1618' encoutnered - an installation is already in-progress. This can also occur if your machine has a pending reboot or updates to install. See '{2}.log' for additional details." -f (Get-Date), $ExitCode, $Msi.FullName | Write-Error -ErrorAction Stop }
            Default { "{0} - SqlLocal Installation failed with exit code '{1}' - see '{2}.log' for details." -f (Get-Date), $ExitCode, $Msi.FullName | Write-Error -ErrorAction Stop }
        }
    }
    else
    {
        throw "Error occured during download of Azure Storage Emulator"
    }

    Write-Verbose 'Done: Successfully installed Azure Storage Emulator' -Verbose
}

function StartAzureStorageEmulator {
    Write-Verbose 'Starting Azure Storage Emulator...' -Verbose

    $process = Get-Process | Where-Object { $_.ProcessName -eq 'AzureStorageEmulator' }
    if ($process) {
        Write-Verbose 'The Azure Storage Emulator is already running.' -Verbose
        return
    }

    Write-Verbose 'Initializing SQL Server Local DB' -Verbose
    $sqlLocalDbToolPath = Get-SqlLocalDbPath
    if ($null -eq $sqlLocalDbToolPath -or (-not (Test-Path $sqlLocalDbToolPath)))
    {
        throw "Cannot find SqlLocalDB"
    }

    $localDbInstances = & "$sqlLocalDbToolPath" info
    if ($null -eq $localDbInstances -or $localDbInstances -inotlike "MSSQLLocalDB")
    {
        & "$sqlLocalDbToolPath" create MSSQLLocalDB
        & "$sqlLocalDbToolPath" start MSSQLLocalDB
    }

    Write-Verbose 'Initializing Azure Storage Emulator' -Verbose
    $storageEmulatorToolPath = Get-StorageEmulatorPath
    & "$storageEmulatorToolPath" init -server '(localdb)\MSSQLLocalDB' -inprocess
    & "$storageEmulatorToolPath" start

    Write-Verbose 'Done: Starting Azure Storage Emulator' -Verbose
}

# Install LocalDB
InstallSqlLocalDb

# Download and install Azure Storage Emulator.
InstallAzureStorageEmulator

# Start Azure Storage Emulator.
StartAzureStorageEmulator