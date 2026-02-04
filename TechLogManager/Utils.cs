using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace TechLogManager;

public static class Utils
{
    public static bool IsTbd(string fname) => fname.StartsWith("frc_tbd", StringComparison.CurrentCultureIgnoreCase);
    public static List<string>? ParseJsonStringList(string json) => JsonSerializer.Deserialize<List<string>>(json);
    internal static string GetRioHostname(string teamNumber) => $"roboRIO-{teamNumber}-FRC.local";

    internal static readonly string? ExeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
}
