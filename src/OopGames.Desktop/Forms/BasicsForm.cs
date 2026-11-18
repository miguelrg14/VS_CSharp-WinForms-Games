using System.Globalization;
using OopGames.Core.Basics;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Forms;

/// <summary>The first exercises of the course, one per tab.</summary>
internal sealed class BasicsForm : Form
{
    public BasicsForm()
    {
        UiKit.SetUpWindow(this, "OOP basics");

        var tabs = new TabControl { Size = new Size(660, 440) };
        tabs.TabPages.Add(Page("Characters", BuildCharactersTab()));
        tabs.TabPages.Add(Page("Dialogs", BuildDialogsTab()));
        tabs.TabPages.Add(Page("Fibonacci & factorial", BuildSequencesTab()));
        tabs.TabPages.Add(Page("Distance", BuildDistanceTab()));
        tabs.TabPages.Add(Page("Football squad", BuildSquadTab()));
        Controls.Add(tabs);
    }

    private static TabPage Page(string title, Control content)
    {
        var page = new TabPage(title) { Padding = new Padding(12) };
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        return page;
    }

    // Inheritance: every item in the list is a Character, but each one describes itself differently.
    private static Control BuildCharactersTab()
    {
        var roster = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        Label type = UiKit.Text(bold: true);
        Label description = UiKit.Text(maxWidth: 560);

        foreach (Character character in CharacterRoster.CreateSample())
        {
            roster.Items.Add(character);
        }

        roster.SelectedIndexChanged += (_, _) =>
        {
            if (roster.SelectedItem is Character character)
            {
                type.Text = $"Runtime type: {character.GetType().Name} (declared as Character)";
                description.Text = character.Describe();
            }
        };
        roster.SelectedIndex = 0;

        return UiKit.Column(
            UiKit.Text("Pick a character. The list holds Character references; the overridden members decide what is shown.", maxWidth: 600),
            roster,
            type,
            description);
    }

    private static Control BuildDialogsTab()
    {
        Label answer = UiKit.Text();
        Button ask = UiKit.Button("Ask a question", (sender, _) =>
        {
            DialogResult result = MessageBox.Show(
                "Do you want to continue?", "Yes, no or cancel", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            answer.Text = $"You answered: {result}";
        }, 160);

        Button second = UiKit.Button("Open a second window", (sender, _) =>
        {
            Form owner = ((Control)sender!).FindForm()!;
            var window = new Form
            {
                Text = "Second window",
                Size = new Size(320, 200),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(owner.Right + 8, owner.Top),
            };
            window.Controls.Add(new Label
            {
                Text = "This window is owned by the first one: it closes with it and stays on top of it.",
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
            });
            window.Show(owner);
        }, 160);

        return UiKit.Column(UiKit.Text("Message boxes return which button was pressed; owned windows follow their owner."), ask, answer, second);
    }

    private static Control BuildSequencesTab()
    {
        var number = new NumericUpDown { Minimum = 0, Maximum = 300, Value = 10, Width = 80 };
        var output = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = UiKit.LogFont,
            Size = new Size(600, 280),
        };

        Button calculate = UiKit.Button("Calculate", (_, _) =>
        {
            int n = (int)number.Value;
            output.Text =
                $"First {n} Fibonacci numbers:{Environment.NewLine}{string.Join(", ", Sequences.Fibonacci(n))}" +
                $"{Environment.NewLine}{Environment.NewLine}{n}! = {Sequences.Factorial(n)}";
        });

        return UiKit.Column(UiKit.Row(UiKit.Text("n ="), number, calculate), output);
    }

    private static Control BuildDistanceTab()
    {
        NumericUpDown x1 = Coordinate(), y1 = Coordinate(), x2 = Coordinate(3), y2 = Coordinate(4);
        Label result = UiKit.Text(bold: true);

        Button calculate = UiKit.Button("Calculate distance", (_, _) =>
        {
            var a = new Point2D((double)x1.Value, (double)y1.Value);
            var b = new Point2D((double)x2.Value, (double)y2.Value);
            result.Text = $"Distance from A to B: {a.DistanceTo(b).ToString("0.###", CultureInfo.CurrentCulture)}";
        }, 160);

        return UiKit.Column(
            UiKit.Row(UiKit.Text("Point A   x"), x1, UiKit.Text("y"), y1),
            UiKit.Row(UiKit.Text("Point B   x"), x2, UiKit.Text("y"), y2),
            calculate,
            result);
    }

    /// <summary>NumericUpDown only accepts numbers, so there is no text to parse and nothing to catch.</summary>
    private static NumericUpDown Coordinate(decimal value = 0) => new()
    {
        Minimum = -10_000,
        Maximum = 10_000,
        DecimalPlaces = 2,
        Value = value,
        Width = 90,
    };

    private static Control BuildSquadTab()
    {
        var squad = new Squad();
        var name = new TextBox { Width = 160 };
        var shirt = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Width = 60 };
        var minutes = new NumericUpDown { Minimum = 0, Maximum = 10_000, DecimalPlaces = 1, Width = 80 };
        var goals = new NumericUpDown { Minimum = 0, Maximum = 500, Width = 60, Enabled = false };
        var position = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        position.Items.AddRange(Enum.GetValues<Position>().Cast<object>().ToArray());
        position.SelectedIndex = 0;
        position.SelectedIndexChanged += (_, _) => goals.Enabled = (Position)position.SelectedItem! != Position.Goalkeeper;

        var table = new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Size = new Size(620, 220),
        };
        Label total = UiKit.Text();

        Button add = UiKit.Button("Add player", (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text))
            {
                MessageBox.Show("Type the player's name.", "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SquadMember member = Squad.Create(name.Text, (int)shirt.Value, (double)minutes.Value, (Position)position.SelectedItem!, (int)goals.Value);
            if (!squad.TryAdd(member, out string? error))
            {
                MessageBox.Show(error, "Cannot add the player", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // A fresh list each time makes the grid refresh; the anonymous type picks the columns to show.
            table.DataSource = squad.Members
                .Select(m => new { m.ShirtNumber, m.Name, m.Position, m.MinutesPlayed, m.Goals })
                .ToList();
            total.Text = $"{squad.Members.Count} players, {squad.TotalGoals} goals in total";
            name.Clear();
        });

        return UiKit.Column(
            UiKit.Row(UiKit.Text("Name"), name, UiKit.Text("Number"), shirt, UiKit.Text("Position"), position),
            UiKit.Row(UiKit.Text("Minutes played"), minutes, UiKit.Text("Goals"), goals, add),
            table,
            total);
    }
}
