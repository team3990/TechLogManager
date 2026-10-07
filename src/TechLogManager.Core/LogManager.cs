namespace TechLogManager;

/// <summary>Lists, downloads and deletes every kind of log. This is the entry point used by the GUI and the CLI.</summary>
public sealed class LogManager(string host, Action<string>? log = null) : IDisposable
{
    private readonly Limelight _limelight = new();
    private readonly Action<string> _log = log ?? (_ => { });
    private readonly RobotConnection _robot = new(host);

    public string Host => _robot.Host;

    public void Dispose()
    {
        _robot.Dispose();
        _limelight.Dispose();
    }

    /// <summary>Connects to the robot. Required before anything else (the Limelight list is also read from the robot).</summary>
    public Task ConnectAsync(CancellationToken ct = default)
    {
        return _robot.ConnectAsync(ct);
    }

    /// <summary>
    /// Lists the logs of the given sources, newest first. A source that fails is reported in
    /// <see cref="ListResult.Errors"/> and does not prevent the others from being listed.
    /// </summary>
    public async Task<ListResult> ListAsync(IEnumerable<LogSource> sources, CancellationToken ct = default)
    {
        var entries = new List<LogEntry>();
        var errors = new List<string>();

        foreach (var source in sources.Distinct())
            try
            {
                _log($"Listing {source} logs...");
                entries.AddRange(source == LogSource.Limelight
                    ? await _limelight.ListAsync(_robot, _log, ct)
                    : await RobotLogs.ListAsync(_robot, source, _log, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var error = $"Error listing {source} logs: {ex.Message}";
                _log(error);
                errors.Add(error);
            }

        // Stable sort: entries without a date keep their relative order, after the dated ones
        var sorted = entries
            .OrderBy(e => e.Date == null)
            .ThenByDescending(e => e.Date)
            .ToList();
        return new ListResult(sorted, errors);
    }

    /// <summary>Downloads a log under <paramref name="dest"/> and returns the local file (or folder, for Limelight).</summary>
    public async Task<string> DownloadAsync(LogEntry entry, string dest, IProgress<long>? bytesDownloaded = null,
        CancellationToken ct = default)
    {
        _log($"Downloading {entry.Name}...");
        var path = entry.Source == LogSource.Limelight
            ? await _limelight.DownloadAsync(entry, dest, bytesDownloaded, ct)
            : await RobotLogs.DownloadAsync(_robot, entry, dest, bytesDownloaded, ct);
        _log($"Downloaded {entry.Name} to {path}");
        return path;
    }

    public async Task DeleteAsync(LogEntry entry, CancellationToken ct = default)
    {
        _log($"Deleting {entry.Name}...");
        if (entry.Source == LogSource.Limelight)
            await _limelight.DeleteAsync(entry, ct);
        else
            await RobotLogs.DeleteAsync(_robot, entry, ct);
        _log($"Deleted {entry.Name}");
    }
}

public sealed record ListResult(List<LogEntry> Entries, List<string> Errors);
