namespace TechLogManager;

public static class GuiLib
{
    public static int ShowChoices(string title, string message, string button1Text, string button2Text, string button3Text)
    {
        var result = -1;

        // Create the form
        var dialog = new Form
        {
            Text = title,
            Width = 400,
            Height = 200,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        // Add label for message
        var label = new Label
        {
            Text = message,
            Left = 20,
            Top = 20,
            Width = 360,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Add buttons
        var button1 = new Button
        {
            Text = button1Text,
            Left = 20,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.OK
        };
        button1.Click += (sender, e) =>
        {
            result = 0;
            dialog.Close();
        };

        var button2 = new Button
        {
            Text = button2Text,
            Left = 140,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.Cancel
        };
        button2.Click += (sender, e) =>
        {
            result = 1;
            dialog.Close();
        };

        var button3 = new Button
        {
            Text = button3Text,
            Left = 260,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.Abort
        };
        button3.Click += (sender, e) =>
        {
            result = 2;
            dialog.Close();
        };

        // Add controls to form
        dialog.Controls.Add(label);
        dialog.Controls.Add(button1);
        dialog.Controls.Add(button2);
        dialog.Controls.Add(button3);

        // Show dialog
        dialog.ShowDialog();

        return result;
    }
    
    public static int ShowChoices(string title, string message, string button1Text, string button2Text)
    {
        var result = -1;

        // Create the form
        var dialog = new Form
        {
            Text = title,
            Width = 300,
            Height = 200,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        // Add label for message
        var label = new Label
        {
            Text = message,
            Left = 20,
            Top = 20,
            Width = 120,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Add buttons
        var button1 = new Button
        {
            Text = button1Text,
            Left = 20,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.OK
        };
        button1.Click += (sender, e) =>
        {
            result = 0;
            dialog.Close();
        };

        var button2 = new Button
        {
            Text = button2Text,
            Left = 140,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.Cancel
        };
        button2.Click += (sender, e) =>
        {
            result = 1;
            dialog.Close();
        };

        // Add controls to form
        dialog.Controls.Add(label);
        dialog.Controls.Add(button1);
        dialog.Controls.Add(button2);

        // Show dialog
        dialog.ShowDialog();

        return result;
    }
    
    public static string ShowInputDialog(string title, string promptText)
    {
        Form prompt = new Form()
        {
            Width = 400,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };
    
        Label textLabel = new Label() 
        { 
            Left = 20, 
            Top = 20, 
            Text = promptText,
            AutoSize = true
        };
    
        TextBox textBox = new TextBox() 
        { 
            Left = 20, 
            Top = 50, 
            Width = 340 
        };
    
        Button confirmation = new Button() 
        { 
            Text = "OK", 
            Left = 280, 
            Width = 80, 
            Top = 80,
            DialogResult = DialogResult.OK 
        };
    
        confirmation.Click += (sender, e) => { prompt.Close(); };
    
        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;
    
        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }
}
