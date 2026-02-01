using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace TechLogManager;

public partial class MainWindow : Window
{
    private string? _selectedTeam;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Team9406Button_Click(object? sender, RoutedEventArgs e)
    {
        _selectedTeam = "9406";
        SelectedTeamText.Text = "Selected: Team 9406";
    }

    private void Team3990Button_Click(object? sender, RoutedEventArgs e)
    {
        _selectedTeam = "3990";
        SelectedTeamText.Text = "Selected: Team 3990";
    }

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedTeam))
        {
            await ShowMessageDialog("Error", "Please select a team first!");
            return;
        }

        StartButton.IsEnabled = false;
        LogOutput.Text = "";

        try
        {
            var action = GetSelectedAction();
            var downloadRoborio = RoborioCheckbox.IsChecked ?? false;
            var downloadLimelight = LimelightCheckbox.IsChecked ?? false;

            if (!downloadRoborio && !downloadLimelight)
            {
                await ShowMessageDialog("Error", "Please select at least one log type!");
                return;
            }

            Log("Starting log operations...");
            Log($"Team: {_selectedTeam}");
            Log($"Action: {action}");
            Log("");

            if (downloadLimelight) await ProcessLimelightLogs(_selectedTeam, action);

            if (downloadRoborio) await ProcessRoborioLogs(_selectedTeam, action);

            Log("");
            Log("Operations completed!");

            CommitButton.IsEnabled = true;
            PushButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            await ShowMessageDialog("Error", $"Operation failed: {ex.Message}");
        }
        finally
        {
            StartButton.IsEnabled = true;
        }
    }

    private async void CommitButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            CommitButton.IsEnabled = false;
            Log("Committing to git...");
            await GitCommit();
            Log("Git commit successful!");
        }
        catch (Exception ex)
        {
            Log($"Git commit failed: {ex.Message}");
            await ShowMessageDialog("Error", $"Git commit failed: {ex.Message}");
        }
        finally
        {
            CommitButton.IsEnabled = true;
        }
    }

    private async void PushButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            PushButton.IsEnabled = false;
            Log("Pushing to remote...");
            await GitPush();
            Log("Git push successful!");
        }
        catch (Exception ex)
        {
            Log($"Git push failed: {ex.Message}");
            await ShowMessageDialog("Error", $"Git push failed: {ex.Message}");
        }
        finally
        {
            PushButton.IsEnabled = true;
        }
    }

    private int GetSelectedAction()
    {
        if (DlAndDelRadio.IsChecked == true) return 0; // Download and delete
        if (DownloadRadio.IsChecked == true) return 1; // Download only
        if (DeleteRadio.IsChecked == true) return 2; // Delete only
        return 0;
    }

    private async Task ProcessLimelightLogs(string teamNumber, int action)
    {
        Log("Processing Limelight logs...");

        try
        {
            // Get list of limelights from RoboRIO
            var limelightsJson = await SshCommand(
                $"lvuser@roboRIO-{teamNumber}-FRC.local",
                "cat /home/lvuser/limelights.json"
            );

            var limelights = Utils.ParseJsonStringList(limelightsJson);
            Log($"Found {limelights.Count} Limelight(s): {string.Join(", ", limelights)}");

            foreach (var llname in limelights) await ProcessSingleLimelight(teamNumber, action, llname);
        }
        catch (Exception ex)
        {
            Log($"Limelight error: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessSingleLimelight(string teamNumber, int action, string llname)
    {
        Log($"Processing Limelight: {llname}");

        if (action < 2) // Download
        {
            var links = Limelight.GetRecordingLinks(llname);
            Log($"Found {links.Count} recording(s)");

            var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
            var baseFolder = $"{date}@{teamNumber}";

            for (var i = 0; i < links.Count; i++)
            {
                var folderName = Path.Combine(baseFolder, $"rec{i + 1}");
                Directory.CreateDirectory(folderName);

                var recording = links[i];

                // Download video
                if (!string.IsNullOrEmpty(recording.video))
                {
                    Log($"  Downloading video {i + 1}...");
                    await DownloadFile(recording.video, Path.Combine(folderName, "video.avi"));
                }

                // Download manifest
                if (!string.IsNullOrEmpty(recording.manifest))
                {
                    Log($"  Downloading manifest {i + 1}...");
                    await DownloadFile(recording.manifest, Path.Combine(folderName, "manifest.jsonl"));
                }

                // Download bootlog
                if (!string.IsNullOrEmpty(recording.bootlog))
                {
                    Log($"  Downloading bootlog {i + 1}...");
                    await DownloadFile(recording.bootlog, Path.Combine(folderName, "bootlog.txt.gz"));
                }

                Log($"  Downloaded to {folderName}");
            }
        }

        if (action == 0 || action == 2) // Delete
        {
            Log($"Deleting videos from {llname}...");
            Limelight.DeleteAllVideos(llname);
            Log($"Videos deleted from {llname}");
        }
    }

    private async Task ProcessRoborioLogs(string teamNumber, int action)
    {
        Log("Processing RoboRIO logs...");

        // TODO: Implement RoboRIO log download
        // This will involve SCP commands to download .wpilog files
        // You'll need to implement similar to your PowerShell script

        await Task.CompletedTask;
    }

    private void Log(string message)
    {
        Dispatcher.UIThread.Post(() => { LogOutput.Text += message + Environment.NewLine; });
    }

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

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"SSH command failed: {error}");
        }

        return output.Trim();
    }

    private async Task DownloadFile(string url, string outputPath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream);
    }

    private async Task GitCommit()
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

    private async Task GitPush()
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

    private async Task ShowMessageDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 100
        };
        button.Click += (_, _) => dialog.Close();
        stack.Children.Add(button);

        dialog.Content = stack;
        await dialog.ShowDialog(this);
    }
}
