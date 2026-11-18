using System.Numerics;

namespace OopGames.Core.Basics;

public static class Sequences
{
    /// <summary>The first <paramref name="count"/> Fibonacci numbers, starting 0, 1, 1, 2...</summary>
    public static IEnumerable<BigInteger> Fibonacci(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        BigInteger current = 0;
        BigInteger next = 1;
        for (int i = 0; i < count; i++)
        {
            yield return current;
            (current, next) = (next, current + next);
        }
    }

    /// <summary>
    /// n! with BigInteger, so large inputs do not overflow. 0! is 1 by definition
    /// (returning 0 for it, as a naive base case might, is a common mistake).
    /// </summary>
    public static BigInteger Factorial(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        return n <= 1 ? BigInteger.One : n * Factorial(n - 1);
    }
}
