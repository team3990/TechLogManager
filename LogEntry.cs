namespace TechLogManager;

public class LogEntry(string name, LogSource source, Func<string, Action, Task> actionCallback)
{
    public readonly Func<string, Action, Task> ActionCallback = actionCallback;
    public readonly string Name = name;
    public readonly LogSource Source = source;
}

public enum LogSource
{
    DriverStation,
    Hoot,
    Limelight,
    RoboRio
}
