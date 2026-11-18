namespace OopGames.Desktop.Ui;

/// <summary>Small factory helpers so every form builds its controls the same way.</summary>
internal static class UiKit
{
    public static readonly Font TitleFont = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font BoldFont = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font LogFont = new("Consolas", 9.5f);

    public static Label Title(string text) => new()
    {
        Text = text,
        Font = TitleFont,
        AutoSize = true,
        Margin = new Padding(3, 3, 3, 9),
    };

    public static Label Text(string text = "", bool bold = false, int maxWidth = 0) => new()
    {
        Text = text,
        AutoSize = true,
        Font = bold ? BoldFont : Control.DefaultFont,
        MaximumSize = new Size(maxWidth, 0),
        Margin = new Padding(3, 6, 3, 3),
    };

    public static Button Button(string text, EventHandler onClick, int width = 120)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            Margin = new Padding(3),
        };
        button.Click += onClick;
        return button;
    }

    public static ProgressBar HealthBar(int max) => new()
    {
        Maximum = max,
        Value = max,
        Width = 180,
        Height = 16,
        Margin = new Padding(3, 6, 3, 3),
    };

    /// <summary>Progress bars throw if the value is out of range, so clamp it.</summary>
    public static void SetHealth(ProgressBar bar, int value) => bar.Value = Math.Clamp(value, bar.Minimum, bar.Maximum);

    public static ListBox Log(int height = 160) => new()
    {
        Font = LogFont,
        Height = height,
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        HorizontalScrollbar = true,
    };

    /// <summary>Adds the lines at the bottom of the log and scrolls to them.</summary>
    public static void Append(ListBox log, IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            log.Items.Add(line);
        }

        if (log.Items.Count > 0)
        {
            log.TopIndex = log.Items.Count - 1;
        }
    }

    /// <summary>A vertical stack of controls that sizes itself to its content.</summary>
    public static FlowLayoutPanel Column(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    /// <summary>A horizontal row of controls that sizes itself to its content.</summary>
    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    public static GroupBox Group(string text, Control content)
    {
        var group = new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(9),
        };
        // Not docked: an auto-sized group box grows to fit the bounds of its content.
        content.Location = new Point(group.DisplayRectangle.Left, group.DisplayRectangle.Top);
        group.Controls.Add(content);
        return group;
    }

    /// <summary>Common settings for every game window.</summary>
    public static void SetUpWindow(Form form, string title)
    {
        form.Text = title;
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.AutoSize = true;
        form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        form.Padding = new Padding(12);
    }
}
