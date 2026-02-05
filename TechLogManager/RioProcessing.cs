namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessRoborioLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing RoboRIO logs...");

        var f1 = await SshCommand(teamNumber, "find /home/lvuser/logs -name '*.wpilog'");
        var f2 = await SshCommand(teamNumber, "find /U/logs -name '*.wpilog'");
        if (f1.IsWhiteSpace() && f2.IsWhiteSpace()) return;

        var files = f1.Split("\n").Concat(f2.Split("\n")).ToList();
        files.Sort((a, b) =>
            string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

        files = (from file in files
                where !file.StartsWith("FRC_TBD")
                where file.EndsWith(".wpilog")
                select file)
            .ToList();

        if (action.IsDownload())
        {
            if (all)
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    Log($"Processing file {fileName}");
                    var result = await ScpTransfer(teamNumber, file, destination);
                    Log(result);
                }
            }
            else
            {
                var file = files[^1];
                Log($"Processing file {file}");
                var result = await ScpTransfer(teamNumber, file, destination);
                Log(result);
            }
        }

        if (action.IsDelete())
            if (all)
            {
                var result = await SshCommand(teamNumber, "rm -f /home/lvuser/logs/*.wpilog");
                Log(result);
                result = await SshCommand(teamNumber, "rm -f /U/logs/*.wpilog");
                Log(result);
            }
            else
            {
                var result = await SshCommand(teamNumber, $"rm -f {files[^1]}");
                Log(result);
            }
    }
}