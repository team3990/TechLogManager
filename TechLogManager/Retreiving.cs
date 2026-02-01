using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TechLogManager;

public partial class MainWindow
{
    private async Task<string> SshCommand(string target, string command)
    {
        // This is a placeholder - you'll need to implement SSH execution
        // Options:
        // 1. Use SSH.NET library (recommended)
        // 2. Call ssh.exe via Process.Start

        // For now, using Process.Start as example:
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = $"{target} \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0) return output.Trim();
        var error = await process.StandardError.ReadToEndAsync();
        throw new Exception($"SSH command failed: {error}");

    }

    private static async Task DownloadFile(string url, string outputPath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream);
    }

    private static async Task GitCommit()
    {
        // Option 1: Use LibGit2Sharp (recommended)
        // Option 2: Call git.exe via Process.Start

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "commit -am \"Auto-commit logs\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Git commit failed: {error}");
        }
    }

    private static async Task GitPush()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "push",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Git push failed: {error}");
        }
    }
}
