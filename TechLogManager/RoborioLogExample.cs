using System;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace TechLogManager;

/// <summary>
///     Example implementation for RoboRIO log operations
///     Add this to your MainWindow.axaml.cs or create a separate RoborioLogger class
/// </summary>
public class RoborioLogExample
{
    // Replace the TODO in MainWindow.axaml.cs ProcessRoborioLogs with this:
    public static async Task DownloadRoborioLogs(string teamNumber, int action, Action<string> logCallback)
    {
        var host = $"roboRIO-{teamNumber}-FRC.local";
        var username = "lvuser";
        var password = ""; // RoboRIO typically has no password
        var remoteLogDir = "/home/lvuser/logs";

        logCallback("Connecting to RoboRIO...");

        await Task.Run(() =>
        {
            using var sshClient = new SshClient(host, username, password);
            sshClient.Connect();

            // Get list of log files
            var listCommand = sshClient.RunCommand($"ls {remoteLogDir}");
            if (listCommand.ExitStatus != 0) throw new Exception($"Failed to list log files: {listCommand.Error}");

            var logFiles = listCommand.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            logCallback($"Found {logFiles.Length} log file(s)");

            sshClient.Disconnect();

            if (action < 2 && logFiles.Length > 0) // Download
            {
                // Create timestamped folder
                var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
                var localFolder = Path.Combine(date + "@" + teamNumber, "roborio");
                Directory.CreateDirectory(localFolder);

                using var scpClient = new ScpClient(host, username, password);
                scpClient.Connect();

                foreach (var logFile in logFiles)
                {
                    var fileName = logFile.Trim();
                    if (string.IsNullOrEmpty(fileName)) continue;

                    var remotePath = $"{remoteLogDir}/{fileName}";
                    var localPath = Path.Combine(localFolder, fileName);

                    logCallback($"  Downloading {fileName}...");

                    using var fileStream = File.Create(localPath);
                    scpClient.Download(remotePath, fileStream);

                    logCallback($"  Downloaded {fileName}");
                }

                scpClient.Disconnect();
                logCallback($"All RoboRIO logs downloaded to {localFolder}");
            }

            if (action == 0 || action == 2) // Delete
            {
                using var sshClient2 = new SshClient(host, username, password);
                sshClient2.Connect();

                logCallback("Deleting RoboRIO logs...");
                var deleteCommand = sshClient2.RunCommand($"rm -rf {remoteLogDir}/*.wpilog {remoteLogDir}/*.hoot");

                if (deleteCommand.ExitStatus != 0)
                    logCallback($"Warning: Delete may have failed: {deleteCommand.Error}");
                else
                    logCallback("RoboRIO logs deleted");

                sshClient2.Disconnect();
            }
        });
    }

    // Example: How to filter for specific log types (TBD vs dated logs)
    public static bool IsTbdLog(string fileName)
    {
        return fileName.StartsWith("frc_tbd", StringComparison.OrdinalIgnoreCase);
    }

    // Example: How to rename TBD logs with proper timestamp
    public static async Task RenameTbdLogs(string folderPath, string teamNumber, Action<string> logCallback)
    {
        var files = Directory.GetFiles(folderPath, "*.wpilog");
        var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (IsTbdLog(fileName))
            {
                var newName = fileName.Replace("frc_tbd", $"FRC_{date}");
                var newPath = Path.Combine(Path.GetDirectoryName(file)!, newName);

                File.Move(file, newPath);
                logCallback($"Renamed: {fileName} -> {newName}");
            }
        }

        await Task.CompletedTask;
    }
}

/// <summary>
///     Integration example for MainWindow.axaml.cs
/// </summary>
public class MainWindowIntegrationExample
{
    // Replace the ProcessRoborioLogs method in MainWindow.axaml.cs with:
    /*
    private async Task ProcessRoborioLogs(string teamNumber, int action)
    {
        Log("Processing RoboRIO logs...");

        try
        {
            await RoborioLogExample.DownloadRoborioLogs(teamNumber, action, Log);
        }
        catch (Exception ex)
        {
            Log($"RoboRIO error: {ex.Message}");
            throw;
        }
    }
    */
}
