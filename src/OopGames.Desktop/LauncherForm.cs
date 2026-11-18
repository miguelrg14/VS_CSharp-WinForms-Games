using OopGames.Desktop.Forms;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop;

/// <summary>Main window: one entry per project, in the order they were made during the course.</summary>
internal sealed class LauncherForm : Form
{
    private sealed record Entry(string Title, string Description, Func<Form> Create);

    private static readonly Entry[] Entries =
    [
        new("OOP basics",
            "Inheritance with abstract classes, dialogs, Fibonacci and factorial, distance between points and a football squad table.",
            () => new BasicsForm()),
        new("Duel",
            "A striker with critical hits against a guardian with a shield, both used through the same interfaces.",
            () => new DuelForm()),
        new("Elemental tower",
            "Climb a tower of fire, water and grass enemies. Choosing the right element doubles your damage.",
            () => new ElementalTowerForm()),
        new("Weapon arena",
            "Three against three with the sword > axe > spear triangle. Pick who attacks whom.",
            () => new ArenaForm()),
        new("Blackjack",
            "Final project: two-player blackjack where winning a round hurts the other player. With health, shields, bets and trump cards.",
            () => new BlackjackForm()),
    ];

    public LauncherForm()
    {
        Text = "OOP Games";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        Label title = UiKit.Title("Object-oriented programming in C#");
        grid.Controls.Add(title, 0, 0);
        grid.SetColumnSpan(title, 2);

        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            grid.Controls.Add(UiKit.Column(UiKit.Text($"{i + 1}. {entry.Title}", bold: true), UiKit.Text(entry.Description, maxWidth: 420)), 0, i + 1);
            grid.Controls.Add(UiKit.Button("Open", (_, _) => Open(entry), 90), 1, i + 1);
        }

        Controls.Add(grid);
    }

    private void Open(Entry entry)
    {
        using Form form = entry.Create();
        form.ShowDialog(this);
    }
}
