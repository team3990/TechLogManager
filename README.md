# logs

This is the log processor. It will take the logs in the roborio, download them to your pc, delete them from the roborio and upload them to this repo.

Running steps :

1. Run this : `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned`
2. Ensure you have powershell 7 on your pc. If not, run `winget install microsoft.powershell`
3. Run the script : `.\processLogs.ps1`

To run the script, enter `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned` and click it and it will (hopefully) work

Supported logs :

- [x] wpilog
- [ ] hoot
- [ ] limelight rewind
