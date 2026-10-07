# TechLogManager

Lists, downloads and deletes the logs of a 2027 FRC robot (Systemcore):

| Type      | What                                   | Where                                                         |
|-----------|----------------------------------------|---------------------------------------------------------------|
| wpilog    | WPILib data logs (`WPILIB_*.wpilog`)   | `/home/systemcore/logs` and `/U/logs` (USB stick) on the robot |
| hoot      | CTRE logs (`*.hoot`)                   | same folders, one subfolder per session                       |
| limelight | Limelight recordings (video, manifest) | each Limelight listed in `/home/systemcore/limelights.json`   |

There is a desktop app (`TechLogManager`) and a command line version (`tlm`).

## Download

Run the **Build** workflow in the [Actions tab](https://github.com/team3990/TechLogManager/actions/workflows/build.yml)
and download the GUI or CLI artifact for your OS.

## Desktop app

1. Open the settings and set the logs repo location (where downloads go) and, if needed, the robot address
   (default `robot.local`, `172.26.0.1` over USB on Windows).
2. Pick the log types and click **Discover Logs**.
3. Download or delete logs, one by one, the selected ones, or all the visible ones. Downloads go to
   `{repo location}/{folder name}`.

## Command line

```shell
tlm list                                  # every log, newest first
tlm list --source wpilog,hoot --json      # only some types, as JSON
tlm list --matches                        # only logs recorded during FMS matches
tlm download --latest 3 --source wpilog   # the 3 newest wpilogs
tlm download WPILIB_20261005_184314.wpilog --dest ./logs
tlm delete --all --source hoot            # prints what would be deleted
tlm delete --all --source hoot --yes      # actually deletes
```

Common options: `--host` (robot address), `--json`. Run `tlm --help` or `tlm <command> --help` for everything.

`tlm` uses the same settings as the desktop app: without `--dest`, downloads go to
`{repo location}/{yyyy-MM-dd-HH'h'mm}`.

Exit codes: `0` success, `1` something failed (connection, a log type could not be listed, a download/delete failed),
`2` invalid usage (nothing selected, unknown log name, `delete` without `--yes`).

## Downloaded files

```
{dest}/wpilog/WPILIB_20261005_184314.wpilog
{dest}/hoot/2026-10-06_19-08-23.hoot
{dest}/limelight/{limelight}/{recording}/video.avi, manifest.jsonl, bootlog.txt.gz
```

## Building

Requires the .NET 10 SDK.

```shell
dotnet build TechLogManager.slnx
dotnet test TechLogManager.slnx
dotnet run --project src/TechLogManager       # desktop app
dotnet run --project src/TechLogManager.Cli -- list
```
