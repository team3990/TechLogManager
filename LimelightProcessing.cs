using LimelightClasses;
using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public static class LimelightProcessing
{
    public static async Task<List<LogEntry>> GetLogs(ClientManager conn)
    {
        Log("Processing Limelight logs...");

        var entries = new List<LogEntry>();
        try
        {
            var limelightsJson = await conn.RunCommandAsync("cat /home/lvuser/limelights.json");

            var limelights = ParseJsonStringList(limelightsJson) ??
                             throw new FileNotFoundException("Failed to get limelight list");
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights)
                entries.AddRange(await ProcessSingleLimelight(llname, conn));
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }

        return entries;
    }

    private static async Task<List<LogEntry>> ProcessSingleLimelight(string llname, ClientManager conn)
    {
        Log($"Processing {llname}");

        var recs = await LimelightUtils.GetRecordingsAsync(llname);

        return recs.Select(rec =>
        {
            rec.GetLinks(); // Convert file names to full URLs
            return new LogEntry(rec.Name, LogSource.Limelight, async (dest, action) =>
            {
                if (action.IsDownload())
                {
                    if (!string.IsNullOrEmpty(rec.Video))
                        await conn.DownloadFileHttpAsync(rec.Video, dest
                            .Combine(llname).CreateDirectory().Combine("video.avi"));
                    if (!string.IsNullOrEmpty(rec.Manifest))
                        await conn.DownloadFileHttpAsync(rec.Manifest, dest
                            .Combine(llname).CreateDirectory().Combine("manifest.jsonl"));
                    if (!string.IsNullOrEmpty(rec.Bootlog))
                        await conn.DownloadFileHttpAsync(rec.Bootlog, dest
                            .Combine(llname).CreateDirectory().Combine("bootlog.txt.gz"));
                }

                if (action.IsDelete())
                    await rec.Delete();
            });
        }).ToList().Reversed();
    }

    public static async Task DownloadAll(string dest, ClientManager conn)
    {
        var limelightsJson = await conn.RunCommandAsync("cat /home/lvuser/limelights.json");
        var limelights = ParseJsonStringList(limelightsJson);
        if (limelights == null) return;

        foreach (var llname in limelights)
        foreach (RecordingDetail rec in await LimelightUtils.GetRecordingsAsync(llname))
        {
            if (!string.IsNullOrEmpty(rec.Video))
                await conn.DownloadFileHttpAsync(rec.Video, dest
                    .Combine(llname + "-" + rec.Name).CreateDirectory().Combine("video.avi"));
            if (!string.IsNullOrEmpty(rec.Manifest))
                await conn.DownloadFileHttpAsync(rec.Manifest, dest
                    .Combine(llname + "-" + rec.Name).CreateDirectory().Combine("manifest.jsonl"));
            if (!string.IsNullOrEmpty(rec.Bootlog))
                await conn.DownloadFileHttpAsync(rec.Bootlog, dest
                    .Combine(llname + "-" + rec.Name).CreateDirectory().Combine("bootlog.txt.gz"));
        }
    }

    public static async Task DeleteAll(ClientManager conn)
    {
        var limelightsJson = await conn.RunCommandAsync("cat /home/lvuser/limelights.json");
        var limelights = ParseJsonStringList(limelightsJson);
        if (limelights == null) return;

        foreach (var limelight in limelights)
            await LimelightUtils.DeleteAllVideosAsync(limelight);
    }
}
