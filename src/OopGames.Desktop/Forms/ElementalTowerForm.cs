using OopGames.Core.ElementalTower;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Forms;

internal sealed class ElementalTowerForm : Form
{
    private readonly Random _random = new();
    private readonly ListBox _log = UiKit.Log();
    private readonly Label _heroStats = UiKit.Text();
    private readonly Label _enemyStats = UiKit.Text(bold: true);
    private readonly Label _progress = UiKit.Text();
    private readonly ProgressBar _heroHealth = UiKit.HealthBar(1);
    private readonly ProgressBar _enemyHealth = UiKit.HealthBar(1);
    private readonly PictureBox _enemyPicture = new() { Size = new Size(Sprites.Size, Sprites.Size) };
    private readonly List<Button> _attackButtons = [];
    private TowerBattle _battle = null!;

    public ElementalTowerForm()
    {
        UiKit.SetUpWindow(this, "Elemental tower");

        FlowLayoutPanel attacks = UiKit.Row();
        foreach (Element element in Enum.GetValues<Element>())
        {
            Button button = UiKit.Button($"{element} attack", (_, _) => Attack(element), 110);
            _attackButtons.Add(button);
            attacks.Controls.Add(button);
        }

        var logPanel = new Panel { Size = new Size(560, 180) };
        logPanel.Controls.Add(_log);

        Controls.Add(UiKit.Column(
            UiKit.Text("Fire beats grass, grass beats water, water beats fire. Normal attacks can be critical hits.", maxWidth: 560),
            UiKit.Row(
                UiKit.Group("Hero", UiKit.Column(_heroHealth, _heroStats)),
                UiKit.Group("Enemy", UiKit.Row(_enemyPicture, UiKit.Column(_enemyStats, _enemyHealth, _progress)))),
            attacks,
            logPanel,
            UiKit.Button("Restart", (_, _) => NewBattle())));

        NewBattle();
    }

    private void NewBattle()
    {
        _battle = TowerBattle.CreateDefault(_random);
        _heroHealth.Maximum = _battle.Hero.MaxHealth;
        _log.Items.Clear();
        UiKit.Append(_log, [$"{_battle.CurrentEnemy.Name} ({_battle.CurrentEnemy.Element}) guards the first floor."]);
        _attackButtons.ForEach(b => b.Enabled = true);
        RefreshView();
    }

    private void Attack(Element element)
    {
        UiKit.Append(_log, _battle.Attack(element));
        RefreshView();

        if (_battle.State != BattleState.InProgress)
        {
            _attackButtons.ForEach(b => b.Enabled = false);
            MessageBox.Show(
                _battle.State == BattleState.Won ? "You cleared the tower!" : "Your hero has fallen.",
                "Battle over",
                MessageBoxButtons.OK,
                _battle.State == BattleState.Won ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation);
        }
    }

    private void RefreshView()
    {
        TowerHero hero = _battle.Hero;
        ElementalEnemy enemy = _battle.CurrentEnemy;

        UiKit.SetHealth(_heroHealth, hero.Health);
        _heroStats.Text = $"{hero.Name}: {hero.Health}/{hero.MaxHealth} HP{Environment.NewLine}Damage {hero.Damage}, critical {hero.CritChancePercent}% x{hero.CritMultiplier}";

        _enemyHealth.Maximum = enemy.MaxHealth;
        UiKit.SetHealth(_enemyHealth, enemy.Health);
        _enemyStats.Text = $"{enemy.Name} ({enemy.Element})  {enemy.Health}/{enemy.MaxHealth} HP";
        _progress.Text = $"Defeated {_battle.DefeatedEnemies} of {_battle.TotalEnemies}";

        Image? old = _enemyPicture.Image;
        _enemyPicture.Image = enemy.IsAlive ? Sprites.ForElement(enemy.Element) : Sprites.Grave();
        old?.Dispose();
    }
}
