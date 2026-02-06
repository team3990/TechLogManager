namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessHootLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing ctre (hoot) logs...");
        
        string? f1 = null;
        string? f2 = null;
        try
        {
            f1 = await SshCommand(teamNumber, "find /home/lvuser/logs -name '*.hoot'");
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            f2 = await SshCommand(teamNumber, "find /U/logs -name '*.hoot'");
        }
        catch (Exception)
        {
            // ignored
        }

        if (f1.IsWhiteSpace()) f1 = null;
        if (f2.IsWhiteSpace()) f2 = null;
        if (f1 == null && f2 == null) return;

        var files = f1 == null
            ? f2!.Split("\n").ToList() // f1 is null
            : f2 == null
                ? f1.Split("\n").ToList() // f2 is null
                : f1.Split("\n").Concat(f2.Split("\n")).ToList(); // none is null
        if (files.Count == 0) return;
        files.Sort((a, b) =>
            string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

        if (action.IsDownload())
        {
            if (all) foreach (var file in files)
            {
                if (file.IsWhiteSpace()) continue;
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
                var result = await SshCommand(teamNumber, GetHootDeleteScript("/home/lvuser/logs"));
                Log(result);
                result = await SshCommand(teamNumber, GetHootDeleteScript("/U/logs"));
                Log(result);
            }
            else
            {
                var result = await SshCommand(teamNumber, $"rm -rf {Path.GetDirectoryName(files[^1])}");
                Log(result);
            }
    }

    private static string GetHootDeleteScript(string dir)
    {
        return $"'cd {dir} && find . -depth -type d | while read -r dir; do [ \"$dir\" = \".\" ] && continue; if [ -z \"$(ls -A \"$dir\")\" ]; then rmdir \"$dir\"; continue; fi; total_files=$(find \"$dir\" -maxdepth 1 -type f | wc -l); hoot_files=$(find \"$dir\" -maxdepth 1 -type f -name \"*.hoot\" | wc -l); if [ \"$total_files\" -gt 0 ] && [ \"$total_files\" -eq \"$hoot_files\" ]; then rm -rf \"$dir\"; elif [ \"$hoot_files\" -gt 0 ]; then find \"$dir\" -maxdepth 1 -type f -name \"*.hoot\" -delete; fi; done'";
    }
}
