using OopGames.Core.Arena;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Forms;

/// <summary>Squad battle: choose one of your units and one enemy each round; the weapon triangle does the rest.</summary>
internal sealed class ArenaForm : Form
{
    /// <summary>The controls that show one unit.</summary>
    private sealed class UnitCard
    {
        public UnitCard(Unit unit)
        {
            Unit = unit;
            Picture = new PictureBox { Size = new Size(Sprites.Size, Sprites.Size), Image = Sprites.ForWeapon(unit.Weapon, unit.Team) };
            Health = UiKit.HealthBar(unit.MaxHealth);
            Health.Width = 120;
            Select = new RadioButton { Text = $"{unit.Name} ({unit.Weapon})", AutoSize = true };
            Panel = UiKit.Row(Picture, UiKit.Column(Select, Health));
        }

        public Unit Unit { get; }

        public PictureBox Picture { get; }

        public ProgressBar Health { get; }

        public RadioButton Select { get; }

        public Control Panel { get; }

        public void Refresh()
        {
            UiKit.SetHealth(Health, Unit.Health);
            if (!Unit.IsAlive && Select.Enabled)
            {
                Select.Checked = false;
                Select.Enabled = false;
                Image? old = Picture.Image;
                Picture.Image = Sprites.Grave();
                old?.Dispose();
            }
        }
    }

    private readonly Random _random = new();
    private readonly ListBox _log = UiKit.Log();
    private readonly Label _clock = UiKit.Text();
    private readonly Label _round = UiKit.Text();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly Panel _playerSide = new() { AutoSize = true };
    private readonly Panel _enemySide = new() { AutoSize = true };
    private readonly Button _attack;
    private List<UnitCard> _playerCards = [];
    private List<UnitCard> _enemyCards = [];
    private ArenaBattle _battle = null!;
    private int _seconds;

    public ArenaForm()
    {
        UiKit.SetUpWindow(this, "Weapon arena");
        _attack = UiKit.Button("Attack!", (_, _) => PlayRound(), 140);
        _timer.Tick += (_, _) => _clock.Text = $"Time: {++_seconds} s";

        var logPanel = new Panel { Size = new Size(620, 170) };
        logPanel.Controls.Add(_log);

        Controls.Add(UiKit.Column(
            UiKit.Text("Sword beats axe, axe beats spear, spear beats sword (x2 damage; x0.5 the other way round).", maxWidth: 620),
            UiKit.Row(UiKit.Group("Your squad - choose the attacker", _playerSide), UiKit.Group("Enemies - choose the target", _enemySide)),
            UiKit.Row(_attack, UiKit.Button("New battle", (_, _) => NewBattle(), 140), _round, _clock),
            logPanel));

        FormClosed += (_, _) => _timer.Dispose();
        NewBattle();
    }

    private void NewBattle()
    {
        _battle = ArenaBattle.CreateDefault(_random);
        _playerCards = Build(_playerSide, _battle.PlayerUnits);
        _enemyCards = Build(_enemySide, _battle.EnemyUnits);
        _playerCards[0].Select.Checked = true;
        _enemyCards[0].Select.Checked = true;

        _log.Items.Clear();
        UiKit.Append(_log, [$"Enemy squad: {string.Join(", ", _battle.EnemyUnits.Select(u => $"{u.Name}"))}."]);
        _attack.Enabled = true;
        _seconds = 0;
        _clock.Text = "Time: 0 s";
        _round.Text = "Round 0";
        _timer.Start();
    }

    /// <summary>Replaces the cards of one side of the arena with new ones for <paramref name="units"/>.</summary>
    private static List<UnitCard> Build(Panel side, IEnumerable<Unit> units)
    {
        foreach (Control old in side.Controls.Cast<Control>().ToList())
        {
            old.Dispose();
        }

        side.Controls.Clear();
        List<UnitCard> cards = [.. units.Select(u => new UnitCard(u))];
        FlowLayoutPanel column = UiKit.Column([.. cards.Select(c => c.Panel)]);
        side.Controls.Add(column);

        // Each radio button lives in its own card panel, and WinForms only groups radio buttons that share
        // a parent. Unchecking the others by hand turns the whole side into a single choice again.
        foreach (UnitCard card in cards)
        {
            card.Select.CheckedChanged += (_, _) =>
            {
                if (card.Select.Checked)
                {
                    cards.Where(c => c != card).ToList().ForEach(c => c.Select.Checked = false);
                }
            };
        }

        return cards;
    }

    private void PlayRound()
    {
        UnitCard? attacker = _playerCards.Find(c => c.Select.Checked);
        UnitCard? target = _enemyCards.Find(c => c.Select.Checked);
        if (attacker is null || target is null)
        {
            MessageBox.Show("Choose one of your units and one enemy.", "Choose", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        UiKit.Append(_log, _battle.PlayRound(attacker.Unit, target.Unit).Select(e => e.ToString()));
        _round.Text = $"Round {_battle.Round}";
        _playerCards.ForEach(c => c.Refresh());
        _enemyCards.ForEach(c => c.Refresh());

        // Keep a valid selection on each side after a unit falls.
        SelectFirstAlive(_playerCards);
        SelectFirstAlive(_enemyCards);

        if (_battle.State != ArenaState.InProgress)
        {
            _timer.Stop();
            _attack.Enabled = false;
            bool won = _battle.State == ArenaState.PlayerWon;
            MessageBox.Show(
                $"{(won ? "Victory" : "Defeat")} in {_battle.Round} rounds and {_seconds} seconds.",
                won ? "You win!" : "The enemy wins",
                MessageBoxButtons.OK,
                won ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation);
        }
    }

    private static void SelectFirstAlive(List<UnitCard> cards)
    {
        if (!cards.Exists(c => c.Select.Checked))
        {
            UnitCard? firstAlive = cards.Find(c => c.Unit.IsAlive);
            if (firstAlive is not null)
            {
                firstAlive.Select.Checked = true;
            }
        }
    }
}
