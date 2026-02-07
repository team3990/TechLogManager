using System.Text.Json;

namespace TechLogManager;

public class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TechLogManager",
        "settings.json"
    );

    public string DefaultTeamNumber { get; set; } = "";
    public string RepositoryLocation { get; set; } = "";

    public static SettingsManager Instance => field ?? Load();

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    private static SettingsManager Load()
    {
        Console.WriteLine("Loading settings");
        if (!File.Exists(SettingsPath))
        {
            Console.WriteLine("Settings file not found, creating new");
            return new SettingsManager();
        }
        try
        {
            var json = JsonSerializer.Deserialize<SettingsManager>(File.ReadAllText(SettingsPath));
            if (json == null)
            {
                throw new Exception();
            }
            Console.WriteLine("Got settings");
            return json;
        }
        catch
        {
            Console.WriteLine("Failed to read settings file, creating new");
            return new SettingsManager();
        }
    }
}