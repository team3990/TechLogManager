using Renci.SshNet;
using Renci.SshNet.Common;

namespace TechLogManager;

/// <summary>SFTP connection to the Systemcore.</summary>
public sealed class RobotConnection : IDisposable
{
    public const string DefaultHost = "robot.local";
    private const string Username = "systemcore";
    private const string Password = "systemcore";

    private readonly SftpClient _sftpClient;

    /// <param name="host">Host name or IP, optionally followed by ":port" (default port 22).</param>
    public RobotConnection(string host)
    {
        Host = string.IsNullOrWhiteSpace(host) ? DefaultHost : host.Trim();

        var port = 22;
        var hostName = Host;
        var colon = Host.LastIndexOf(':');
        if (colon > 0 && Host.IndexOf(':') == colon && int.TryParse(Host[(colon + 1)..], out var parsedPort))
        {
            hostName = Host[..colon];
            port = parsedPort;
        }

        _sftpClient = new SftpClient(hostName, port, Username, Password);
        _sftpClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
    }

    public string Host { get; }

    public void Dispose()
    {
        _sftpClient.Dispose();
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            await _sftpClient.ConnectAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new Exception($"Could not connect to {Host}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists the regular files in <paramref name="directory"/> and its subdirectories. A missing directory is empty.
    /// Paths are built from <paramref name="directory"/> (not the server's canonical path) so they keep the same prefix.
    /// </summary>
    public async Task<List<RemoteFile>> ListFilesAsync(string directory, int maxDepth = 3, CancellationToken ct = default)
    {
        var files = new List<RemoteFile>();
        try
        {
            await foreach (var file in _sftpClient.ListDirectoryAsync(directory, ct))
            {
                if (file.Name is "." or "..") continue;
                var path = $"{directory.TrimEnd('/')}/{file.Name}";
                if (file.IsRegularFile) files.Add(new RemoteFile(path, file.Name, file.Length, file.LastWriteTimeUtc));
                else if (file.IsDirectory && maxDepth > 0)
                    files.AddRange(await ListFilesAsync(path, maxDepth - 1, ct));
            }
        }
        catch (SftpPathNotFoundException)
        {
            // Directory does not exist (e.g. no USB stick), nothing to list
        }

        return files;
    }

    public async Task<string?> TryReadAllTextAsync(string path, CancellationToken ct = default)
    {
        if (!await _sftpClient.ExistsAsync(path, ct)) return null;
        return await Task.Run(() => _sftpClient.ReadAllText(path), ct);
    }

    /// <summary>
    /// Downloads a file. It is written to "{localPath}.part" first so an interrupted download never looks complete.
    /// </summary>
    public async Task DownloadAsync(string remotePath, string localPath, IProgress<long>? bytesDownloaded = null,
        CancellationToken ct = default)
    {
        var partPath = localPath + ".part";
        try
        {
            await using (var stream = File.Create(partPath))
            {
                var progress = bytesDownloaded == null
                    ? null
                    : new Progress<DownloadFileProgressReport>(r => bytesDownloaded.Report((long)r.TotalBytesDownloaded));
                await _sftpClient.DownloadFileAsync(remotePath, stream, progress, ct);
            }

            File.Move(partPath, localPath, true);
        }
        catch
        {
            File.Delete(partPath);
            throw;
        }
    }

    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        return _sftpClient.DeleteFileAsync(path, ct);
    }

    /// <summary>Deletes <paramref name="directory"/> if it contains nothing.</summary>
    public async Task DeleteDirectoryIfEmptyAsync(string directory, CancellationToken ct = default)
    {
        await foreach (var file in _sftpClient.ListDirectoryAsync(directory, ct))
            if (file.Name is not ("." or ".."))
                return;

        await _sftpClient.DeleteDirectoryAsync(directory, ct);
    }
}

public sealed record RemoteFile(string FullName, string Name, long Length, DateTime LastWriteTimeUtc);
