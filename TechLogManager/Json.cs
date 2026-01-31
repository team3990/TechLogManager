using System.Text.Json;

namespace TechLogManager;

public static class Json
{
    public static List<string> ParseJsonStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json);
    }
}
