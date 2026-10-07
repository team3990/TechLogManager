using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using TechLogManager;

namespace TechLogManager.Cli;

// Command line version of TechLogManager.
// stdout only contains the result (a table, or JSON with --json), progress and errors go to stderr.
// Exit codes: 0 = success, 1 = something failed, 2 = invalid usage (nothing selected, unknown name, delete without --yes)
internal static class Program
{
    private const int Success = 0;
    private const int Failure = 1;
    private const int Usage = 2;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly Option<string?> HostOption = new("--host")
    {
        Description = $"Robot address (default: the GUI setting, or {RobotConnection.DefaultHost})",
        Recursive = true
    };

    private static readonly Option<bool> JsonOption = new("--json")
    {
        Description = "Print the result as JSON",
        Recursive = true
    };

    private static readonly Option<string[]> SourceOption = new("--source", "-s")
    {
        Description = "Log types to include: wpilog, hoot, limelight (comma separated or repeated, default: all)",
        AllowMultipleArgumentsPerToken = true
    };

    private static readonly Option<bool> MatchesOption = new("--matches")
    {
        Description = "Only include logs recorded during an FMS match"
    };

    private static readonly Option<int?> LatestOption = new("--latest", "-n")
    {
        Description = "Only the N newest logs"
    };

    private static readonly Option<bool> AllOption = new("--all")
    {
        Description = "Every log that matches the filters"
    };

    private static readonly Argument<string[]> NamesArgument = new("names")
    {
        Description = "Log names, as printed by 'tlm list'",
        Arity = ArgumentArity.ZeroOrMore
    };

    private static async Task<int> Main(string[] args)
    {
        var listCommand = new Command("list", "List the logs on the robot (newest first)")
        {
            SourceOption, MatchesOption, LatestOption
        };
        listCommand.SetAction((result, ct) => RunAsync(result, ct, ListAsync));

        var destOption = new Option<string?>("--dest", "-d")
        {
            Description = "Destination folder (default: '{repository}/{yyyy-MM-dd-HH'h'mm}' from the GUI settings)"
        };
        var downloadCommand = new Command("download", "Download logs")
        {
            NamesArgument, SourceOption, MatchesOption, LatestOption, AllOption, destOption
        };
        downloadCommand.SetAction((result, ct) =>
            RunAsync(result, ct, (manager, entries, json, token) =>
                DownloadAsync(manager, entries, json, result.GetValue(destOption), token)));

        var yesOption = new Option<bool>("--yes", "-y")
        {
            Description = "Actually delete (without it, only prints what would be deleted)"
        };
        var deleteCommand = new Command("delete", "Delete logs from the robot / Limelights")
        {
            NamesArgument, SourceOption, MatchesOption, LatestOption, AllOption, yesOption
        };
        deleteCommand.SetAction((result, ct) =>
            RunAsync(result, ct, (manager, entries, json, token) =>
                DeleteAsync(manager, entries, json, result.GetValue(yesOption), token)));

        var root = new RootCommand("TechLogManager: list, download and delete robot logs (Systemcore + Limelight)")
        {
            HostOption, JsonOption, listCommand, downloadCommand, deleteCommand
        };

        return await root.Parse(args).InvokeAsync();
    }

    private delegate Task<int> CommandBody(LogManager manager, List<LogEntry> entries, bool json, CancellationToken ct);

    /// <summary>Connects, lists, filters and selects the logs, then runs <paramref name="body"/> on the selection.</summary>
    private static async Task<int> RunAsync(ParseResult result, CancellationToken ct, CommandBody body)
    {
        var settings = SettingsManager.Load();
        var json = result.GetValue(JsonOption);

        List<LogSource> sources;
        try
        {
            sources = ParseSources(result.GetValue(SourceOption));
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return Usage;
        }

        using var manager = new LogManager(result.GetValue(HostOption) ?? settings.RobotHost, Console.Error.WriteLine);
        try
        {
            await manager.ConnectAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine(ex.Message);
            return Failure;
        }

        var listResult = await manager.ListAsync(sources, ct);
        IEnumerable<LogEntry> entries = listResult.Entries;
        if (result.GetValue(MatchesOption)) entries = entries.Where(e => e.Match != null);

        // 'list' has no names/--all: it shows everything that matches the filters
        var isList = result.CommandResult.Command.Name == "list";
        var names = isList ? [] : result.GetValue(NamesArgument) ?? [];
        var all = !isList && result.GetValue(AllOption);
        var latest = result.GetValue(LatestOption);

        List<LogEntry> selected;
        if (names.Length > 0)
        {
            var byName = entries.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
            var missing = names.Where(n => !byName.ContainsKey(n)).ToList();
            if (missing.Count > 0)
            {
                Console.Error.WriteLine($"Log(s) not found: {string.Join(", ", missing)}");
                return Usage;
            }

            selected = names.Select(n => byName[n]).Distinct().ToList();
        }
        else if (isList || all || latest != null)
        {
            selected = entries.ToList();
        }
        else
        {
            Console.Error.WriteLine("Nothing selected: pass log names, --latest N or --all");
            return Usage;
        }

        if (latest != null) selected = selected.Take(latest.Value).ToList();

        var exitCode = await body(manager, selected, json, ct);

        // A source that failed to list makes the selection incomplete, unless the logs were picked by name (all found)
        return listResult.Errors.Count > 0 && names.Length == 0 && exitCode == Success ? Failure : exitCode;
    }

    private static List<LogSource> ParseSources(string[]? values)
    {
        if (values == null || values.Length == 0) return Enum.GetValues<LogSource>().ToList();

        return values
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => Enum.TryParse<LogSource>(v, true, out var source)
                ? source
                : throw new ArgumentException($"Unknown source '{v}' (expected wpilog, hoot or limelight)"))
            .Distinct()
            .ToList();
    }

    private static Task<int> ListAsync(LogManager manager, List<LogEntry> entries, bool json, CancellationToken ct)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, Json));
            return Task.FromResult(Success);
        }

        foreach (var entry in entries)
            Console.WriteLine(string.Join("  ",
                (entry.Date?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "").PadRight(19),
                entry.Source.ToString().PadRight(9),
                (entry.Size is { } size ? $"{size / 1024.0 / 1024.0:0.0} MB" : "").PadLeft(9),
                (entry.Match is { } match ? $"{match.Event} {match.Type.ToString()[0]}{match.Number}" : "").PadRight(14),
                entry.Name));
        Console.Error.WriteLine($"{entries.Count} log(s)");
        return Task.FromResult(Success);
    }

    private static async Task<int> DownloadAsync(LogManager manager, List<LogEntry> entries, bool json, string? dest,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dest))
        {
            var repository = SettingsManager.Load().RepositoryLocation;
            if (string.IsNullOrWhiteSpace(repository))
            {
                Console.Error.WriteLine("No destination: pass --dest or set the repository location in the GUI settings");
                return Usage;
            }

            dest = Path.Combine(repository, DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm"));
        }

        var results = new List<TransferResult>();
        foreach (var entry in entries)
            try
            {
                var path = await manager.DownloadAsync(entry, dest, ct: ct);
                results.Add(new TransferResult(entry.Name, entry.Source, path, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"Error downloading {entry.Name}: {ex.Message}");
                results.Add(new TransferResult(entry.Name, entry.Source, null, ex.Message));
            }

        if (json) Console.WriteLine(JsonSerializer.Serialize(results, Json));
        else
            foreach (var r in results.Where(r => r.Path != null))
                Console.WriteLine(r.Path);

        Console.Error.WriteLine($"Downloaded {results.Count(r => r.Error == null)}/{results.Count} log(s) to {dest}");
        return results.All(r => r.Error == null) ? Success : Failure;
    }

    private static async Task<int> DeleteAsync(LogManager manager, List<LogEntry> entries, bool json, bool yes,
        CancellationToken ct)
    {
        if (!yes)
        {
            Console.Error.WriteLine($"Would delete {entries.Count} log(s), pass --yes to delete them:");
            foreach (var entry in entries) Console.Error.WriteLine($"  {entry.Name}");
            return Usage;
        }

        var results = new List<TransferResult>();
        foreach (var entry in entries)
            try
            {
                await manager.DeleteAsync(entry, ct);
                results.Add(new TransferResult(entry.Name, entry.Source, null, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"Error deleting {entry.Name}: {ex.Message}");
                results.Add(new TransferResult(entry.Name, entry.Source, null, ex.Message));
            }

        if (json) Console.WriteLine(JsonSerializer.Serialize(results, Json));
        Console.Error.WriteLine($"Deleted {results.Count(r => r.Error == null)}/{results.Count} log(s)");
        return results.All(r => r.Error == null) ? Success : Failure;
    }

    private sealed record TransferResult(string Name, LogSource Source, string? Path, string? Error);
}
