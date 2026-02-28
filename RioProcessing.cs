using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public static class RioProcessing
{
    public static async Task<List<LogEntry>> GetLogs(ClientManager conn)
    {
        string? f1;
        string? f2;

        try
        {
            var results = await conn.RunCommandsAsync(
                "find /home/lvuser/logs -name '*.wpilog' 2>/dev/null || true",
                "find /U/logs -name '*.wpilog' 2>/dev/null || true"
            );
            f1 = results[0];
            f2 = results[1];
        }
        catch (Exception ex)
        {
            Log($"Error finding log files: {ex.Message}");
            return [];
        }

        if (f1.IsWhiteSpace()) f1 = null;
        if (f2.IsWhiteSpace()) f2 = null;
        if (f1 == null && f2 == null)
        {
            Log("No RoboRIO log files found");
            return [];
        }

        var files = f1 == null
            ? f2!.Split("\n").ToList() // f1 is null
            : f2 == null
                ? f1.Split("\n").ToList() // f2 is null
                : f1.Split("\n").Concat(f2.Split("\n")).ToList(); // none is null

        files = files.Where(f => !string.IsNullOrWhiteSpace(f) && !f.StartsWith("FRC_TBD")).ToList();

        if (files.Count == 0)
        {
            Log("No valid RoboRIO log files found");
            return [];
        }

        files.Sort((a, b) =>
            string.Compare(File.GetName(a), File.GetName(b), StringComparison.OrdinalIgnoreCase));

        Log($"Found {files.Count} RoboRIO log file(s)");
        
        return files.Select(file => new LogEntry(file.GetFileName()!, LogSource.RoboRio, async (dest, action) =>
            {
                if (action.IsDownload())
                {
                    var result = await conn.DownloadFileScpAsync(file, dest
                        .Combine("wpilog").CreateDirectory().Combine(file.GetFileName()!));
                    Log(result);
                }

                if (action.IsDelete())
                {
                    var result = await conn.RunCommandAsync($"rm -f {file}");
                    Log(result);
                }
            }))
            .OrderByDescending(e => e.Name).ToList();
    }
}
