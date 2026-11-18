namespace OopGames.Core.Basics;

/// <summary>
/// The first inheritance exercise: an abstract base class with the shared data, and subclasses
/// that only say what makes them different.
/// </summary>
public abstract class Character(string name, int health)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("A character needs a name.", nameof(name))
        : name;

    public int Health { get; } = health > 0
        ? health
        : throw new ArgumentOutOfRangeException(nameof(health), health, "Health must be positive.");

    /// <summary>Each subclass must describe its role: that is what makes the property abstract.</summary>
    public abstract string Role { get; }

    /// <summary>Virtual: subclasses may add details, but they do not have to.</summary>
    public virtual string Describe() => $"{Name} ({Role}) - {Health} HP";

    public override string ToString() => Name;
}

public sealed class Hero(string name, int health = 100) : Character(name, health)
{
    public override string Role => "Hero";
}

public sealed class Companion(string name, int health = 60) : Character(name, health)
{
    public override string Role => "Companion";
}

public enum EnemyState
{
    Idle,
    Patrol,
    Hostile,
    Defensive,
}

public sealed class Villain(string name, int health = 120, EnemyState state = EnemyState.Patrol) : Character(name, health)
{
    public EnemyState State { get; } = state;

    public override string Role => "Villain";

    public override string Describe() => $"{base.Describe()}, currently {State.ToString().ToLowerInvariant()}";
}

public static class CharacterRoster
{
    public static IReadOnlyList<Character> CreateSample() =>
    [
        new Hero("Aria"),
        new Companion("Bram"),
        new Villain("Morvath", state: EnemyState.Hostile),
        new Villain("Grub the Scout", 40, EnemyState.Idle),
    ];
}
