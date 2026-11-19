namespace OopGames.Core.Arena;

public enum Weapon
{
    Sword,
    Axe,
    Spear,
}

public enum Team
{
    Player,
    Enemy,
}

/// <summary>
/// The weapon triangle: sword beats axe, axe beats spear, spear beats sword.
/// Attacking with the advantage doubles the damage; with the disadvantage it is halved.
/// </summary>
public static class WeaponTriangle
{
    private static readonly double[,] Table =
    {
        //            Sword Axe  Spear  <- defender
        /* Sword */ { 1.0, 2.0, 0.5 },
        /* Axe   */ { 0.5, 1.0, 2.0 },
        /* Spear */ { 2.0, 0.5, 1.0 },
    };

    public static double Multiplier(Weapon attacker, Weapon defender) => Table[(int)attacker, (int)defender];

    /// <summary>
    /// Multiplies first and rounds at the end. Casting the multiplier to int before multiplying
    /// would turn 0.5 into 0 (and every disadvantaged hit into no damage at all).
    /// </summary>
    public static int Damage(int baseDamage, Weapon attacker, Weapon defender) =>
        (int)Math.Round(baseDamage * Multiplier(attacker, defender), MidpointRounding.AwayFromZero);
}

public sealed class Unit
{
    public Unit(string name, Weapon weapon, Team team, int health, int damage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(health);
        Name = name;
        Weapon = weapon;
        Team = team;
        MaxHealth = health;
        Health = health;
        Damage = damage;
    }

    public string Name { get; }

    public Weapon Weapon { get; }

    public Team Team { get; }

    public int MaxHealth { get; }

    public int Health { get; private set; }

    public int Damage { get; }

    public bool IsAlive => Health > 0;

    internal int TakeDamage(int amount)
    {
        int lost = Math.Min(amount, Health);
        Health -= lost;
        return lost;
    }
}

public sealed record ArenaEvent(Unit Attacker, Unit Defender, int Damage, double Multiplier, bool Defeated)
{
    public override string ToString()
    {
        string effect = Multiplier > 1 ? " - weapon advantage" : Multiplier < 1 ? " - weapon disadvantage" : string.Empty;
        string ending = Defeated ? $" {Defender.Name} falls." : string.Empty;
        return $"{Attacker.Name} ({Attacker.Weapon}) hits {Defender.Name} ({Defender.Weapon}) for {Damage}{effect}.{ending}";
    }
}

public enum ArenaState
{
    InProgress,
    PlayerWon,
    EnemyWon,
}

/// <summary>
/// Squad against squad. Each round the player chooses which unit attacks which enemy;
/// then one enemy answers, preferring a target it has the weapon advantage against.
/// Every unit is its own object: two squad slots never share the same instance.
/// </summary>
public sealed class ArenaBattle
{
    private readonly Random _random;

    public ArenaBattle(IEnumerable<Unit> playerUnits, IEnumerable<Unit> enemyUnits, Random random)
    {
        PlayerUnits = [.. playerUnits];
        EnemyUnits = [.. enemyUnits];
        if (PlayerUnits.Count == 0 || EnemyUnits.Count == 0)
        {
            throw new ArgumentException("Both squads need at least one unit.");
        }

        if (PlayerUnits.Any(u => u.Team != Team.Player) || EnemyUnits.Any(u => u.Team != Team.Enemy))
        {
            throw new ArgumentException("Every unit must be in the squad of its own team.");
        }

        _random = random;
    }

    public IReadOnlyList<Unit> PlayerUnits { get; }

    public IReadOnlyList<Unit> EnemyUnits { get; }

    public int Round { get; private set; }

    public ArenaState State =>
        EnemyUnits.All(u => !u.IsAlive) ? ArenaState.PlayerWon
        : PlayerUnits.All(u => !u.IsAlive) ? ArenaState.EnemyWon
        : ArenaState.InProgress;

    public static ArenaBattle CreateDefault(Random random)
    {
        Unit[] heroes =
        [
            new("Aldric", Weapon.Sword, Team.Player, 100, 30),
            new("Brenna", Weapon.Axe, Team.Player, 100, 30),
            new("Corin", Weapon.Spear, Team.Player, 100, 30),
        ];

        // A random enemy squad: each slot rolls its own weapon and is a brand-new object.
        Unit[] enemies =
        [
            .. Enumerable.Range(1, 3).Select(i =>
            {
                var weapon = (Weapon)random.Next(3);
                return new Unit($"{weapon} raider {i}", weapon, Team.Enemy, 100, 30);
            }),
        ];

        return new ArenaBattle(heroes, enemies, random);
    }

    /// <summary>Plays one round: the chosen player attack, then the enemy answer if any enemy is left.</summary>
    public IReadOnlyList<ArenaEvent> PlayRound(Unit attacker, Unit target)
    {
        if (State != ArenaState.InProgress)
        {
            throw new InvalidOperationException("The battle is over.");
        }

        if (!PlayerUnits.Contains(attacker) || !attacker.IsAlive)
        {
            throw new ArgumentException("The attacker must be a living unit of your squad.", nameof(attacker));
        }

        if (!EnemyUnits.Contains(target) || !target.IsAlive)
        {
            throw new ArgumentException("The target must be a living enemy.", nameof(target));
        }

        Round++;
        var events = new List<ArenaEvent> { Hit(attacker, target) };
        if (State == ArenaState.InProgress)
        {
            Unit enemy = Pick(EnemyUnits.Where(u => u.IsAlive));
            events.Add(Hit(enemy, ChooseEnemyTarget(enemy)));
        }

        return events;
    }

    /// <summary>The enemy AI: hit a unit it beats in the weapon triangle if there is one, otherwise anyone.</summary>
    internal Unit ChooseEnemyTarget(Unit enemy)
    {
        List<Unit> alive = [.. PlayerUnits.Where(u => u.IsAlive)];
        List<Unit> favourable = [.. alive.Where(u => WeaponTriangle.Multiplier(enemy.Weapon, u.Weapon) > 1)];
        return Pick(favourable.Count > 0 ? favourable : alive);
    }

    private static ArenaEvent Hit(Unit attacker, Unit defender)
    {
        int damage = WeaponTriangle.Damage(attacker.Damage, attacker.Weapon, defender.Weapon);
        defender.TakeDamage(damage);
        return new ArenaEvent(attacker, defender, damage, WeaponTriangle.Multiplier(attacker.Weapon, defender.Weapon), !defender.IsAlive);
    }

    private Unit Pick(IEnumerable<Unit> units)
    {
        List<Unit> list = [.. units];
        return list[_random.Next(list.Count)];
    }
}
