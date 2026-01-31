# Same as mkdir -p folderName
function New-Directory-Protected
{
    param($folderName)

    if (-not (Test-Path $folderName))
    {
        New-Item -Path $folderName -ItemType Directory | Out-Null
    }
}
