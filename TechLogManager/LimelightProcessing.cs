namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessLimelightLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing Limelight logs...");

        // Use a single connection for getting the limelight list
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

        try
        {
            var limelightsJson = await connection.RunCommandAsync("cat /home/lvuser/limelights.json");

            var limelights = Utils.ParseJsonStringList(limelightsJson) ??
                             throw new FileNotFoundException("Failed to get limelight list");
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights)
                await ProcessSingleLimelight(action, all, llname, Path.Combine(destination, llname));
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessSingleLimelight(Action action, bool all, string llname, string destination)
    {
        Log($"Processing Limelight: {llname}");
        var recs = await LimelightUtils.GetRecordingsAsync(llname);
        Log($"Found {recs.Count} recording(s)");

        if (action.IsDownload())
        {
            if (all)
            {
                for (var i = 0; i < recs.Count; i++)
                {
                    var folderName = Path.Combine(destination, $"rec{i + 1}");
                    Directory.CreateDirectory(folderName);

                    var links = recs[i].GetLinks();

                    // Download video
                    if (!string.IsNullOrEmpty(links.video))
                    {
                        Log($"  Downloading video {i + 1}...");
                        await RemoteOperations.DownloadFileAsync(links.video, Path.Combine(folderName, "video.avi"));
                    }

                    // Download manifest
                    if (!string.IsNullOrEmpty(links.manifest))
                    {
                        Log($"  Downloading manifest {i + 1}...");
                        await RemoteOperations.DownloadFileAsync(links.manifest, Path.Combine(folderName, "manifest.jsonl"));
                    }

                    // Download bootlog
                    if (!string.IsNullOrEmpty(links.bootlog))
                    {
                        Log($"  Downloading bootlog {i + 1}...");
                        await RemoteOperations.DownloadFileAsync(links.bootlog, Path.Combine(folderName, "bootlog.txt.gz"));
                    }

                    Log($"  Downloaded to {folderName}");
                }
            }
            else
            {
                var folderName = Path.Combine(destination, "rec");
                Directory.CreateDirectory(folderName);

                var links = recs[^1].GetLinks();

                // Download video
                if (!string.IsNullOrEmpty(links.video))
                {
                    Log("  Downloading video...");
                    await RemoteOperations.DownloadFileAsync(links.video, Path.Combine(folderName, "video.avi"));
                }

                // Download manifest
                if (!string.IsNullOrEmpty(links.manifest))
                {
                    Log("  Downloading manifest...");
                    await RemoteOperations.DownloadFileAsync(links.manifest, Path.Combine(folderName, "manifest.jsonl"));
                }

                // Download bootlog
                if (!string.IsNullOrEmpty(links.bootlog))
                {
                    Log("  Downloading bootlog...");
                    await RemoteOperations.DownloadFileAsync(links.bootlog, Path.Combine(folderName, "bootlog.txt.gz"));
                }

                Log($"  Downloaded to {folderName}");
            }
        }

        if (action.IsDelete())
        {
            if (all)
            {
                Log($"Deleting videos from {llname}...");
                await LimelightUtils.DeleteAllVideosAsync(llname);
                Log($"Videos deleted from {llname}");
            }
            else
            {
                Log($"Deleting latest video from {llname}...");
                await recs[^1].Delete();
            }
        }
    }
}
