using OopGames.Core.Arena;
using OopGames.Core.ElementalTower;

namespace OopGames.Core.Tests;

public class ElementalTowerTests
{
    [Theory]
    [InlineData(Element.Fire, Element.Grass, 2.0)]
    [InlineData(Element.Grass, Element.Water, 2.0)]
    [InlineData(Element.Water, Element.Fire, 2.0)]
    [InlineData(Element.Fire, Element.Water, 0.5)]
    [InlineData(Element.Normal, Element.Fire, 1.0)]
    [InlineData(Element.Fire, Element.Fire, 1.0)]
    public void Chart_FollowsTheTriangle(Element attack, Element defender, double expected)
    {
        Assert.Equal(expected, ElementChart.Multiplier(attack, defender), 10);
    }

    [Fact]
    public void ApplyTo_RoundsInsteadOfTruncating()
    {
        Assert.Equal(8, ElementChart.ApplyTo(15, Element.Fire, Element.Water)); // 7.5 -> 8
    }

    [Fact]
    public void Battle_GoesThroughTheEnemiesInOrderAndCanBeWon()
    {
        var battle = new TowerBattle(
            new TowerHero("Hero", 1000, 100, 0, 1),
            [new ElementalEnemy("First", Element.Fire, 50, 5), new ElementalEnemy("Second", Element.Grass, 50, 5)],
            new Random(1));

        battle.Attack(Element.Water);
        Assert.Equal("Second", battle.CurrentEnemy.Name);
        Assert.Equal(1, battle.DefeatedEnemies);

        battle.Attack(Element.Fire);
        Assert.Equal(BattleState.Won, battle.State);
        Assert.Throws<InvalidOperationException>(() => battle.Attack(Element.Fire));
    }

    [Fact]
    public void SurvivingEnemy_StrikesBack_AndCanWin()
    {
        var battle = new TowerBattle(
            new TowerHero("Hero", 10, 1, 0, 1),
            [new ElementalEnemy("Brute", Element.Water, 500, 6)],
            new Random(1));

        battle.Attack(Element.Normal);
        Assert.Equal(4, battle.Hero.Health);

        battle.Attack(Element.Normal);
        Assert.Equal(BattleState.Lost, battle.State);
        Assert.Equal(0, battle.Hero.Health);
    }

    [Fact]
    public void DefaultTower_HasThreeEnemiesOfDifferentElements()
    {
        TowerBattle battle = TowerBattle.CreateDefault(new Random(3));

        Assert.Equal(3, battle.TotalEnemies);
        Assert.Equal(Element.Fire, battle.CurrentEnemy.Element);
    }
}

public class ArenaTests
{
    [Theory]
    [InlineData(Weapon.Sword, Weapon.Axe, 2.0)]
    [InlineData(Weapon.Axe, Weapon.Spear, 2.0)]
    [InlineData(Weapon.Spear, Weapon.Sword, 2.0)]
    [InlineData(Weapon.Axe, Weapon.Sword, 0.5)]
    [InlineData(Weapon.Spear, Weapon.Spear, 1.0)]
    public void Triangle_GivesAdvantageAndDisadvantage(Weapon attacker, Weapon defender, double expected)
    {
        Assert.Equal(expected, WeaponTriangle.Multiplier(attacker, defender), 10);
    }

    [Fact]
    public void DisadvantagedHits_StillDealDamage()
    {
        Assert.Equal(15, WeaponTriangle.Damage(30, Weapon.Axe, Weapon.Sword));
        Assert.Equal(60, WeaponTriangle.Damage(30, Weapon.Sword, Weapon.Axe));
    }

    [Fact]
    public void DefaultBattle_CreatesIndependentUnits()
    {
        ArenaBattle battle = ArenaBattle.CreateDefault(new Random(4));

        Assert.Equal(3, battle.EnemyUnits.Count);
        Assert.Equal(6, battle.PlayerUnits.Concat(battle.EnemyUnits).Distinct().Count());
        Assert.All(battle.EnemyUnits, u => Assert.Equal(Team.Enemy, u.Team));
    }

    [Fact]
    public void PlayRound_PlayerHitsTheChosenTarget_ThenAnEnemyAnswers()
    {
        var sword = new Unit("Sword", Weapon.Sword, Team.Player, 100, 30);
        var axeEnemy = new Unit("Axe", Weapon.Axe, Team.Enemy, 100, 30);
        var battle = new ArenaBattle([sword], [axeEnemy], new Random(0));

        IReadOnlyList<ArenaEvent> events = battle.PlayRound(sword, axeEnemy);

        Assert.Equal(2, events.Count);
        Assert.Equal(40, axeEnemy.Health);   // 30 x 2
        Assert.Equal(85, sword.Health);      // 30 x 0.5
        Assert.Equal(1, battle.Round);
    }

    [Fact]
    public void EnemyAi_PrefersTargetsItBeats()
    {
        var sword = new Unit("Sword", Weapon.Sword, Team.Player, 100, 30);
        var spear = new Unit("Spear", Weapon.Spear, Team.Player, 100, 30);
        var axeEnemy = new Unit("Axe", Weapon.Axe, Team.Enemy, 100, 30);
        var battle = new ArenaBattle([sword, spear], [axeEnemy], new Random(0));

        for (int i = 0; i < 20; i++)
        {
            Assert.Same(spear, battle.ChooseEnemyTarget(axeEnemy));
        }
    }

    [Fact]
    public void KillingTheLastEnemy_WinsWithoutACounterattack()
    {
        var sword = new Unit("Sword", Weapon.Sword, Team.Player, 100, 30);
        var weakEnemy = new Unit("Axe", Weapon.Axe, Team.Enemy, 10, 30);
        var battle = new ArenaBattle([sword], [weakEnemy], new Random(0));

        IReadOnlyList<ArenaEvent> events = battle.PlayRound(sword, weakEnemy);

        Assert.Single(events);
        Assert.True(events[0].Defeated);
        Assert.Equal(ArenaState.PlayerWon, battle.State);
        Assert.Equal(100, sword.Health);
        Assert.Equal("Sword (Sword) hits Axe (Axe) for 60 - weapon advantage. Axe falls.", events[0].ToString());
    }

    [Fact]
    public void PlayRound_RejectsInvalidChoices()
    {
        var sword = new Unit("Sword", Weapon.Sword, Team.Player, 100, 30);
        var enemy = new Unit("Axe", Weapon.Axe, Team.Enemy, 100, 30);
        var battle = new ArenaBattle([sword], [enemy], new Random(0));

        Assert.Throws<ArgumentException>(() => battle.PlayRound(enemy, sword));
        Assert.Throws<ArgumentException>(() => new ArenaBattle([enemy], [sword], new Random(0)));
    }
}
