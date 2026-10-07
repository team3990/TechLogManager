namespace TechLogManager;

/// <summary>WPILib (.wpilog) and CTRE (.hoot) logs stored on the Systemcore.</summary>
public static class RobotLogs
{
    /// <summary>Where logs are written: internal storage, then the USB stick (if any).</summary>
    public static readonly string[] LogDirectories = ["/home/systemcore/logs", "/U/logs"];

    public static async Task<List<LogEntry>> ListAsync(RobotConnection conn, LogSource source, Action<string> log,
        CancellationToken ct = default)
    {
        var extension = source switch
        {
            LogSource.Wpilog => ".wpilog",
            LogSource.Hoot => ".hoot",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Not a robot log source")
        };

        var entries = new List<LogEntry>();
        foreach (var directory in LogDirectories)
        {
            var files = (await conn.ListFilesAsync(directory, ct: ct))
                .Where(f => f.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();
            log($"Found {files.Count} {extension} file(s) in {directory}");

            foreach (var file in files)
            {
                if (source == LogSource.Wpilog && LogFileNames.IsTemporaryWpilog(file.Name))
                {
                    log($"Skipping {file.FullName} (not renamed yet: DS never connected or still recording)");
                    continue;
                }

                entries.Add(CreateEntry(source, file));
            }
        }

        return entries;
    }

    private static LogEntry CreateEntry(LogSource source, RemoteFile file)
    {
        DateTimeOffset? date = null;
        MatchInfo? match = null;

        switch (source)
        {
            case LogSource.Wpilog when LogFileNames.TryParseWpilog(file.Name, out var wpilogDate, out match):
                date = wpilogDate;
                break;
            case LogSource.Hoot when LogFileNames.TryParseHoot(file.Name, out var hootDate):
                date = hootDate.ToUniversalTime();
                break;
        }

        // Fall back to the modification time when the name has no date in it
        date ??= new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);

        return new LogEntry(source, file.Name, file.FullName, date, file.Length, match);
    }

    /// <summary>Downloads to "{dest}/wpilog/{name}" or "{dest}/hoot/{name}" and returns the local path.</summary>
    public static async Task<string> DownloadAsync(RobotConnection conn, LogEntry entry, string dest,
        IProgress<long>? bytesDownloaded = null, CancellationToken ct = default)
    {
        var directory = Directory.CreateDirectory(Path.Combine(dest, SubfolderName(entry.Source))).FullName;
        var localPath = Path.Combine(directory, entry.Name);
        await conn.DownloadAsync(entry.RemotePath, localPath, bytesDownloaded, ct);
        return localPath;
    }

    public static async Task DeleteAsync(RobotConnection conn, LogEntry entry, CancellationToken ct = default)
    {
        await conn.DeleteFileAsync(entry.RemotePath, ct);

        // Hoot logs are in one folder per session: remove the folder once it is empty
        var parent = ParentDirectory(entry.RemotePath);
        if (entry.Source == LogSource.Hoot && parent != null && !LogDirectories.Contains(parent))
            await conn.DeleteDirectoryIfEmptyAsync(parent, ct);
    }

    internal static string SubfolderName(LogSource source)
    {
        return source.ToString().ToLowerInvariant();
    }

    private static string? ParentDirectory(string remotePath)
    {
        var index = remotePath.TrimEnd('/').LastIndexOf('/');
        return index > 0 ? remotePath[..index] : null;
    }
}
