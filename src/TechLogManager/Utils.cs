using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TechLogManager;

public static class Utils
{
    public static async Task ShowMessageDialog(this Window window, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 100
        };
        button.Click += (_, _) => dialog.Close();
        stack.Children.Add(button);

        dialog.Content = stack;
        await dialog.ShowDialog(window);
    }

    public static void Log(string message)
    {
        Console.WriteLine(message);
    }

    extension(string str)
    {
        internal string WrapPath() => str.Replace("\\", "\\​").Replace("/", "/​");
    }
}
