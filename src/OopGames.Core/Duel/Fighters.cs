namespace OopGames.Core.Duel;

/// <summary>The result of one hit, so the UI can explain what happened.</summary>
public readonly record struct HitReport(int Damage, bool IsCritical, int AbsorbedByShield, int HealthLost);

/// <summary>Something that can deal damage.</summary>
public interface IAttacker
{
    string Name { get; }

    /// <summary>Rolls this turn's damage. Randomness comes from the caller so tests can use a seed.</summary>
    (int Damage, bool IsCritical) RollDamage(Random random);
}

/// <summary>Something that can take damage.</summary>
public interface IDamageable
{
    int Health { get; }

    bool IsAlive { get; }

    /// <summary>Applies the damage and returns how much was absorbed and how much health was lost.</summary>
    (int Absorbed, int HealthLost) TakeDamage(int damage);
}

/// <summary>
/// Shared behaviour for both duelists. Subclasses override only what makes them special:
/// the striker rolls critical hits, the guardian absorbs damage with a shield.
/// </summary>
public abstract class Fighter : IAttacker, IDamageable
{
    protected Fighter(string name, int health, int damage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(health);
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        Name = name;
        MaxHealth = health;
        Health = health;
        Damage = damage;
    }

    public string Name { get; }

    public int MaxHealth { get; }

    public int Health { get; private set; }

    public int Damage { get; }

    public bool IsAlive => Health > 0;

    public virtual (int Damage, bool IsCritical) RollDamage(Random random) => (Damage, false);

    public virtual (int Absorbed, int HealthLost) TakeDamage(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        return (0, LoseHealth(damage));
    }

    /// <summary>Health never goes below 0. Returns the health actually lost.</summary>
    protected int LoseHealth(int amount)
    {
        int lost = Math.Min(amount, Health);
        Health -= lost;
        return lost;
    }
}

/// <summary>Hits with a chance of a critical strike that multiplies the damage.</summary>
public sealed class Striker : Fighter
{
    public Striker(string name, int health, int damage, int critChancePercent, int critMultiplier)
        : base(name, health, damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(critChancePercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(critChancePercent, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(critMultiplier, 1);
        CritChancePercent = critChancePercent;
        CritMultiplier = critMultiplier;
    }

    public int CritChancePercent { get; }

    public int CritMultiplier { get; }

    /// <summary>
    /// random.Next(100) is 0-99, so a 10% chance means a roll below 10.
    /// (Comparing the other way round would turn a 10% chance into 90%.)
    /// </summary>
    public override (int Damage, bool IsCritical) RollDamage(Random random) =>
        random.Next(100) < CritChancePercent ? (Damage * CritMultiplier, true) : (Damage, false);
}

/// <summary>Carries a shield that absorbs damage until it breaks.</summary>
public sealed class Guardian : Fighter
{
    public Guardian(string name, int health, int damage, int shield)
        : base(name, health, damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shield);
        Shield = shield;
    }

    public int Shield { get; private set; }

    /// <summary>The shield takes the hit first; only the damage it cannot absorb reaches health.</summary>
    public override (int Absorbed, int HealthLost) TakeDamage(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        int absorbed = Math.Min(damage, Shield);
        Shield -= absorbed;
        return (absorbed, LoseHealth(damage - absorbed));
    }
}

public static class DuelRules
{
    /// <summary>
    /// Works with any attacker and any target through the interfaces, so the same code covers
    /// a striker hitting a guardian and the other way round.
    /// </summary>
    public static HitReport Attack(IAttacker attacker, IDamageable target, Random random)
    {
        if (!target.IsAlive)
        {
            throw new InvalidOperationException("The target is already defeated.");
        }

        (int damage, bool isCritical) = attacker.RollDamage(random);
        (int absorbed, int healthLost) = target.TakeDamage(damage);
        return new HitReport(damage, isCritical, absorbed, healthLost);
    }
}
