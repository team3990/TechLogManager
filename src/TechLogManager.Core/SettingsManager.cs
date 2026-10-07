using System.Text.Json;

namespace TechLogManager;

/// <summary>User settings, shared by the GUI and the CLI.</summary>
public class SettingsManager
{
    public static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TechLogManager",
        "settings.json"
    );

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    public string RobotHost { get; set; } = RobotConnection.DefaultHost;
    public string RepositoryLocation { get; set; } = "";

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(this, PrettyJson);
        File.WriteAllText(SettingsPath, json);
    }

    public static SettingsManager Load()
    {
        if (!File.Exists(SettingsPath))
        {
            Console.Error.WriteLine("Settings file not found, creating new");
            return new SettingsManager();
        }

        try
        {
            var json = JsonSerializer.Deserialize<SettingsManager>(File.ReadAllText(SettingsPath));
            return json ?? throw new Exception("Deserialization returned null");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Failed to read settings file, creating new");
            Console.Error.WriteLine(e);
            return new SettingsManager();
        }
    }
}
