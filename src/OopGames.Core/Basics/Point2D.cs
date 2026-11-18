namespace OopGames.Core.Basics;

/// <summary>
/// A point in the plane. A record struct is enough: two points with the same coordinates are equal,
/// and there is no need for subclasses such as "Point1" and "Point2" that add nothing.
/// </summary>
public readonly record struct Point2D(double X, double Y)
{
    /// <summary>Euclidean distance: the square root of dx² + dy².</summary>
    public double DistanceTo(Point2D other) => Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));
}
