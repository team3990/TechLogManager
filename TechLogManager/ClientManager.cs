using LibGit2Sharp;
using Renci.SshNet;
using static TechLogManager.Utils;

namespace TechLogManager;

public class ClientManager : IDisposable
{
    private readonly string _teamNumber;
    private readonly SshClient _sshClient;
    private readonly ScpClient _scpClient;
    private bool _disposed;

    public ClientManager(string teamNumber)
    {
        _teamNumber = teamNumber;
        var hostname = GetRioHostname(teamNumber);

        _sshClient = new SshClient(hostname, "lvuser", "");
        _scpClient = new ScpClient(hostname, "lvuser", "");
    }

    public async Task ConnectAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                _sshClient.Connect();
                if (!_sshClient.IsConnected)
                    throw new Exception($"Failed to connect SSH client to {GetRioHostname(_teamNumber)}");

                _scpClient.Connect();
                if (!_scpClient.IsConnected)
                    throw new Exception($"Failed to connect SCP client to {GetRioHostname(_teamNumber)}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection failed: {ex.GetType().Name} {ex.Message}", ex);
            }
        });
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

                if (result.ExitStatus != 0)
                    throw new Exception($"Command failed with exit code {result.ExitStatus}: {result.Error}");

                return result.Result;
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
        foreach (var command in commands)
        {
            results.Add(await RunCommandAsync(command));
        }
        return results;
    }

    public async Task<string> DownloadFileAsync(string remotePath, string localPath)
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

    public async Task<List<string>> DownloadFilesAsync(IEnumerable<(string remotePath, string localPath)> files)
    {
        var results = new List<string>();
        foreach (var (remotePath, localPath) in files)
        {
            results.Add(await DownloadFileAsync(remotePath, localPath));
        }
        return results;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_sshClient.IsConnected)
                _sshClient.Disconnect();
            
            if (_scpClient.IsConnected)
                _scpClient.Disconnect();

            _sshClient.Dispose();
            _scpClient.Dispose();
        }
        catch
        {
            // ignored
        }

        _disposed = true;
    }
}

public static class RemoteOperations
{
    public static async Task DownloadFileAsync(string url, string outputPath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream);
    }

    public static void GitCommit(string message)
    {
        try
        {
            using var repo = new Repository(ExeDirectory);
            // Stage all changes (modified, new, and deleted files)
            Commands.Stage(repo, "*");

            // Check if there are any changes to commit
            var status = repo.RetrieveStatus();
            if (!status.Any(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered))
            {
                Console.WriteLine("No changes to commit");
                return;
            }

            // Create signature for the commit
            var signature = new Signature("TechLogManager", "nobody@example.com", DateTimeOffset.Now);

            // Commit the changes
            var commit = repo.Commit(message, signature, signature);

            Console.WriteLine($"Committed: {commit.Sha[..7]} - {commit.MessageShort}");
        }
        catch (RepositoryNotFoundException)
        {
            throw new Exception("Git repository not found in executable directory");
        }
        catch (Exception ex)
        {
            throw new Exception($"Git commit failed: {ex.Message}", ex);
        }
    }

    public static async Task GitPushAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(ExeDirectory);
                // Get the current branch
                var currentBranch = repo.Head;

                if (currentBranch.TrackedBranch == null)
                    throw new Exception("Current branch has no upstream tracking branch");

                // Get the remote
                var remote = repo.Network.Remotes["origin"];
                if (remote == null) throw new Exception("Remote 'origin' not found");

                // Push options (for authentication if needed)
                var options = new PushOptions();

                // Push the current branch
                repo.Network.Push(currentBranch, options);

                Console.WriteLine($"Pushed {currentBranch.FriendlyName} to {remote.Name}");
            }
            catch (RepositoryNotFoundException)
            {
                throw new Exception("Git repository not found in executable directory");
            }
            catch (Exception ex)
            {
                throw new Exception($"Git push failed: {ex.Message}", ex);
            }
        });
    }
}
