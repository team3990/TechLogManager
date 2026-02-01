namespace TechLogManager;

public static class GuiLib
{
    public static int[] ShowCheckboxChoices(string title, string message, params string[] options)
    {
        const int checkboxHeight = 25;
        const int checkboxSpacing = 5;
        const int checkboxWidth = 260;
        const int margin = 20;
        const int buttonHeight = 30;
        const int labelHeight = 60;

        var totalCheckboxHeight = options.Length * checkboxHeight + (options.Length - 1) * checkboxSpacing;
        var formHeight =
            margin + labelHeight + margin + totalCheckboxHeight + margin + buttonHeight + margin +
            40; // +40 for title bar

        var dialog = new Form
        {
            Text = title,
            Width = 300,
            Height = formHeight,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = message,
            Left = margin,
            Top = margin,
            Width = checkboxWidth,
            Height = labelHeight,
            TextAlign = ContentAlignment.MiddleCenter
        };

        dialog.Controls.Add(label);

        var checkboxes = new CheckBox[options.Length];
        var currentTop = margin + labelHeight + margin;

        for (var i = 0; i < options.Length; i++)
        {
            var checkbox = new CheckBox
            {
                Text = options[i],
                Left = margin,
                Top = currentTop,
                Width = checkboxWidth,
                Height = checkboxHeight
            };

            checkboxes[i] = checkbox;
            dialog.Controls.Add(checkbox);
            currentTop += checkboxHeight + checkboxSpacing;
        }

        int[]? result = null;

        var okButton = new Button
        {
            Text = "OK",
            Left = margin + (checkboxWidth - 100) / 2,
            Top = currentTop + margin,
            Width = 100,
            Height = buttonHeight
        };

        okButton.Click += (_, _) =>
        {
            var selected = new List<int>();
            for (var i = 0; i < checkboxes.Length; i++)
                if (checkboxes[i].Checked)
                    selected.Add(i);

            result = selected.ToArray();
            dialog.Close();
        };

        dialog.Controls.Add(okButton);
        dialog.ShowDialog();

        return result ?? [];
    }

    public static int ShowChoices(string title, string message, params string[] buttons)
    {
        var result = -1;

        const int buttonHeight = 30;
        const int buttonSpacing = 10;
        const int buttonWidth = 260;
        const int margin = 20;

        var labelHeight = 60;
        var totalButtonHeight = buttons.Length * buttonHeight + (buttons.Length - 1) * buttonSpacing;
        var formHeight = margin + labelHeight + margin + totalButtonHeight + margin + 40; // +40 for title bar

        var dialog = new Form
        {
            Text = title,
            Width = 300,
            Height = formHeight,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = message,
            Left = margin,
            Top = margin,
            Width = buttonWidth,
            Height = labelHeight,
            TextAlign = ContentAlignment.MiddleCenter
        };

        dialog.Controls.Add(label);

        var currentTop = margin + labelHeight + margin;

        for (var i = 0; i < buttons.Length; i++)
        {
            var buttonIndex = i;
            var button = new Button
            {
                Text = buttons[i],
                Left = margin,
                Top = currentTop,
                Width = buttonWidth,
                Height = buttonHeight
            };

            button.Click += (_, _) =>
            {
                result = buttonIndex;
                dialog.Close();
            };

            dialog.Controls.Add(button);
            currentTop += buttonHeight + buttonSpacing;
        }

        dialog.ShowDialog();

        return result;
    }

    public static int ShowChoices(string title, string message, string button1Text, string button2Text,
        string button3Text)
    {
        var result = -1;

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

        var label = new Label
        {
            Text = message,
            Left = 20,
            Top = 20,
            Width = 360,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var button1 = new Button
        {
            Text = button1Text,
            Left = 20,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.OK
        };
        button1.Click += (_, _) =>
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
        button2.Click += (_, _) =>
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

        button3.Click += (_, _) =>
        {
            result = 2;
            dialog.Close();
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(button1);
        dialog.Controls.Add(button2);
        dialog.Controls.Add(button3);

        dialog.ShowDialog();

        return result;
    }

    public static int ShowChoices(string title, string message, string button1Text, string button2Text)
    {
        var result = -1;

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

        var label = new Label
        {
            Text = message,
            Left = 20,
            Top = 20,
            Width = 260,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var button1 = new Button
        {
            Text = button1Text,
            Left = 20,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.OK
        };
        button1.Click += (_, _) =>
        {
            result = 0;
            dialog.Close();
        };

        var button2 = new Button
        {
            Text = button2Text,
            Left = 160,
            Top = 100,
            Width = 100,
            DialogResult = DialogResult.Cancel
        };
        button2.Click += (_, _) =>
        {
            result = 1;
            dialog.Close();
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(button1);
        dialog.Controls.Add(button2);

        dialog.ShowDialog();

        return result;
    }

    public static string ShowInputDialog(string title, string promptText)
    {
        var prompt = new Form
        {
            Width = 400,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var textLabel = new Label
        {
            Left = 20,
            Top = 20,
            Text = promptText,
            AutoSize = true
        };

        var textBox = new TextBox
        {
            Left = 20,
            Top = 50,
            Width = 340
        };

        var confirmation = new Button
        {
            Text = "OK",
            Left = 280,
            Width = 80,
            Top = 80,
            DialogResult = DialogResult.OK
        };

        confirmation.Click += (_, _) => { prompt.Close(); };

        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }

    public static string ShowLogoChoice(string title, string message, string logo1Path, string logo2Path,
        string team1Number, string team2Number)
    {
        var result = string.Empty;

        var dialog = new Form
        {
            Text = title,
            Width = 500,
            Height = 500,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        var label = new Label
        {
            Text = message,
            Left = 20,
            Top = 20,
            Width = 460,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 12, FontStyle.Bold)
        };

        var button1 = new Button
        {
            Left = 50,
            Top = 80,
            Width = 150,
            Height = 150,
            BackColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };

        try
        {
            if (File.Exists(logo1Path))
            {
                button1.BackgroundImage = Image.FromFile(logo1Path);
                button1.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                button1.Text = team1Number;
                button1.Font = new Font("Arial", 16, FontStyle.Bold);
            }
        }
        catch
        {
            button1.Text = team1Number;
            button1.Font = new Font("Arial", 16, FontStyle.Bold);
        }

        button1.Click += (_, _) =>
        {
            result = team1Number;
            dialog.Close();
        };

        // Create button 2 with logo
        var button2 = new Button
        {
            Left = 280,
            Top = 80,
            Width = 150,
            Height = 150,
            BackColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };

        try
        {
            if (File.Exists(logo2Path))
            {
                button2.BackgroundImage = Image.FromFile(logo2Path);
                button2.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                button2.Text = team2Number;
                button2.Font = new Font("Arial", 16, FontStyle.Bold);
            }
        }
        catch
        {
            button2.Text = team2Number;
            button2.Font = new Font("Arial", 16, FontStyle.Bold);
        }

        button2.Click += (_, _) =>
        {
            result = team2Number;
            dialog.Close();
        };

        var label1 = new Label
        {
            Text = $"Team {team1Number}",
            Left = 50,
            Top = 240,
            Width = 150,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        var label2 = new Label
        {
            Text = $"Team {team2Number}",
            Left = 280,
            Top = 240,
            Width = 150,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        var textBox = new TextBox
        {
            Left = 50,
            Top = 350,
            Width = 100
        };

        var confirmation = new Button
        {
            Text = "OK",
            Left = 200,
            Width = 80,
            Top = 350,
            DialogResult = DialogResult.OK
        };

        confirmation.Click += (_, _) => { dialog.Close(); };

        dialog.Controls.Add(label);
        dialog.Controls.Add(button1);
        dialog.Controls.Add(button2);
        dialog.Controls.Add(label1);
        dialog.Controls.Add(label2);
        dialog.Controls.Add(textBox);
        dialog.Controls.Add(confirmation);

        var dialogResult = dialog.ShowDialog();

        if (dialogResult == DialogResult.OK) result = textBox.Text;

        return result;
    }
}
