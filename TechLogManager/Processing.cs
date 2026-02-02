using System;
using System.IO;
using System.Threading.Tasks;

namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessLimelightLogs(string teamNumber, int action)
    {
        Log("Processing Limelight logs...");

        try
        {
            // Get list of limelights from RoboRIO
            var limelightsJson = await SshCommand(
                $"roboRIO-{teamNumber}-FRC.local",
                "cat /home/lvuser/limelights.json"
            );

            var limelights = Utils.ParseJsonStringList(limelightsJson);
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights) await ProcessSingleLimelight(teamNumber, action, llname);
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessSingleLimelight(string teamNumber, int action, string llname)
    {
        Log($"Processing Limelight: {llname}");

        if (action < 2) // Download
        {
            var links = await Limelight.GetRecordingLinksAsync(llname);
            Log($"Found {links.Count} recording(s)");

            var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
            var baseFolder = $"{date}@{teamNumber}";

            for (var i = 0; i < links.Count; i++)
            {
                var folderName = Path.Combine(baseFolder, $"rec{i + 1}");
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

    private async Task ProcessRoborioLogs(string teamNumber, int action)
    {
        Log("Processing RoboRIO logs...");

        await Task.CompletedTask;
    }

    private async Task ProcessDsLogs(string teamNumber, int action)
    {
        Log("Processing driver station logs...");
    }

    private async Task ProcessHootLogs(string teamNumber, int action)
    {
        Log("Processing ctre (hoot) logs...");
    }
}
