using OopGames.Core.Blackjack;

namespace OopGames.Core.Tests;

public class BlackjackTests
{
    private static BlackjackGame NewGame(int seed = 1) => new("Ana", "Ben", new Random(seed));

    [Theory]
    [InlineData(20, 18, 21, 0)]   // closer to the target
    [InlineData(22, 18, 21, 1)]   // player one goes over
    [InlineData(21, 21, 21, null)] // tie
    [InlineData(23, 25, 21, null)] // both over
    [InlineData(23, 20, 24, 0)]   // a higher target changes the result
    public void DecideWinner_FollowsTheRules(int one, int two, int target, int? expected)
    {
        Assert.Equal(expected, BlackjackGame.DecideWinner(one, two, target));
    }

    [Fact]
    public void NewGame_StartsWithAFullDeckAndTheDefaultTarget()
    {
        BlackjackGame game = NewGame();

        Assert.Equal(BlackjackGame.HighestCard, game.CardsLeft);
        Assert.Equal(BlackjackGame.DefaultTarget, game.Target);
        Assert.Equal(0, game.ActiveIndex);
        Assert.All(game.Players, p => Assert.Equal(BlackjackGame.StartingHealth, p.Health));
    }

    [Fact]
    public void Draw_GivesTheCardToTheActivePlayer_AndPassesTheTurn()
    {
        BlackjackGame game = NewGame();

        int card = game.Draw();

        Assert.Equal(new[] { card }, game.Players[0].Hand);
        Assert.Equal(1, game.ActiveIndex);
        Assert.Equal(BlackjackGame.HighestCard - 1, game.CardsLeft);
    }

    [Fact]
    public void ACardIsNeverDealtTwiceInARound()
    {
        BlackjackGame game = NewGame(7);
        var dealt = new List<int>();
        for (int i = 0; i < BlackjackGame.HighestCard - 1; i++)
        {
            dealt.Add(game.Draw());
        }

        Assert.Equal(dealt.Count, dealt.Distinct().Count());
        Assert.Single(Enumerable.Range(1, BlackjackGame.HighestCard).Except(dealt));
    }

    [Fact]
    public void TwoPassesInARow_EndTheRound()
    {
        BlackjackGame game = NewGame();
        game.Draw(); // Ana
        game.Pass(); // Ben
        game.Pass(); // Ana -> round over

        Assert.NotNull(game.LastRound);
        Assert.Equal(2, game.Round);
        Assert.Equal(0, game.LastRound.WinnerIndex); // Ben had no cards: 0 points.
        Assert.Equal(BlackjackGame.StartingHealth - BlackjackGame.BaseDamage, game.Players[1].Health);
        Assert.Equal(BlackjackGame.HighestCard, game.CardsLeft);
        Assert.Equal(1, game.ActiveIndex); // The other player starts the next round.
    }

    [Fact]
    public void Ace_CountsOneWhenElevenWouldGoOver()
    {
        BlackjackPlayer player = NewGame().Players[0];
        player.AddCard(1);
        Assert.Equal(11, player.PointsFor(21));

        player.AddCard(10);
        player.AddCard(5);
        Assert.Equal(16, player.PointsFor(21));
        Assert.Equal(26, player.PointsFor(27));
    }

    [Fact]
    public void ShieldAbsorbsTheHitFirst()
    {
        BlackjackPlayer player = NewGame().Players[0];
        player.AddShield(10);

        Assert.Equal(10, player.TakeHit(20));
        Assert.Equal(0, player.Shield);
        Assert.Equal(90, player.Health);
    }

    [Fact]
    public void Trumps_MustBeBoughtBeforeUse_AndCostCoins()
    {
        BlackjackGame game = NewGame();

        Assert.False(game.TryUseTrump(TrumpCard.PowerUp, out _));
        TrumpCard? bought = game.BuyTrump();

        Assert.NotNull(bought);
        Assert.Equal(BlackjackGame.StartingCoins - BlackjackGame.TrumpPrice, game.Players[0].Coins);
        Assert.Equal(1, game.Players[0].Trumps[bought.Value]);
    }

    [Fact]
    public void TargetTrump_ChangesTheTargetForTheRound()
    {
        BlackjackGame game = NewGame();
        game.Players[0].AddTrump(TrumpCard.TargetTwentySeven);

        Assert.True(game.TryUseTrump(TrumpCard.TargetTwentySeven, out _));
        Assert.Equal(27, game.Target);
        Assert.Equal(0, game.Players[0].Trumps[TrumpCard.TargetTwentySeven]);
    }

    [Fact]
    public void ReturnLastCard_PutsItBackInTheDeck()
    {
        BlackjackGame game = NewGame();
        game.Draw();  // Ana
        game.Draw();  // Ben
        game.Players[0].AddTrump(TrumpCard.ReturnLastCard);

        Assert.True(game.TryUseTrump(TrumpCard.ReturnLastCard, out _));
        Assert.Empty(game.Players[0].Hand);
        Assert.Equal(BlackjackGame.HighestCard - 1, game.CardsLeft);
    }

    [Fact]
    public void ReturnLastCard_WithAnEmptyHand_IsRefusedAndKept()
    {
        BlackjackGame game = NewGame();
        game.Players[0].AddTrump(TrumpCard.ReturnLastCard);

        Assert.False(game.TryUseTrump(TrumpCard.ReturnLastCard, out string? error));
        Assert.NotNull(error);
        Assert.Equal(1, game.Players[0].Trumps[TrumpCard.ReturnLastCard]);
    }

    [Fact]
    public void PowerUp_AddsDamageForOneWinOnly()
    {
        BlackjackGame game = NewGame();
        game.Players[0].AddTrump(TrumpCard.PowerUp);
        game.TryUseTrump(TrumpCard.PowerUp, out _);
        game.Draw(); // Ana gets a card, Ben will have none.
        game.Pass();
        game.Pass();

        Assert.Equal(BlackjackGame.StartingHealth - BlackjackGame.BaseDamage - BlackjackGame.TrumpBonus, game.Players[1].Health);
        Assert.Equal(0, game.Players[0].BonusDamage);
    }

    [Fact]
    public void Bets_GoToTheWinner()
    {
        BlackjackGame game = NewGame();
        game.TryRaiseBet();   // Ana bets 1
        game.Draw();          // Ana
        game.TryRaiseBet();   // Ben bets 1
        game.Pass();          // Ben
        game.Pass();          // Ana -> Ana wins

        RoundResult result = game.LastRound!;
        Assert.Equal(2, result.PotWon);
        Assert.Equal(BlackjackGame.StartingCoins + 1 + 1, game.Players[0].Coins); // +1 net from the pot, +1 from the bank
        Assert.Equal(BlackjackGame.StartingCoins - 1 + 2, game.Players[1].Coins); // lost the bet, +2 consolation
        Assert.Equal(BlackjackGame.StartingBankCoins - 3, game.BankCoins);
    }

    [Fact]
    public void Bets_AreReturnedWhenNobodyWins()
    {
        BlackjackGame game = NewGame();
        game.TryRaiseBet();
        game.Pass(); // Ana
        game.Pass(); // Ben -> 0 vs 0, tie

        Assert.Null(game.LastRound!.WinnerIndex);
        Assert.Equal(BlackjackGame.StartingCoins, game.Players[0].Coins);
    }

    [Fact]
    public void Game_EndsWhenAPlayerRunsOutOfHealth()
    {
        BlackjackGame game = NewGame();
        while (!game.IsGameOver)
        {
            if (game.ActiveIndex == 0 && game.Players[0].Hand.Count == 0)
            {
                game.Draw(); // Only Ana ever draws, so Ben always loses.
            }
            else
            {
                game.Pass();
            }
        }

        Assert.Equal(0, game.GameWinnerIndex);
        Assert.False(game.Players[1].IsAlive);
        Assert.Throws<InvalidOperationException>(() => game.Pass());
    }
}
