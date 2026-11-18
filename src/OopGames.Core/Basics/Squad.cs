namespace OopGames.Core.Basics;

public enum Position
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward,
}

/// <summary>
/// Data shared by every squad member. Goalkeepers and outfield players derive from it;
/// an outfield player is not a kind of goalkeeper, so neither inherits from the other.
/// </summary>
public abstract class SquadMember
{
    protected SquadMember(string name, int shirtNumber, double minutesPlayed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(shirtNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shirtNumber, 99);
        ArgumentOutOfRangeException.ThrowIfNegative(minutesPlayed);

        Name = name.Trim();
        ShirtNumber = shirtNumber;
        MinutesPlayed = minutesPlayed;
    }

    public string Name { get; }

    public int ShirtNumber { get; }

    public double MinutesPlayed { get; }

    public abstract Position Position { get; }

    /// <summary>Goals scored. Goalkeepers report 0 instead of having a field that makes no sense for them.</summary>
    public virtual int Goals => 0;
}

public sealed class Goalkeeper(string name, int shirtNumber, double minutesPlayed)
    : SquadMember(name, shirtNumber, minutesPlayed)
{
    public override Position Position => Position.Goalkeeper;
}

public sealed class OutfieldPlayer : SquadMember
{
    public OutfieldPlayer(string name, int shirtNumber, double minutesPlayed, Position position, int goals)
        : base(name, shirtNumber, minutesPlayed)
    {
        if (position == Position.Goalkeeper)
        {
            throw new ArgumentException("Use the Goalkeeper class for goalkeepers.", nameof(position));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(goals);
        Position = position;
        Goals = goals;
    }

    public override Position Position { get; }

    public override int Goals { get; }
}

/// <summary>A team sheet that refuses two players with the same shirt number.</summary>
public sealed class Squad
{
    private readonly List<SquadMember> _members = [];

    public IReadOnlyList<SquadMember> Members => _members;

    public int TotalGoals => _members.Sum(m => m.Goals);

    public bool TryAdd(SquadMember member, out string? error)
    {
        if (_members.Exists(m => m.ShirtNumber == member.ShirtNumber))
        {
            error = $"Shirt number {member.ShirtNumber} is already taken.";
            return false;
        }

        _members.Add(member);
        error = null;
        return true;
    }

    public static SquadMember Create(string name, int shirtNumber, double minutesPlayed, Position position, int goals) =>
        position == Position.Goalkeeper
            ? new Goalkeeper(name, shirtNumber, minutesPlayed)
            : new OutfieldPlayer(name, shirtNumber, minutesPlayed, position, goals);
}
