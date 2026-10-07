using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TechLogManager;

/// <summary>Recordings stored on the Limelights. The robot code lists the Limelight names in <see cref="LimelightListPath"/>.</summary>
public sealed class Limelight : IDisposable
{
    /// <summary>JSON array of Limelight host names (without ".local"), written by the robot code.</summary>
    public const string LimelightListPath = "/home/systemcore/limelights.json";

    private const int Port = 5807;

    private readonly HttpClient _httpClient = new();

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static string BaseUrl(string limelight)
    {
        return $"http://{limelight}.local:{Port}";
    }

    public async Task<List<LogEntry>> ListAsync(RobotConnection conn, Action<string> log, CancellationToken ct = default)
    {
        var json = await conn.TryReadAllTextAsync(LimelightListPath, ct)
                   ?? throw new FileNotFoundException($"{LimelightListPath} not found on the robot");
        var limelights = JsonSerializer.Deserialize<List<string>>(json)
                         ?? throw new FormatException($"{LimelightListPath} is not a list of names");
        log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

        var entries = new List<LogEntry>();
        foreach (var limelight in limelights)
            try
            {
                var videos = await _httpClient.GetFromJsonAsync<List<VideoInfo>>($"{BaseUrl(limelight)}/videolist", ct)
                             ?? [];
                log($"Found {videos.Count} recording(s) on {limelight}");
                entries.AddRange(videos.Select(video => new LogEntry(LogSource.Limelight,
                    $"{limelight}_{video.name}", video.name, null, video.size, Device: limelight)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log($"Error listing recordings on {limelight}: {ex.Message}");
            }

        return entries;
    }

    /// <summary>
    /// Downloads to "{dest}/limelight/{limelight}/{recording}/" (video.avi, manifest.jsonl, bootlog.txt.gz) and
    /// returns that folder. The manifest and boot log are skipped if the Limelight does not have them.
    /// </summary>
    public async Task<string> DownloadAsync(LogEntry entry, string dest, IProgress<long>? bytesDownloaded = null,
        CancellationToken ct = default)
    {
        var directory = Directory.CreateDirectory(Path.Combine(dest, RobotLogs.SubfolderName(LogSource.Limelight),
            entry.Device!, entry.RemotePath)).FullName;
        var recordingUrl = $"{BaseUrl(entry.Device!)}/recording/";

        await DownloadFileAsync($"{recordingUrl}{entry.RemotePath}.avi", Path.Combine(directory, "video.avi"),
            true, bytesDownloaded, ct);
        await DownloadFileAsync($"{recordingUrl}{entry.RemotePath}_manifest.jsonl",
            Path.Combine(directory, "manifest.jsonl"), false, null, ct);
        await DownloadFileAsync($"{recordingUrl}{entry.RemotePath}_bootlog.txt.gz",
            Path.Combine(directory, "bootlog.txt.gz"), false, null, ct);

        return directory;
    }

    private async Task DownloadFileAsync(string url, string localPath, bool required, IProgress<long>? bytesDownloaded,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!required && response.StatusCode == HttpStatusCode.NotFound) return;
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(localPath);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            total += read;
            bytesDownloaded?.Report(total);
        }
    }

    public async Task DeleteAsync(LogEntry entry, CancellationToken ct = default)
    {
        // The Limelight expects the video file name (recording name + ".avi")
        var name = Uri.EscapeDataString($"{entry.RemotePath}.avi");
        using var response = await _httpClient.DeleteAsync($"{BaseUrl(entry.Device!)}/delete-video?name={name}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ReSharper disable InconsistentNaming (matches the Limelight JSON)
    private sealed class VideoInfo
    {
        public string name { get; set; } = "";
        public long size { get; set; }
    }
}
