namespace TechLogManager;

public static class Gui
{
    private const string Title = "Tech log manager";

    public static string AskTeamNumber()
    {
        string logo1Path = "Images/team9606_logo.png";  
        string logo2Path = "Images/team3990_logo.png";  

        return GuiLib.ShowLogoChoice(
            Title, 
            "Select Your Team", 
            logo1Path, 
            logo2Path, 
            "9606", 
            "3990"
        );
    }

    public static int AskAction()
    {
        return GuiLib.ShowChoices(Title, "Choose your operation", "download&delete", "download", "delete");
    }

    public static bool AskCommit()
    {
        return GuiLib.ShowChoices(Title, "Do you want to commit?", "yes", "no") == 0;
    }

    public static bool AskPush()
    {
        return GuiLib.ShowChoices(Title, "Do you want to push?", "yes", "no") == 0;
    }
}