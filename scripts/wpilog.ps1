# Imports
. .\scripts\utils.ps1

function WpilogMain
{
    param($teamNumber, $whatAction)

    # Get list of remote files with timestamps
    $remoteFiles = ssh lvuser@roboRIO-${teamNumber}-FRC.local "find /home/lvuser/logs -name '*.wpilog' -printf '%T@ %p\n'"
    Write-Output $remoteFiles

    if ([string]::IsNullOrWhiteSpace($remoteFiles))
    {
        Write-Output "No log files found on robot"
        exit
    }

    if ($whatAction -lt 3)
    {
        # Process each file
        $remoteFiles -split "`n" | ForEach-Object {
            if ($_ -match '^(\d+\.\d+)\s+(.+)$')
            {
                $timestamp = [DateTimeOffset]::FromUnixTimeSeconds([long]$matches[1]).LocalDateTime
                $remotePath = $matches[2]
                $fileName = Split-Path $remotePath -Leaf

                # Create folder with file's timestamp
                $folderName = $timestamp.ToString("yyyy-MM-dd-HH\hmm") + "@$teamNumber"
                Write-Output "Writing to folder $folderName"

                Write-Output "Processing $fileName (modified: $timestamp)"
                New-Directory-Protected $folderName

                # Download specific file
                scp lvuser@${ip}:$remotePath .\$folderName\
            }
        }
    }

    if ($whatAction -eq 1 -or $whatAction -eq 3)
    {
        # Clean up remote files
        ssh lvuser@roboRIO-${teamNumber}-FRC.local "rm -rf /home/lvuser/logs/*"
    }
}
