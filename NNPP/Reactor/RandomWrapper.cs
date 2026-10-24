using System.Security.Cryptography;

namespace NNPP.Reactor;

public class RandomWrapper
{
    public static int Next(int min, int max)
    {
        if (min == max)
        {
            return min;
        }

        return RandomNumberGenerator.GetInt32(min, max);
    }
}