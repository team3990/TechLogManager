using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace TechLogManager;

public static class GitOperations
{
    public static void GitCommit(string repoPath, string message)
    {
        try
        {
            using var repo = new Repository(repoPath);
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

    public static async Task GitPushAsync(string repoPath, string? user = null, string? password = null)
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(repoPath);
                // Get the current branch
                var currentBranch = repo.Head;

                if (currentBranch.TrackedBranch == null)
                    throw new Exception("Current branch has no upstream tracking branch");

                // Get the remote
                var remote = repo.Network.Remotes["origin"];
                if (remote == null) throw new Exception("Remote 'origin' not found");

                // Push options (for authentication if needed)
                PushOptions options;
                if (user != null && password != null)
                    options = new PushOptions
                    {
                        CredentialsProvider = GetCredentialHandler(user, password)
                    };
                else options = new PushOptions();

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

    public static async Task<bool> GitCloneAsync(string repoUrl, string repoPath, string? user = null, string? password = null)
    {
        await Task.Run(() =>
        {
            try
            {
                if (user != null && password != null)
                    Repository.Clone(repoUrl, repoPath, new CloneOptions
                    {
                        FetchOptions =
                        {
                            CredentialsProvider = GetCredentialHandler(user, password)
                        }
                    });
                else Repository.Clone(repoUrl, repoPath);
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        });
        return false;
    }

    private static CredentialsHandler? GetCredentialHandler(string? user, string? password)
    {
        if (user == null || password == null) return null;
        return (_, _, _) => new UsernamePasswordCredentials
        {
            Username = user,
            Password = password
        };
    }
}
