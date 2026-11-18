using OopGames.Core.TwentyOne;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Forms;

/// <summary>
/// Hot-seat twenty-one duel. The form only forwards clicks to <see cref="TwentyOneGame"/> and redraws
/// its state; the opponent's cards and trumps stay hidden while it is not their turn.
/// </summary>
internal sealed class TwentyOneForm : Form
{
    private const string Rules =
        "Cards 1 to 11 are dealt once per round. On your turn, draw a card or pass.\n" +
        "The round ends when both players pass in a row or the deck runs out.\n" +
        "Closest to the target (21 by default) without going over wins and hits the other player; " +
        "the shield absorbs damage first. A 1 counts 11 unless that would bust you.\n\n" +
        "Coins: bet them (the winner takes both bets) or buy random trump cards:\n" +
        "  Power up +10 damage for your next win - Shield up +10 shield - Return your last card\n" +
        "  Target 24 / Target 27 for this round - Minus two points for you this round.\n\n" +
        "The first player to lose all their health loses the game.";

    /// <summary>Everything shown for one player.</summary>
    private sealed class PlayerPanel
    {
        public PlayerPanel(string title)
        {
            Health = UiKit.HealthBar(TwentyOneGame.StartingHealth);
            Stats = UiKit.Text();
            Hand = UiKit.Text(bold: true, maxWidth: 260);
            Trumps = UiKit.Text(maxWidth: 260);
            Group = UiKit.Group(title, UiKit.Column(Health, Stats, Hand, Trumps));
        }

        public GroupBox Group { get; }

        public ProgressBar Health { get; }

        public Label Stats { get; }

        public Label Hand { get; }

        public Label Trumps { get; }
    }

    private readonly Random _random = new();
    private readonly PlayerPanel[] _panels = [new("Player 1"), new("Player 2")];
    private readonly Label _table = UiKit.Text(bold: true);
    private readonly Label _turn = UiKit.Text(bold: true);
    private readonly ComboBox _trumpChoice = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly ListBox _log = UiKit.Log();
    private readonly List<Button> _actions = [];
    private TwentyOneGame _game = null!;

    public TwentyOneForm()
    {
        UiKit.SetUpWindow(this, "Twenty-one duel");

        FlowLayoutPanel actions = UiKit.Row(
            ActionButton("Draw card", Draw),
            ActionButton("Pass", PassTurn),
            ActionButton("Bet 1 coin", RaiseBet),
            ActionButton($"Buy trump ({TwentyOneGame.TrumpPrice} coin)", BuyTrump, 140));
        FlowLayoutPanel trumps = UiKit.Row(UiKit.Text("Your trump cards:"), _trumpChoice, ActionButton("Use", UseTrump, 70));

        var logPanel = new Panel { Size = new Size(600, 170) };
        logPanel.Controls.Add(_log);

        Controls.Add(UiKit.Column(
            UiKit.Row(_table, UiKit.Button("Rules", (_, _) => MessageBox.Show(Rules, "How to play"), 80), UiKit.Button("New game", (_, _) => NewGame(), 100)),
            UiKit.Row(_panels[0].Group, _panels[1].Group),
            _turn,
            actions,
            trumps,
            logPanel));

        NewGame();
    }

    private Button ActionButton(string text, Action onClick, int width = 110)
    {
        Button button = UiKit.Button(text, (_, _) => Play(onClick), width);
        _actions.Add(button);
        return button;
    }

    private void NewGame()
    {
        _game = new TwentyOneGame("Player 1", "Player 2", _random);
        _log.Items.Clear();
        UiKit.Append(_log, ["New game. Player 1 starts."]);
        _actions.ForEach(b => b.Enabled = true);
        RefreshView();
    }

    /// <summary>
    /// Runs an action, then reports anything that changed: a finished round, the end of the game,
    /// or a change of turn (so the next player can take the seat before their cards are shown).
    /// </summary>
    private void Play(Action action)
    {
        int roundBefore = _game.Round;
        int playerBefore = _game.ActiveIndex;
        action();

        bool roundEnded = _game.Round != roundBefore || _game.IsGameOver;
        if (roundEnded && _game.LastRound is RoundResult result)
        {
            string summary = Describe(result);
            UiKit.Append(_log, [summary]);
            MessageBox.Show(summary, $"Round {roundBefore} result");
        }

        if (_game.IsGameOver)
        {
            _actions.ForEach(b => b.Enabled = false);
            string winner = _game.GameWinnerIndex is int w ? $"{_game.Players[w].Name} wins the game!" : "Both players fell: it's a draw.";
            UiKit.Append(_log, [winner]);
            RefreshView(revealAll: true);
            MessageBox.Show(winner, "Game over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_game.ActiveIndex != playerBefore || roundEnded)
        {
            // Hide everything until the next player confirms they are in front of the screen.
            RefreshView(hideAll: true);
            MessageBox.Show($"{_game.ActivePlayer.Name}, it's your turn. Press OK when the other player is not looking.", "Change seats");
        }

        RefreshView();
    }

    /// <summary>Drawing passes the turn, so the card is shown to its owner right away, before the seats change.</summary>
    private void Draw()
    {
        TwentyOnePlayer player = _game.ActivePlayer;
        int roundBefore = _game.Round;
        int card = _game.Draw();
        UiKit.Append(_log, [$"{player.Name} draws a card."]);

        string points = _game.Round == roundBefore ? $" You now have {player.PointsFor(_game.Target)} points." : string.Empty;
        MessageBox.Show($"{player.Name}, you drew a {card}.{points}", "Your card");
    }

    private void PassTurn()
    {
        UiKit.Append(_log, [$"{_game.ActivePlayer.Name} passes."]);
        _game.Pass();
    }

    private void RaiseBet()
    {
        if (_game.TryRaiseBet())
        {
            UiKit.Append(_log, [$"{_game.ActivePlayer.Name} raises their bet to {_game.ActivePlayer.Bet}."]);
        }
        else
        {
            MessageBox.Show("You have no coins left.", "Bet");
        }
    }

    private void BuyTrump()
    {
        if (_game.BuyTrump() is TrumpCard card)
        {
            UiKit.Append(_log, [$"{_game.ActivePlayer.Name} buys a trump card."]);
            MessageBox.Show($"You got: {TrumpName(card)}", "Trump card");
        }
        else
        {
            MessageBox.Show("You do not have enough coins.", "Trump card");
        }
    }

    private void UseTrump()
    {
        if (_trumpChoice.SelectedItem is not TrumpItem item)
        {
            MessageBox.Show("You have no trump cards. Buy one first.", "Trump card");
            return;
        }

        if (_game.TryUseTrump(item.Card, out string? error))
        {
            UiKit.Append(_log, [$"{_game.ActivePlayer.Name} plays {TrumpName(item.Card)}."]);
        }
        else
        {
            MessageBox.Show(error, "Trump card");
        }
    }

    private sealed record TrumpItem(TrumpCard Card, int Count)
    {
        public override string ToString() => $"{TrumpName(Card)} x{Count}";
    }

    private void RefreshView(bool hideAll = false, bool revealAll = false)
    {
        _table.Text = $"Round {_game.Round}   Target: {_game.Target}   Cards in the deck: {_game.CardsLeft}   Bank: {_game.BankCoins} coins";
        _turn.Text = hideAll ? "Changing seats..." : $"{_game.ActivePlayer.Name}'s turn";

        for (int i = 0; i < _panels.Length; i++)
        {
            TwentyOnePlayer player = _game.Players[i];
            PlayerPanel panel = _panels[i];
            bool visible = revealAll || (!hideAll && i == _game.ActiveIndex);

            UiKit.SetHealth(panel.Health, player.Health);
            panel.Stats.Text = $"Health {player.Health}  Shield {player.Shield}  Damage {player.Damage}{Environment.NewLine}Coins {player.Coins}  Bet {player.Bet}";
            panel.Hand.Text = visible
                ? $"Cards: {(player.Hand.Count == 0 ? "none" : string.Join(" ", player.Hand))}  ({player.PointsFor(_game.Target)} points)"
                : $"Cards: {player.Hand.Count} hidden";
            int trumpCount = player.Trumps.Values.Sum();
            panel.Trumps.Text = visible
                ? $"Trumps: {(trumpCount == 0 ? "none" : string.Join(", ", player.Trumps.Where(t => t.Value > 0).Select(t => $"{TrumpName(t.Key)} x{t.Value}")))}"
                : $"Trumps: {trumpCount} hidden";
            panel.Group.BackColor = !hideAll && i == _game.ActiveIndex && !_game.IsGameOver ? Color.Honeydew : SystemColors.Control;
        }

        _trumpChoice.Items.Clear();
        if (!hideAll && !_game.IsGameOver)
        {
            foreach ((TrumpCard card, int count) in _game.ActivePlayer.Trumps.Where(t => t.Value > 0))
            {
                _trumpChoice.Items.Add(new TrumpItem(card, count));
            }
        }

        if (_trumpChoice.Items.Count > 0)
        {
            _trumpChoice.SelectedIndex = 0;
        }
    }

    private string Describe(RoundResult result)
    {
        string points = $"{_game.Players[0].Name} {result.PointsPlayerOne} - {result.PointsPlayerTwo} {_game.Players[1].Name} (target {result.Target})";
        if (result.WinnerIndex is not int w)
        {
            return $"{points}. Nobody wins the round; bets are returned.";
        }

        string winner = _game.Players[w].Name;
        string loser = _game.Players[1 - w].Name;
        string pot = result.PotWon > 0 ? $" and takes a pot of {result.PotWon} coins" : string.Empty;
        return $"{points}. {winner} wins the round{pot}; {loser} loses {result.HealthLost} health.";
    }

    private static string TrumpName(TrumpCard card) => card switch
    {
        TrumpCard.PowerUp => "Power up (+10 damage)",
        TrumpCard.ShieldUp => "Shield up (+10 shield)",
        TrumpCard.ReturnLastCard => "Return last card",
        TrumpCard.TargetTwentyFour => "Target 24",
        TrumpCard.TargetTwentySeven => "Target 27",
        TrumpCard.MinusTwo => "Minus two",
        _ => card.ToString(),
    };
}
