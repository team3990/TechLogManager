namespace TechLogManager;

public static class Gui
{
    private const string Title = "Tech log manager";

    public static string AskTeamNumber()
    {
        return GuiLib.ShowInputDialog(Title, "Enter team number");
    }

    public static int AskAction()
    {
        return GuiLib.ShowChoices(Title, "what to do", "dl and del", "dl", "del");
    }

    public static bool AskCommit()
    {
        return GuiLib.ShowChoices(Title, "commit ?", "yes", "no") == 0;
    }

    public static bool AskPush()
    {
        return GuiLib.ShowChoices(Title, "push ?", "yes", "no") == 0;
    }
}
