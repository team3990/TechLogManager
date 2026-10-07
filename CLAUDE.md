# TechLogManager

Tool for FRC team 3990 to list, download and delete robot logs from a **2027 Systemcore** robot (no roboRIO support).
Desktop app (Avalonia) + CLI (`tlm`), both thin layers over a shared core library. .NET 10, C# 14.

## Layout

```
TechLogManager.slnx
src/TechLogManager.Core/      all the logic, no UI dependency
  LogManager.cs               facade used by the GUI and the CLI: Connect, List, Download, Delete
  RobotConnection.cs          SFTP (SSH.NET) to the Systemcore, user/password systemcore/systemcore
  RobotLogs.cs                .wpilog and .hoot files in /home/systemcore/logs and /U/logs
  Limelight.cs                Limelight recordings over HTTP (port 5807)
  LogFileNames.cs             dates / match info parsed from file names
  LogEntry.cs                 LogEntry record, LogSource / MatchType enums, MatchInfo
  SettingsManager.cs          %AppData%/TechLogManager/settings.json (RobotHost, RepositoryLocation), shared by GUI and CLI
src/TechLogManager/           Avalonia GUI (MainWindow, SettingsWindow); Utils.cs has GUI-only helpers
src/TechLogManager.Cli/       System.CommandLine CLI, assembly name "tlm"
tests/TechLogManager.Core.Tests/  xUnit tests (file name parsing)
logsSample/                   real sample logs, NOT committed (gitignored); used by a test if present
```

## Commands

```shell
dotnet build TechLogManager.slnx
dotnet test TechLogManager.slnx
dotnet run --project src/TechLogManager.Cli -- list --json
```

## Log sources and conventions

- **wpilog**: WPILib `DataLogManager`, names are `WPILIB_yyyyMMdd_HHmmss.wpilog`, or
  `WPILIB_yyyyMMdd_HHmmss_{event}_{P|Q|E}{match}.wpilog` when the FMS is connected. The date is **UTC**.
  `WPILIB_TBD_*.wpilog` files (DS not connected yet / still recording) are skipped.
- **hoot**: CTRE, `{session start}/yyyy-MM-dd_HH-mm-ss.hoot` (one subfolder per session). The date is the robot's
  **local time**; it is parsed with this computer's time zone. The file header also has the UTC Unix time at
  offset 0x48 (undocumented, used only by a test to check the parsing).
- **limelight**: the robot code writes the Limelight names to `/home/systemcore/limelights.json`
  (JSON array of host names without `.local`). Endpoints on `http://{name}.local:5807`: `GET /videolist`,
  `GET /recording/{name}.avi` (+ `_manifest.jsonl`, `_bootlog.txt.gz`), `DELETE /delete-video?name={name}.avi`.
  These are undocumented: the delete endpoint has not been tested on a real Limelight.
- Dates in `LogEntry` are UTC `DateTimeOffset`s (file modification time if the name has no date); displayed in local time.
- If a log type fails to list, the others are still listed (`ListResult.Errors`).
- Downloads go to `{dest}/wpilog/`, `{dest}/hoot/`, `{dest}/limelight/{limelight}/{recording}/`.
  SFTP downloads are written to `*.part` then renamed.
- Deleting the last hoot file of a session also deletes its (empty) session folder.

## Using the CLI to get logs (for Claude)

stdout only has the result (use `--json`), progress and errors go to stderr.
Exit codes: 0 ok, 1 failure, 2 invalid usage (nothing selected, unknown name, `delete` without `--yes`).

```shell
tlm list --json                                # what is on the robot, newest first
tlm list --matches --source wpilog --json      # FMS match logs only
tlm download --latest 2 --source wpilog --dest <dir> --json   # prints the local paths
tlm download <name> <name> --dest <dir>
```

- Default host is `robot.local` (or the GUI setting); `--host` accepts `host` or `host:port`.
  Over USB the Systemcore is `172.26.0.1` (Windows) / `172.27.0.1` (Linux/macOS).
- The computer must be on the robot's network; if `tlm` cannot connect, say so instead of retrying.
- **Never run `tlm delete ... --yes` unless the user explicitly asked to delete those logs.**
- Analysing the content of .wpilog files (crop, merge, ...) is done with the separate wpilogUtils Python repo
  (`../wpilogUtils`), not here.

## Testing without a robot

Any SFTP server works if it exposes `/home/systemcore/logs` and accepts systemcore/systemcore. For example the
`sftpserver` Python package (`pip install sftpserver`), run from a folder containing `home/systemcore/logs/...`:
`sftpserver --host 127.0.0.1 --port 3373 --keyfile <rsa key>`, then `tlm --host 127.0.0.1:3373 list`.

## Code style

- File-scoped namespace `TechLogManager` everywhere (CLI: `TechLogManager.Cli`), nullable enabled, implicit usings.
- Core reports progress through the `Action<string> log` given to `LogManager` (GUI: console, CLI: stderr), never
  writes to stdout.
- Keep UI code out of Core; the GUI and CLI must stay thin.
