namespace TechLogManager;

public static class Gui
{
    public static string AskTeamNumber()
    {
        return GuiLib.ShowLogoChoice(
            "Team number",
            "Select Your Team",
            "TechLogManager/img/team9606_logo.png",
            "TechLogManager/img/team3990_logo.png",
            "9606",
            "3990"
        );
    }

    public static int AskActionDlDel(string title)
    {
        return GuiLib.ShowChoices(title, "Choose your operation", "dl and del", "download", "delete");
    }

    public static int AskActionLlWpi()
    {
        return GuiLib.ShowChoices("What logs", "What to download", "ll and rio", "limelight", "roborio");
    }

    public static bool AskCommit()
    {
        return GuiLib.ShowChoices("Commit choice", "Do you want to commit?", "yes", "no") == 0;
    }

    public static bool AskPush()
    {
        return GuiLib.ShowChoices("Push choice", "Do you want to push?", "yes", "no") == 0;
    }
}
