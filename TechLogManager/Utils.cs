using System.Text.Json;

namespace TechLogManager;

public static class Utils
{
    public static bool IsTbd(string fname)
    {
        return fname.StartsWith("frc_tbd", StringComparison.CurrentCultureIgnoreCase); 
    }
    
    public static List<string> ParseJsonStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json);
    }
}
