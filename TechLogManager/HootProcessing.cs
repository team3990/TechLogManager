namespace TechLogManager;

public partial class MainWindow
{
    private async Task ProcessHootLogs(string teamNumber, Action action, bool all, string destination)
    {
        Log("Processing ctre (hoot) logs...");

        if (action.IsDownload())
        {
            var dpath = Path.Combine(destination, "hoot");
            var result = await ScpTransfer(teamNumber, "/home/lvuser/logs/", dpath);
            Log(result);
            result = await ScpTransfer(teamNumber, "/U/logs/", dpath);
            Log(result);
        }
    }
}
