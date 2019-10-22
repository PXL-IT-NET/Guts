using System;

namespace Guts.Common.Extensions
{
    public static class RandomExtensions
    {
        public static int NextPositive(this Random random)
        {
            return random.Next(1, Int32.MaxValue);
        }

        public static bool NextBool(this Random random)
        {
            return random.Next(0, 2) == 0;
        }

        public static string NextString(this Random random)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
