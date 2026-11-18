using OopGames.Core.Basics;
using OopGames.Core.Duel;

namespace OopGames.Core.Tests;

public class BasicsTests
{
    [Fact]
    public void Characters_ShareTheBaseClass_AndOverrideTheirRole()
    {
        IReadOnlyList<Character> roster = CharacterRoster.CreateSample();

        Assert.Equal(new[] { "Hero", "Companion", "Villain", "Villain" }, roster.Select(c => c.Role));
        Assert.Equal("Morvath (Villain) - 120 HP, currently hostile", roster[2].Describe());
        Assert.Equal("Aria (Hero) - 100 HP", roster[0].Describe());
    }

    [Fact]
    public void Character_RejectsInvalidData()
    {
        Assert.Throws<ArgumentException>(() => new Hero(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hero("Aria", 0));
    }

    [Fact]
    public void DistanceTo_IsEuclidean()
    {
        Assert.Equal(5.0, new Point2D(0, 0).DistanceTo(new Point2D(3, 4)), 10);
        Assert.Equal(0.0, new Point2D(-2, 7).DistanceTo(new Point2D(-2, 7)), 10);
    }

    [Fact]
    public void Squad_CreatesTheRightKindOfMember()
    {
        SquadMember keeper = Squad.Create("Iker", 1, 90, Position.Goalkeeper, goals: 5);
        SquadMember striker = Squad.Create("Lola", 9, 75.5, Position.Forward, goals: 2);

        Assert.IsType<Goalkeeper>(keeper);
        Assert.Equal(0, keeper.Goals); // Goals typed for a goalkeeper are ignored.
        Assert.IsType<OutfieldPlayer>(striker);
        Assert.Equal(2, striker.Goals);
    }

    [Fact]
    public void Squad_RejectsRepeatedShirtNumbers()
    {
        var squad = new Squad();

        Assert.True(squad.TryAdd(Squad.Create("A", 7, 10, Position.Midfielder, 1), out _));
        Assert.False(squad.TryAdd(Squad.Create("B", 7, 10, Position.Defender, 0), out string? error));
        Assert.NotNull(error);
        Assert.Single(squad.Members);
        Assert.Equal(1, squad.TotalGoals);
    }

    [Fact]
    public void OutfieldPlayer_CannotBeAGoalkeeper()
    {
        Assert.Throws<ArgumentException>(() => new OutfieldPlayer("A", 1, 0, Position.Goalkeeper, 0));
    }

    [Fact]
    public void Fibonacci_StartsWithZeroAndOne()
    {
        Assert.Equal(new System.Numerics.BigInteger[] { 0, 1, 1, 2, 3, 5, 8 }, Sequences.Fibonacci(7));
        Assert.Empty(Sequences.Fibonacci(0));
    }

    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "1")]
    [InlineData(5, "120")]
    [InlineData(25, "15511210043330985984000000")]
    public void Factorial_HandlesZeroAndLargeNumbers(int n, string expected)
    {
        Assert.Equal(expected, Sequences.Factorial(n).ToString());
    }
}

public class DuelTests
{
    /// <summary>A Random that always returns the same roll, to test both sides of a probability.</summary>
    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => Math.Min(value, maxValue - 1);
    }

    [Fact]
    public void Striker_CritsWhenTheRollIsBelowTheChance()
    {
        var striker = new Striker("S", 100, 10, critChancePercent: 10, critMultiplier: 3);

        Assert.Equal((30, true), striker.RollDamage(new FixedRandom(9)));
        Assert.Equal((10, false), striker.RollDamage(new FixedRandom(10)));
    }

    [Fact]
    public void Striker_WithZeroChance_NeverCrits()
    {
        var striker = new Striker("S", 100, 10, 0, 5);

        Assert.False(striker.RollDamage(new FixedRandom(0)).IsCritical);
    }

    [Fact]
    public void Guardian_ShieldAbsorbsFirst_ThenHealthTakesTheRest()
    {
        var guardian = new Guardian("G", 100, 10, shield: 25);

        Assert.Equal((10, 0), guardian.TakeDamage(10));
        Assert.Equal(15, guardian.Shield);

        Assert.Equal((15, 5), guardian.TakeDamage(20));
        Assert.Equal(0, guardian.Shield);
        Assert.Equal(95, guardian.Health);
    }

    [Fact]
    public void Health_NeverGoesBelowZero()
    {
        var striker = new Striker("S", 30, 10, 0, 1);

        Assert.Equal((0, 30), striker.TakeDamage(500));
        Assert.False(striker.IsAlive);
    }

    [Fact]
    public void Attack_WorksThroughTheInterfaces()
    {
        IAttacker attacker = new Striker("S", 100, 15, 100, 2);
        IDamageable target = new Guardian("G", 100, 10, shield: 10);

        HitReport report = DuelRules.Attack(attacker, target, new FixedRandom(0));

        Assert.Equal(new HitReport(30, true, 10, 20), report);
        Assert.Equal(80, target.Health);
    }

    [Fact]
    public void Attack_OnADefeatedTarget_Throws()
    {
        var target = new Guardian("G", 10, 10, 0);
        target.TakeDamage(10);

        Assert.Throws<InvalidOperationException>(() => DuelRules.Attack(new Striker("S", 1, 1, 0, 1), target, new Random(1)));
    }
}
