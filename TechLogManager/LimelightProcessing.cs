namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessLimelightLogs(string teamNumber, Action action, string destination)
    {
        Log("Processing Limelight logs...");

        try
        {
            // Get list of limelights from RoboRIO
            var limelightsJson = await SshCommand(teamNumber, "cat /home/lvuser/limelights.json");

            var limelights = Utils.ParseJsonStringList(limelightsJson) ?? throw new NullReferenceException("Failed to get limelight list");
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights) await ProcessSingleLimelight(action, llname, Path.Combine(destination, llname));
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessSingleLimelight(Action action, string llname, string destination)
    {
        Log($"Processing Limelight: {llname}");

        if (action.IsDownload())
        {
            var links = await LimelightUtils.GetRecordingLinksAsync(llname);
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

        if (action.IsDelete())
        {
            Log($"Deleting videos from {llname}...");
            await LimelightUtils.DeleteAllVideosAsync(llname);
            Log($"Videos deleted from {llname}");
        }
    }
}
