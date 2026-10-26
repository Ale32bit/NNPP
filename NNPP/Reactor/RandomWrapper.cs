using System.Security.Cryptography;

namespace NNPP.Reactor;

public static class RandomWrapper
{
    private static readonly Random Rng = new();
    
    public static int Next(int min, int max)
    {
        return min == max ? min : RandomNumberGenerator.GetInt32(min, max);
    }

    public static bool NextBool()
    {
        return Rng.NextDouble() >= 0.5;
    }
}