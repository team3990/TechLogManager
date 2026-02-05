using System.Reflection;
using System.Text.Json;

namespace TechLogManager;

public static class Utils
{
    public static List<string>? ParseJsonStringList(string json) => JsonSerializer.Deserialize<List<string>>(json);

    public static void DirCheck(string dir)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    internal static string GetRioHostname(string teamNumber) => $"roboRIO-{teamNumber}-FRC.local";

    internal static readonly string? ExeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    extension(Action action)
    {
        internal bool IsDownload() => action is Action.Download or Action.DownloadAndDelete;
        internal bool IsDelete() => action is Action.Delete or Action.DownloadAndDelete;
    }
}

internal enum Action
{
    DownloadAndDelete,
    Download,
    Delete
}