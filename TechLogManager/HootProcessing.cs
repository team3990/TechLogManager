namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessHootLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing ctre (hoot) logs...");

        using var connection = new ClientManager(teamNumber);
        
        try
        {
            await connection.ConnectAsync();
            Log("Connected to RoboRIO");
        }
        catch (Exception ex)
        {
            Log($"Failed to connect to RoboRIO: {ex.Message}");
            return;
        }

        string? f1;
        string? f2;
        
        try
        {
            var results = await connection.RunCommandsAsync(
                "find /home/lvuser/logs -name '*.hoot' 2>/dev/null || true",
                "find /U/logs -name '*.hoot' 2>/dev/null || true"
            );
            f1 = results[0];
            f2 = results[1];
        }
        catch (Exception ex)
        {
            Log($"Error finding hoot files: {ex.Message}");
            return;
        }

        if (f1.IsWhiteSpace()) f1 = null;
        if (f2.IsWhiteSpace()) f2 = null;
        if (f1 == null && f2 == null)
        {
            Log("No hoot log files found");
            return;
        }

        var files = f1 == null
            ? f2!.Split("\n").ToList() // f1 is null
            : f2 == null
                ? f1.Split("\n").ToList() // f2 is null
                : f1.Split("\n").Concat(f2.Split("\n")).ToList(); // none is null
        
        files = files.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        
        if (files.Count == 0)
        {
            Log("No valid hoot log files found");
            return;
        }

        files.Sort((a, b) =>
            string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

        Log($"Found {files.Count} hoot log file(s)");

        if (action.IsDownload())
        {
            if (all)
            {
                // Download all files using the same connection
                Log($"Downloading {files.Count} file(s)...");
                var downloadTasks = files.Select(file => 
                    (remotePath: file, localPath: Path.Combine(destination, Path.GetFileName(file)))
                ).ToList();

                var results = await connection.DownloadFilesAsync(downloadTasks);
                foreach (var result in results)
                {
                    Log(result);
                }
            }
            else
            {
                // Download only the latest file
                var file = files[^1];
                Log($"Downloading latest file: {Path.GetFileName(file)}");
                var result = await connection.DownloadFileAsync(file, Path.Combine(destination, Path.GetFileName(file)));
                Log(result);
            }
        }

        if (action.IsDelete())
        {
            if (all)
            {
                Log("Deleting all hoot log files...");
                await connection.RunCommandsAsync(
                    GetHootDeleteScript("/home/lvuser/logs"),
                    GetHootDeleteScript("/U/logs")
                );
                Log("All hoot log files deleted");
            }
            else
            {
                Log($"Deleting latest file: {Path.GetFileName(files[^1])}");
                await connection.RunCommandAsync($"rm -rf {Path.GetDirectoryName(files[^1])}");
                Log("Latest hoot log file deleted");
            }
        }
    }

    private static string GetHootDeleteScript(string dir)
    {
        return $"cd {dir} && find . -depth -type d | while read -r dir; do [ \"$dir\" = \".\" ] && continue; if [ -z \"$(ls -A \"$dir\")\" ]; then rmdir \"$dir\"; continue; fi; total_files=$(find \"$dir\" -maxdepth 1 -type f | wc -l); hoot_files=$(find \"$dir\" -maxdepth 1 -type f -name \"*.hoot\" | wc -l); if [ \"$total_files\" -gt 0 ] && [ \"$total_files\" -eq \"$hoot_files\" ]; then rm -rf \"$dir\"; elif [ \"$hoot_files\" -gt 0 ]; then find \"$dir\" -maxdepth 1 -type f -name \"*.hoot\" -delete; fi; done 2>/dev/null || true";
    }
}
