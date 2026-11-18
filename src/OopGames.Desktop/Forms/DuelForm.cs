using OopGames.Core.Duel;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Forms;

/// <summary>A striker (critical hits) against a guardian (shield). Both attack through IAttacker / IDamageable.</summary>
internal sealed class DuelForm : Form
{
    private readonly Random _random = new();
    private readonly ListBox _log = UiKit.Log();
    private readonly Label _strikerStats = UiKit.Text();
    private readonly Label _guardianStats = UiKit.Text();
    private readonly ProgressBar _strikerHealth;
    private readonly ProgressBar _guardianHealth;
    private readonly Button _strikerAttack;
    private readonly Button _guardianAttack;
    private Striker _striker = null!;
    private Guardian _guardian = null!;

    public DuelForm()
    {
        UiKit.SetUpWindow(this, "Duel");

        _strikerHealth = UiKit.HealthBar(1);
        _guardianHealth = UiKit.HealthBar(1);
        _strikerAttack = UiKit.Button("Striker attacks", (_, _) => Attack(_striker, _guardian), 150);
        _guardianAttack = UiKit.Button("Guardian attacks", (_, _) => Attack(_guardian, _striker), 150);

        var logPanel = new Panel { Size = new Size(560, 180) };
        logPanel.Controls.Add(_log);

        Controls.Add(UiKit.Column(
            UiKit.Row(
                UiKit.Group("Striker - critical hits", UiKit.Column(_strikerHealth, _strikerStats, _strikerAttack)),
                UiKit.Group("Guardian - shield", UiKit.Column(_guardianHealth, _guardianStats, _guardianAttack))),
            logPanel,
            UiKit.Button("New duel", (_, _) => NewDuel())));

        NewDuel();
    }

    private void NewDuel()
    {
        _striker = new Striker("Kael", health: 120, damage: 10, critChancePercent: 30, critMultiplier: 3);
        _guardian = new Guardian("Brom", health: 150, damage: 12, shield: 40);
        _strikerHealth.Maximum = _striker.MaxHealth;
        _guardianHealth.Maximum = _guardian.MaxHealth;
        _log.Items.Clear();
        UiKit.Append(_log, ["A new duel begins."]);
        SetButtons(true);
        RefreshView();
    }

    private void Attack(Fighter attacker, Fighter target)
    {
        HitReport hit = DuelRules.Attack(attacker, target, _random);
        string critical = hit.IsCritical ? " Critical hit!" : string.Empty;
        string shield = hit.AbsorbedByShield > 0 ? $" The shield absorbs {hit.AbsorbedByShield}." : string.Empty;
        UiKit.Append(_log, [$"{attacker.Name} deals {hit.Damage}.{critical}{shield} {target.Name} loses {hit.HealthLost} HP."]);

        if (!target.IsAlive)
        {
            UiKit.Append(_log, [$"{target.Name} is defeated. {attacker.Name} wins the duel!"]);
            SetButtons(false);
        }

        RefreshView();
    }

    private void RefreshView()
    {
        Striker striker = _striker;
        Guardian guardian = _guardian;
        UiKit.SetHealth(_strikerHealth, striker.Health);
        UiKit.SetHealth(_guardianHealth, guardian.Health);
        _strikerStats.Text = $"{striker.Name}: {striker.Health}/{striker.MaxHealth} HP{Environment.NewLine}Damage {striker.Damage}{Environment.NewLine}Critical: {striker.CritChancePercent}% x{striker.CritMultiplier}";
        _guardianStats.Text = $"{guardian.Name}: {guardian.Health}/{guardian.MaxHealth} HP{Environment.NewLine}Damage {guardian.Damage}{Environment.NewLine}Shield {guardian.Shield}";
    }

    private void SetButtons(bool enabled)
    {
        _strikerAttack.Enabled = enabled;
        _guardianAttack.Enabled = enabled;
    }
}
