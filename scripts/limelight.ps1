# Imports
Add-Type -Path ".\TechLogManager\TechLogManager.dll"
. .\scripts\utils.ps1

function LimelightMain
{
    param($teamNumber, $whatAction)

    $limelightsString = ssh lvuser@roboRIO-${teamNumber}-FRC.local "cat /home/lvuser/limelights.json"
    $limeligts = [TechLogManager.Utils]::ParseJsonStringList($limelightsString)
    Write-Output "limelights :" $limelights
    for ($i = 0; $i -lt $limeligts.Count; $i++)
    {
        Limelight $teamNumber $whatAction $limeligts[$i]
    }
}

function Limelight()
{
    param($teamNumber, $whatAction, $llname)
    Write-Output $llname

    if ($whatAction -lt 2)
    {
        $links = [TechLogManager.Limelight]::GetRecordingLinks($llname)
        Write-Output "links :" $links
        $date = Get-Date -Format "yyyy-MM-dd-HH\hmm"
        $baseFolder = "$date@$teamNumber"

        for ($i = 0; $i -lt $links.Count; $i++)
        {
            $folderName = "$baseFolder\rec$($i + 1)"

            New-Directory-Protected $folderName
            Write-Output "Writing to folder $folderName"

            $recording = $links[$i]

            # Download video
            if ($recording.video)
            {
                Write-Output "Downloading video"
                Invoke-WebRequest -Uri $recording.video -OutFile "$folderName\video.avi"
            }

            # Download manifest
            if ($recording.manifest)
            {
                Write-Output "Downloading manifest"
                Invoke-WebRequest -Uri $recording.manifest -OutFile "$folderName\manifest.jsonl"
            }

            # Download bootlog
            if ($recording.bootlog)
            {
                Write-Output "Downloading bootlog"
                Invoke-WebRequest -Uri $recording.bootlog -OutFile "$folderName\bootlog.txt.gz"
            }

            Write-Host "Downloaded ll files to $folderName"
        }
    }

    if ($whatAction -eq 0 -or $whatAction -eq 2)
    {
        # Clean up remote files
        [TechLogManager.Limelight]::DeleteAllVideos($llname)
    }
}
