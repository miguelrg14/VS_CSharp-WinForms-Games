namespace OopGames.Core.Blackjack;

/// <summary>Single-use cards bought with coins that bend the rules of the current round.</summary>
public enum TrumpCard
{
    /// <summary>+10 damage for the next round you win.</summary>
    PowerUp,

    /// <summary>+10 shield, which absorbs damage before health.</summary>
    ShieldUp,

    /// <summary>Puts your last drawn card back into the deck.</summary>
    ReturnLastCard,

    /// <summary>The target for this round becomes 24.</summary>
    TargetTwentyFour,

    /// <summary>The target for this round becomes 27.</summary>
    TargetTwentySeven,

    /// <summary>Subtracts 2 from your points this round.</summary>
    MinusTwo,
}

public sealed class BlackjackPlayer
{
    private readonly List<int> _hand = [];
    private readonly Dictionary<TrumpCard, int> _trumps = Enum.GetValues<TrumpCard>().ToDictionary(t => t, _ => 0);

    internal BlackjackPlayer(string name, int health, int baseDamage, int coins)
    {
        Name = name;
        Health = health;
        BaseDamage = baseDamage;
        Coins = coins;
    }

    public string Name { get; }

    public int Health { get; private set; }

    public int BaseDamage { get; }

    public int BonusDamage { get; private set; }

    public int Damage => BaseDamage + BonusDamage;

    public int Shield { get; private set; }

    public int Coins { get; private set; }

    public int Bet { get; private set; }

    public IReadOnlyList<int> Hand => _hand;

    /// <summary>Points removed by Minus Two cards this round.</summary>
    public int PointPenalty { get; private set; }

    public IReadOnlyDictionary<TrumpCard, int> Trumps => _trumps;

    public bool IsAlive => Health > 0;

    /// <summary>
    /// The 1 is an ace: it counts 11 unless that would go over the target, then it counts 1.
    /// Computed from the hand every time instead of being stored, so it can never get out of sync.
    /// </summary>
    public int PointsFor(int target)
    {
        int points = _hand.Sum(card => card == 1 ? 11 : card);
        int softAces = _hand.Count(card => card == 1);
        while (points > target && softAces > 0)
        {
            points -= 10;
            softAces--;
        }

        return Math.Max(0, points - PointPenalty);
    }

    internal void AddCard(int card) => _hand.Add(card);

    internal int? RemoveLastCard()
    {
        if (_hand.Count == 0)
        {
            return null;
        }

        int card = _hand[^1];
        _hand.RemoveAt(_hand.Count - 1);
        return card;
    }

    internal void AddTrump(TrumpCard card) => _trumps[card]++;

    internal void ConsumeTrump(TrumpCard card) => _trumps[card]--;

    internal void SpendCoins(int amount) => Coins -= amount;

    internal void ReceiveCoins(int amount) => Coins += amount;

    internal void IncreaseBet()
    {
        Coins--;
        Bet++;
    }

    internal int TakeBet()
    {
        int bet = Bet;
        Bet = 0;
        return bet;
    }

    internal void AddBonusDamage(int amount) => BonusDamage += amount;

    internal void ResetBonusDamage() => BonusDamage = 0;

    internal void AddShield(int amount) => Shield += amount;

    internal void AddPenalty(int amount) => PointPenalty += amount;

    /// <summary>The shield absorbs the hit first; the rest goes to health. Returns the health lost.</summary>
    internal int TakeHit(int damage)
    {
        int absorbed = Math.Min(damage, Shield);
        Shield -= absorbed;
        int lost = Math.Min(damage - absorbed, Health);
        Health -= lost;
        return lost;
    }

    internal void ClearRound()
    {
        _hand.Clear();
        PointPenalty = 0;
    }
}

/// <summary>How a round ended. <see cref="WinnerIndex"/> is null when both bust or tie.</summary>
public sealed record RoundResult(int? WinnerIndex, int PointsPlayerOne, int PointsPlayerTwo, int Target, int HealthLost, int PotWon);

/// <summary>
/// Two-player blackjack duel. Cards 1-11 appear once per round. On your turn you draw or pass; the round ends
/// when both players pass in a row or the deck runs out. The player closest to the target without
/// going over wins the round and hits the other one. Lose all your health and you lose the game.
/// All the rules live here; the form only calls these methods and draws the state.
/// </summary>
public sealed class BlackjackGame
{
    public const int HighestCard = 11;
    public const int DefaultTarget = 21;
    public const int StartingHealth = 100;
    public const int BaseDamage = 20;
    public const int StartingCoins = 5;
    public const int StartingBankCoins = 20;
    public const int TrumpPrice = 1;
    public const int TrumpBonus = 10;

    private readonly Random _random;
    private readonly List<int> _deck = [];
    private readonly BlackjackPlayer[] _players;
    private int _consecutivePasses;
    private int _roundStarter;

    public BlackjackGame(string playerOneName, string playerTwoName, Random random)
    {
        _random = random;
        _players =
        [
            new BlackjackPlayer(playerOneName, StartingHealth, BaseDamage, StartingCoins),
            new BlackjackPlayer(playerTwoName, StartingHealth, BaseDamage, StartingCoins),
        ];
        BankCoins = StartingBankCoins;
        StartRound();
    }

    public IReadOnlyList<BlackjackPlayer> Players => _players;

    public int ActiveIndex { get; private set; }

    public BlackjackPlayer ActivePlayer => _players[ActiveIndex];

    public int CardsLeft => _deck.Count;

    public int Target { get; private set; }

    public int BankCoins { get; private set; }

    public int Round { get; private set; }

    /// <summary>The result of the round that just finished, or null during the first round.</summary>
    public RoundResult? LastRound { get; private set; }

    public bool IsGameOver => _players.Any(p => !p.IsAlive);

    /// <summary>Index of the player still standing, or null while the game goes on (or if both fell).</summary>
    public int? GameWinnerIndex =>
        !IsGameOver ? null : _players[0].IsAlive ? 0 : _players[1].IsAlive ? 1 : null;

    /// <summary>Draws a random card from the deck for the active player and passes the turn.</summary>
    /// <returns>The card drawn.</returns>
    public int Draw()
    {
        EnsurePlaying();
        int index = _random.Next(_deck.Count);
        int card = _deck[index];
        _deck.RemoveAt(index); // Removing the card from the deck makes repeats impossible, no retry loop needed.
        ActivePlayer.AddCard(card);
        _consecutivePasses = 0;

        if (_deck.Count == 0)
        {
            EndRound();
        }
        else
        {
            SwitchTurn();
        }

        return card;
    }

    /// <summary>Stands. If the other player also passed just before, the round ends.</summary>
    public void Pass()
    {
        EnsurePlaying();
        _consecutivePasses++;
        if (_consecutivePasses >= 2)
        {
            EndRound();
        }
        else
        {
            SwitchTurn();
        }
    }

    /// <summary>Moves one coin of the active player into their bet for this round.</summary>
    public bool TryRaiseBet()
    {
        EnsurePlaying();
        if (ActivePlayer.Coins == 0)
        {
            return false;
        }

        ActivePlayer.IncreaseBet();
        return true;
    }

    /// <summary>Spends <see cref="TrumpPrice"/> coins on a random trump card. Returns null if the player cannot pay.</summary>
    public TrumpCard? BuyTrump()
    {
        EnsurePlaying();
        if (ActivePlayer.Coins < TrumpPrice)
        {
            return null;
        }

        ActivePlayer.SpendCoins(TrumpPrice);
        var card = (TrumpCard)_random.Next(Enum.GetValues<TrumpCard>().Length);
        ActivePlayer.AddTrump(card);
        return card;
    }

    /// <summary>Plays a trump card. Using one does not end the turn.</summary>
    public bool TryUseTrump(TrumpCard card, out string? error)
    {
        EnsurePlaying();
        BlackjackPlayer player = ActivePlayer;
        if (player.Trumps[card] == 0)
        {
            error = "You do not have that card.";
            return false;
        }

        switch (card)
        {
            case TrumpCard.PowerUp:
                player.AddBonusDamage(TrumpBonus);
                break;
            case TrumpCard.ShieldUp:
                player.AddShield(TrumpBonus);
                break;
            case TrumpCard.ReturnLastCard:
                if (player.RemoveLastCard() is not int returned)
                {
                    error = "You have no card to return.";
                    return false;
                }

                _deck.Add(returned);
                break;
            case TrumpCard.TargetTwentyFour:
                Target = 24;
                break;
            case TrumpCard.TargetTwentySeven:
                Target = 27;
                break;
            case TrumpCard.MinusTwo:
                player.AddPenalty(2);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(card), card, "Unknown trump card.");
        }

        player.ConsumeTrump(card);
        _consecutivePasses = 0; // Doing something is not passing.
        error = null;
        return true;
    }

    /// <summary>
    /// Closest to the target without going over wins. Both over the target, or the same points: nobody wins.
    /// </summary>
    public static int? DecideWinner(int pointsOne, int pointsTwo, int target)
    {
        bool oneValid = pointsOne <= target;
        bool twoValid = pointsTwo <= target;
        if (oneValid && (!twoValid || pointsOne > pointsTwo))
        {
            return 0;
        }

        if (twoValid && (!oneValid || pointsTwo > pointsOne))
        {
            return 1;
        }

        return null;
    }

    private void EndRound()
    {
        int pointsOne = _players[0].PointsFor(Target);
        int pointsTwo = _players[1].PointsFor(Target);
        int? winnerIndex = DecideWinner(pointsOne, pointsTwo, Target);
        int healthLost = 0;
        int pot = 0;

        if (winnerIndex is int w)
        {
            BlackjackPlayer winner = _players[w];
            BlackjackPlayer loser = _players[1 - w];
            healthLost = loser.TakeHit(winner.Damage);
            winner.ResetBonusDamage(); // Power Up only lasts for one winning round.

            pot = winner.TakeBet() + loser.TakeBet();
            winner.ReceiveCoins(pot);

            // The bank gives a consolation coin pair to the loser and one coin to the winner while it has coins.
            PayFromBank(loser, 2);
            PayFromBank(winner, 1);
        }
        else
        {
            foreach (BlackjackPlayer player in _players)
            {
                player.ReceiveCoins(player.TakeBet()); // Nobody won: bets go back to their owners.
            }
        }

        LastRound = new RoundResult(winnerIndex, pointsOne, pointsTwo, Target, healthLost, pot);
        if (!IsGameOver)
        {
            _roundStarter = 1 - _roundStarter; // Players take turns starting the round.
            StartRound();
        }
    }

    private void StartRound()
    {
        Round++;
        _deck.Clear();
        _deck.AddRange(Enumerable.Range(1, HighestCard));
        foreach (BlackjackPlayer player in _players)
        {
            player.ClearRound();
        }

        Target = DefaultTarget;
        _consecutivePasses = 0;
        ActiveIndex = _roundStarter;
    }

    private void PayFromBank(BlackjackPlayer player, int coins)
    {
        int paid = Math.Min(coins, BankCoins);
        BankCoins -= paid;
        player.ReceiveCoins(paid);
    }

    private void SwitchTurn() => ActiveIndex = 1 - ActiveIndex;

    private void EnsurePlaying()
    {
        if (IsGameOver)
        {
            throw new InvalidOperationException("The game is over.");
        }
    }
}
