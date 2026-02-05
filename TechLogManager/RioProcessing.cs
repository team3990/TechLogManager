namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessRoborioLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing RoboRIO logs...");

        string? f1 = null;
        string? f2 = null;
        try
        {
            f1 = await SshCommand(teamNumber, "find /home/lvuser/logs -name '*.wpilog'");
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            f2 = await SshCommand(teamNumber, "find /U/logs -name '*.wpilog'");
        }
        catch (Exception)
        {
            // ignored
        }

        if (f1 == null && f2 == null) return;
        if (f1.IsWhiteSpace() && f2.IsWhiteSpace()) return;

        var files = f1 == null
            ? f2!.Split("\n").ToList() // f1 is null
            : f2 == null
                ? f1.Split("\n").ToList() // f2 is null
                : f1.Split("\n").Concat(f2.Split("\n")).ToList(); // none is null
        if (files.Count == 0) return;
        files.Sort((a, b) =>
            string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

        files = (from file in files
                where !file.StartsWith("FRC_TBD")
                where file.EndsWith(".wpilog")
                select file)
            .ToList();

        if (action.IsDownload())
        {
            if (all) foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                Log($"Processing file {fileName}");
                var result = await ScpTransfer(teamNumber, file, Path.Combine(destination, Path.GetFileName(file)));
                Log(result);
            }
            else
            {
                var file = files[^1];
                var result = await ScpTransfer(teamNumber, file, Path.Combine(destination, Path.GetFileName(file)));
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