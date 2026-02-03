using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using LibGit2Sharp;
using Renci.SshNet;
using static TechLogManager.Utils;

namespace TechLogManager;

public partial class MainWindow
{
    private static async Task<string> SshCommand(string teamNumber, string command)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SshClient(GetRioHostname(teamNumber), "lvuser", "");
                client.Connect();

                if (!client.IsConnected) throw new Exception($"Failed to connect to {GetRioHostname(teamNumber)}");

                var result = client.RunCommand(command);

                client.Disconnect();

                // Check if the command had errors
                return result.ExitStatus != 0
                    ? throw new Exception($"Command failed with exit code {result.ExitStatus}: {result.Error}")
                    : result.Result;
            }
            catch (Exception ex)
            {
                throw new Exception($"SSH command execution failed: {ex.Message}", ex);
            }
        });
    }

    private static async Task<string> ScpTransfer(string teamNumber, string filepath1, string filepath2)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new ScpClient(GetRioHostname(teamNumber), "lvuser", "");
                client.Connect();

                if (!client.IsConnected) throw new Exception($"Failed to connect to {GetRioHostname(teamNumber)}");

                // Determine transfer direction based on which path is remote
                // Convention: if filepath1 starts with '/', it's a remote path (download)
                // Otherwise, filepath1 is local (upload)
                if (filepath1.StartsWith("/") && !filepath2.StartsWith("/"))
                {
                    // Download: remote -> local
                    client.Download(filepath1, new FileInfo(filepath2));
                    client.Disconnect();
                    return $"Downloaded {filepath1} to {filepath2}";
                }

                // Upload: local -> remote
                client.Upload(new FileInfo(filepath1), filepath2);
                client.Disconnect();
                return $"Uploaded {filepath1} to {filepath2}";
            }
            catch (Exception ex)
            {
                throw new Exception($"SCP transfer failed: {ex.Message}", ex);
            }
        });
    }

    private static async Task DownloadFile(string url, string outputPath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream);
    }

    private static void GitCommit(string message)
    {
        try
        {
            // Get the directory where the executable is located
            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using var repo = new Repository(exeDirectory);
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

    private static async Task GitPush()
    {
        await Task.Run(() =>
        {
            try
            {
                // Get the directory where the executable is located
                var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                using var repo = new Repository(exeDirectory);
                // Get the current branch
                var currentBranch = repo.Head;
                
                if (currentBranch.TrackedBranch == null)
                {
                    throw new Exception("Current branch has no upstream tracking branch");
                }
                
                // Get the remote
                var remote = repo.Network.Remotes["origin"];
                if (remote == null)
                {
                    throw new Exception("Remote 'origin' not found");
                }
                
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
