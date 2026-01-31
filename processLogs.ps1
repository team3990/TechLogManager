Write-Output "Tech log processor"
$teamNum = Read-Host "Enter team number"
if ($teamNum.Length -ne 4) {
    Write-Output "Bad team number"
    exit
}
$p1 = $teamNum.Substring(0, 2)
$p2 = $teamNum.Substring(2)
$ip = "10.$p1.$p2.2"
Write-Output "Using IP $ip"

# Get list of remote files with timestamps
$remoteFiles = ssh lvuser@${ip} "find /home/lvuser/logs -name '*.wpilog' -printf '%T@ %p\n'"

if ([string]::IsNullOrWhiteSpace($remoteFiles)) {
    Write-Output "No log files found on robot"
    exit
}

# Process each file
$remoteFiles -split "`n" | ForEach-Object {
    if ($_ -match '^(\d+\.\d+)\s+(.+)$') {
        $timestamp = [DateTimeOffset]::FromUnixTimeSeconds([long]$matches[1]).LocalDateTime
        $remotePath = $matches[2]
        $fileName = Split-Path $remotePath -Leaf

        # Create folder with file's timestamp
        $folderName = $timestamp.ToString("yyyy-MM-dd-HH\hmm") + "@$teamNum"

        Write-Output "Processing $fileName (modified: $timestamp)"
        New-Item -Path ".\" -Name "$folderName" -ItemType "Directory" -Force | Out-Null

        # Download specific file
        scp lvuser@${ip}:$remotePath .\$folderName\
    }
}

# Clean up remote files
ssh lvuser@${ip} "rm -rf /home/lvuser/logs/*"

git add .
$commitDate = Get-Date -Format "yyyy-MM-dd HH:mm"
git commit -m "Added logs for $commitDate"
git push
