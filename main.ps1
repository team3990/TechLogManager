# Imports
Add-Type -Path ".\TechLogManager\TechLogManager.dll"
. .\scripts\ctre.ps1
. .\scripts\limelight.ps1
. .\scripts\wpilog.ps1

Write-Output "Tech log processor"

git pull > $null

$teamNum = [TechLogManager.Gui]::AskTeamNumber()
Write-Output "Team number $teamNum"
if (([int]$teamNum) -lt 0 -or $teamNum.Length -gt 4)
{
    exit
}

$whatActionMain = [TechLogManager.Gui]::AskActionWhatLogFormat()
if ($whatActionMain.Count -eq 0)
{
    exit
}

for ($i = 0; $i -lt $whatActionMain.Count; $i++)
{
    switch ($whatActionMain[$i])
    {
        0
        {
            Write-Output "Choosed wpilog"
            $whatAction = [TechLogManager.Gui]::AskActionDlDel("Wpilog")
            if ($whatAction -lt 0)
            {
                exit
            }
            WpilogMain $teamNum $whatAction
        } 1
        {
            Write-Output "Choosed limelight"
            $whatAction = [TechLogManager.Gui]::AskActionDlDel("Limelight")
            if ($whatAction -lt 0)
            {
                exit
            }
            LimelightMain $teamNum $whatAction
        } 2
        {
            Write-Output "Choosed ctre"
            $whatAction = [TechLogManager.Gui]::AskActionDlDel("Ctre")
            if ($whatAction -lt 0)
            {
                exit
            }
            CtreMain $teamNum $whatAction
        } 3
        {
            Write-Output "Choosed dslog"
            $whatAction = [TechLogManager.Gui]::AskActionDlDel("Driver station logs")
            if ($whatAction -lt 0)
            {
                exit
            }
            DsMain $whatAction
        } default
        {
            exit
        }
    }
}

if ([TechlogManager.Gui]::AskCommit())
{
    Write-Output "Committing"
    git add .
    $commitDate = Get-Date -Format "yyyy-MM-dd HH:mm"
    git commit -m "Added logs for $commitDate"
} else
{
    exit
}

if ([TechlogManager.Gui]::AskPush())
{
    Write-Output "Pushing"
    git push
}
