using Renci.SshNet;
using static TechLogManager.Utils;

namespace TechLogManager;

public class ClientManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ScpClient _scpClient;
    private readonly SshClient _sshClient;
    private readonly string _teamNumber;
    private bool _connected;
    private bool _disposed;

    public ClientManager(string teamNumber)
    {
        _teamNumber = teamNumber;
        var hostname = GetRioHostname(teamNumber);

        _sshClient = new SshClient(hostname, "lvuser", "");
        _scpClient = new ScpClient(hostname, "lvuser", "");
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _connected = false;
        if (_disposed) return;

        try
        {
            if (_sshClient.IsConnected)
                _sshClient.Disconnect();

            if (_scpClient.IsConnected)
                _scpClient.Disconnect();

            _httpClient.Dispose();
            _sshClient.Dispose();
            _scpClient.Dispose();
        }
        catch
        {
            // ignored
        }

        _disposed = true;
    }

    public async Task ConnectAsync()
    {
        if (_connected) return;
        await Task.Run(() =>
        {
            _sshClient.Connect();
            if (!_sshClient.IsConnected)
                throw new Exception($"Failed to connect SSH client to {GetRioHostname(_teamNumber)}");

            _scpClient.Connect();
            if (!_scpClient.IsConnected)
                throw new Exception($"Failed to connect SCP client to {GetRioHostname(_teamNumber)}");
        });
        _connected = true;
    }

    public async Task<string> RunCommandAsync(string command)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!_sshClient.IsConnected)
                    throw new Exception("SSH client is not connected");

                var result = _sshClient.RunCommand(command);

                return result.ExitStatus != 0
                    ? throw new Exception($"Command failed with exit code {result.ExitStatus}: {result.Error}")
                    : result.Result;
            }
            catch (Exception ex)
            {
                throw new Exception($"SSH command execution failed: {ex.GetType().Name} {ex.Message}", ex);
            }
        });
    }

    public async Task<List<string>> RunCommandsAsync(params string[] commands)
    {
        var results = new List<string>();
        foreach (var command in commands) results.Add(await RunCommandAsync(command));
        return results;
    }

    public async Task<string> DownloadFileScpAsync(string remotePath, string localPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!_scpClient.IsConnected)
                    throw new Exception("SCP client is not connected");

                _scpClient.Download(remotePath, new FileInfo(localPath));
                return $"Downloaded {remotePath} to {localPath}";
            }
            catch (Exception ex)
            {
                throw new Exception($"SCP download failed: {ex.Message}", ex);
            }
        });
    }

    public async Task<List<string>> DownloadFilesScpAsync(IEnumerable<(string remotePath, string localPath)> files)
    {
        var results = new List<string>();
        foreach (var (remotePath, localPath) in files) results.Add(await DownloadFileScpAsync(remotePath, localPath));
        return results;
    }

    public async Task DownloadFileHttpAsync(string url, string outputPath)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream);
    }
}
