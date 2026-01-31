Write-Output "Tech log processor"

$teamNum = Read-Host "Enter team number"

if ($teamNum.Length -ne 4)
{# If length not equal 4
    Write-Output "Bad team number"
    exit
}

$p1 = $teamNum.Substring(0, 2)
$p2 = $teamNum.Substring(2)
$ip = "10.$p1.$p2.2"

Write-Output "Using IP $ip"

$date = Get-Date -Format "yyyy-MM-dd-HH\hmm"
$folder = "$date@$teamNum"

New-Item -Path ".\" -Name "$folder" -ItemType "Directory"

scp lvuser@${ip}:/home/lvuser/logs/*.wpilog .\$folder\
ssh lvuser@${ip} "rm -rf /home/lvuser/logs/*"

git add .
git commit -m "Added logs for $date"
git push
