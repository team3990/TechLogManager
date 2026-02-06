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

    public static SettingsManager Instance
    {
        get
        {
            field ??= Load();
            return field;
        }
    }

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
        if (!File.Exists(SettingsPath)) return new SettingsManager();
        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<SettingsManager>(json) ?? new SettingsManager();
        }
        catch
        {
            return new SettingsManager();
        }
    }
}