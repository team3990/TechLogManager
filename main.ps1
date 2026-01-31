# Imports
Add-Type -Path ".\TechLogManager\TechLogManager.dll"
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

$whatActionMain = [TechLogManager.Gui]::AskActionLlWpi()
if ($whatActionMain -lt 0)
{
    exit
}

switch ($whatActionMain)
{
    0
    {
        Write-Output "Choosed both"
        $whatActionLimelight = [TechLogManager.Gui]::AskActionDlDel("Limelight")
        if ($whatActionLimelight -lt 0)
        {
            exit
        }
        $whatActionWpilog = [TechLogManager.Gui]::AskActionDlDel("Wpilog")
        if ($whatActionWpilog -lt 0)
        {
            exit
        }
        WpilogMain $teamNum $whatActionWpilog
        LimelightMain $teamNum $whatActionLimelight
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
        Write-Output "Choosed wpilog"
        $whatAction = [TechLogManager.Gui]::AskActionDlDel("Wpilog")
        if ($whatAction -lt 0)
        {
            exit
        }
        WpilogMain $teamNum $whatAction
    } default
    {
        exit
    }
}

if ([TechlogManager.Gui]::AskCommit())
{
    Write-Output "Committing"
    git add .
    $commitDate = Get-Date -Format "yyyy-MM-dd HH:mm"
    git commit -m "Added logs for $commitDate"
}
else
{
    exit
}

if ([TechlogManager.Gui]::AskPush())
{
    Write-Output "Pushing"
    git push
}
