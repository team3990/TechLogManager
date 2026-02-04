using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessLimelightLogs(string teamNumber, int action, string destination)
    {
        Log("Processing Limelight logs...");

        try
        {
            // Get list of limelights from RoboRIO
            var limelightsJson = await SshCommand(teamNumber, "cat /home/lvuser/limelights.json");

            var limelights = Utils.ParseJsonStringList(limelightsJson);
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights) await ProcessSingleLimelight(action, llname, Path.Combine(destination, llname));
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessSingleLimelight(int action, string llname, string destination)
    {
        Log($"Processing Limelight: {llname}");

        if (action < 2) // Download
        {
            var links = await Limelight.GetRecordingLinksAsync(llname);
            Log($"Found {links.Count} recording(s)");

            for (var i = 0; i < links.Count; i++)
            {
                var folderName = Path.Combine(destination, $"rec{i + 1}");
                Directory.CreateDirectory(folderName);

                var recording = links[i];

                // Download video
                if (!string.IsNullOrEmpty(recording.video))
                {
                    Log($"  Downloading video {i + 1}...");
                    await DownloadFile(recording.video, Path.Combine(folderName, "video.avi"));
                }

                // Download manifest
                if (!string.IsNullOrEmpty(recording.manifest))
                {
                    Log($"  Downloading manifest {i + 1}...");
                    await DownloadFile(recording.manifest, Path.Combine(folderName, "manifest.jsonl"));
                }

                // Download bootlog
                if (!string.IsNullOrEmpty(recording.bootlog))
                {
                    Log($"  Downloading bootlog {i + 1}...");
                    await DownloadFile(recording.bootlog, Path.Combine(folderName, "bootlog.txt.gz"));
                }

                Log($"  Downloaded to {folderName}");
            }
        }

        if (action is 0 or 2) // Delete
        {
            Log($"Deleting videos from {llname}...");
            await Limelight.DeleteAllVideosAsync(llname);
            Log($"Videos deleted from {llname}");
        }
    }

    
    [GeneratedRegex(@"^(.+\.wpilog)$")]
    private static partial Regex WpilogRegex();
    private async Task ProcessRoborioLogs(string teamNumber, int action, string destination)
    {
        Log("Processing RoboRIO logs...");

        var f1 = await SshCommand(teamNumber, "find /home/lvuser/logs -name '*.wpilog");
        var f2 = await SshCommand(teamNumber, "find /U/logs -name '*.wpilog'");
        if (f1.IsWhiteSpace()) return;
        if (f2.IsWhiteSpace()) return;
        var files = f1.Split("\n").Concat(f2.Split("\n")).ToList();

        var matches = files.Where(s => WpilogRegex().IsMatch(s))
            .Select(s => WpilogRegex().Match(s))
            .ToArray();

        foreach (var file in matches)
        {
            var fileName = Path.GetFileName(file.Groups[1].Value);
            Log($"Processing file {fileName}");
            var result = await ScpTransfer(teamNumber, file.Groups[1].Value, destination);
            Log(result);
        }

        if (action is 0 or 2) // Delete
        {
            var result1 = await SshCommand(teamNumber, "rm -f /home/lvuser/logs/*.wpilog");
            Log(result1);
            var result2 = await SshCommand(teamNumber, "rm -f /U/logs/*.wpilog");
            Log(result2);
        }
    }

    private async Task ProcessDsLogs(string teamNumber, int action, string destination)
    {
        Log("Processing driver station logs...");

        await Task.CompletedTask;
    }

    private async Task ProcessHootLogs(string teamNumber, int action, string destination)
    {
        Log("Processing ctre (hoot) logs...");

        await Task.CompletedTask;
    }
}
