using System.Text.RegularExpressions;

namespace TechLogManager;

public partial class MainWindow
{
    [GeneratedRegex(@"^(.+\.wpilog)$")]
    private static partial Regex WpilogRegex();
    private async Task ProcessRoborioLogs(string teamNumber, Action action, string destination)
    {
        Log("Processing RoboRIO logs...");

        if (action.IsDownload())
        {
            var f1 = await SshCommand(teamNumber, "find /home/lvuser/logs -name '*.wpilog");
            var f2 = await SshCommand(teamNumber, "find /U/logs -name '*.wpilog'");
            if (f1.IsWhiteSpace()) return;
            if (f2.IsWhiteSpace()) return;
            var files = f1.Split("\n").Concat(f2.Split("\n")).ToList();

            var matches = files.Where(s => WpilogRegex().IsMatch(s))
                .Select(s => WpilogRegex().Match(s))
                .ToArray();

            foreach (var file in matches)
            {
                var fileName = Path.GetFileName(file.Groups[1].Value);
                Log($"Processing file {fileName}");
                var result = await ScpTransfer(teamNumber, file.Groups[1].Value, destination);
                Log(result);
            }
        }

        if (action.IsDelete())
        {
            var result1 = await SshCommand(teamNumber, "rm -f /home/lvuser/logs/*.wpilog");
            Log(result1);
            var result2 = await SshCommand(teamNumber, "rm -f /U/logs/*.wpilog");
            Log(result2);
        }
    }
}
