namespace OopGames.Core.ElementalTower;

public enum Element
{
    Normal,
    Fire,
    Water,
    Grass,
}

/// <summary>Fire beats grass, grass beats water, water beats fire. Normal is neutral.</summary>
public static class ElementChart
{
    public const double Strong = 2.0;
    public const double Weak = 0.5;

    public static double Multiplier(Element attack, Element defender) => (attack, defender) switch
    {
        (Element.Fire, Element.Grass) or (Element.Grass, Element.Water) or (Element.Water, Element.Fire) => Strong,
        (Element.Fire, Element.Water) or (Element.Grass, Element.Fire) or (Element.Water, Element.Grass) => Weak,
        _ => 1.0,
    };

    public static int ApplyTo(int damage, Element attack, Element defender) =>
        (int)Math.Round(damage * Multiplier(attack, defender), MidpointRounding.AwayFromZero);
}

public abstract class Combatant(string name, int health, int damage)
{
    public string Name { get; } = name;

    public int MaxHealth { get; } = health;

    public int Health { get; private set; } = health;

    public int Damage { get; } = damage;

    public bool IsAlive => Health > 0;

    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        int lost = Math.Min(amount, Health);
        Health -= lost;
        return lost;
    }
}

/// <summary>The player knows every element and can roll critical hits with normal attacks.</summary>
public sealed class TowerHero(string name, int health, int damage, int critChancePercent, int critMultiplier)
    : Combatant(name, health, damage)
{
    public int CritChancePercent { get; } = critChancePercent;

    public int CritMultiplier { get; } = critMultiplier;
}

public sealed class ElementalEnemy(string name, Element element, int health, int damage) : Combatant(name, health, damage)
{
    public Element Element { get; } = element;
}

public enum BattleState
{
    InProgress,
    Won,
    Lost,
}

/// <summary>
/// The hero climbs a tower of elemental enemies, one at a time, in queue order.
/// Every action returns a log the UI shows; the class never touches a form.
/// </summary>
public sealed class TowerBattle
{
    private readonly Queue<ElementalEnemy> _waiting;
    private readonly Random _random;

    public TowerBattle(TowerHero hero, IEnumerable<ElementalEnemy> enemies, Random random)
    {
        Hero = hero;
        _random = random;
        _waiting = new Queue<ElementalEnemy>(enemies);
        CurrentEnemy = _waiting.TryDequeue(out ElementalEnemy? first)
            ? first
            : throw new ArgumentException("The tower needs at least one enemy.", nameof(enemies));
        TotalEnemies = _waiting.Count + 1;
    }

    public TowerHero Hero { get; }

    public ElementalEnemy CurrentEnemy { get; private set; }

    public int TotalEnemies { get; }

    public int DefeatedEnemies { get; private set; }

    public BattleState State { get; private set; } = BattleState.InProgress;

    public static TowerBattle CreateDefault(Random random) => new(
        new TowerHero("Hero", 300, 10, critChancePercent: 50, critMultiplier: 2),
        [
            new ElementalEnemy("Ember Imp", Element.Fire, 100, 15),
            new ElementalEnemy("Tide Serpent", Element.Water, 100, 15),
            new ElementalEnemy("Thorn Golem", Element.Grass, 100, 15),
        ],
        random);

    /// <summary>The hero attacks with an element; if the enemy survives it strikes back.</summary>
    public IReadOnlyList<string> Attack(Element element)
    {
        if (State != BattleState.InProgress)
        {
            throw new InvalidOperationException("The battle is over.");
        }

        var log = new List<string>();
        int damage = Hero.Damage;
        if (element == Element.Normal && _random.Next(100) < Hero.CritChancePercent)
        {
            damage *= Hero.CritMultiplier;
            log.Add("Critical hit!");
        }

        damage = ElementChart.ApplyTo(damage, element, CurrentEnemy.Element);
        CurrentEnemy.TakeDamage(damage);
        log.Add($"{Hero.Name} uses a {element.ToString().ToLowerInvariant()} attack: {damage} damage{Effectiveness(element, CurrentEnemy.Element)}.");

        if (!CurrentEnemy.IsAlive)
        {
            DefeatedEnemies++;
            log.Add($"{CurrentEnemy.Name} is defeated!");
            if (_waiting.TryDequeue(out ElementalEnemy? next))
            {
                CurrentEnemy = next;
                log.Add($"{next.Name} ({next.Element}) steps forward.");
            }
            else
            {
                State = BattleState.Won;
                log.Add("Every enemy in the tower is defeated. You win!");
            }

            return log;
        }

        Hero.TakeDamage(CurrentEnemy.Damage);
        log.Add($"{CurrentEnemy.Name} strikes back: {CurrentEnemy.Damage} damage.");
        if (!Hero.IsAlive)
        {
            State = BattleState.Lost;
            log.Add($"{Hero.Name} has fallen. Game over.");
        }

        return log;
    }

    private static string Effectiveness(Element attack, Element defender) => ElementChart.Multiplier(attack, defender) switch
    {
        ElementChart.Strong => " (super effective)",
        ElementChart.Weak => " (not very effective)",
        _ => string.Empty,
    };
}
