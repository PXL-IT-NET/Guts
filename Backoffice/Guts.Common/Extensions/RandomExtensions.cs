using System;
using System.Linq;

namespace Guts.Common.Extensions
{
    public static class RandomExtensions
    {
        public static int NextPositive(this Random random)
        {
            return random.Next(1, Int32.MaxValue);
        }

        public static int NextZeroOrNegative(this Random random, int minimumValue = -1)
        {
            return -1 * random.Next(0, (-1 * minimumValue) + 1);
        }

        public static bool NextBool(this Random random)
        {
            return random.Next(0, 2) == 0;
        }

        public static string NextString(this Random random)
        {
            return Guid.NewGuid().ToString();
        }

        public static DateTime NextDateTimeInFuture(this Random random)
        {
            return DateTime.Now.AddDays(random.Next(1, 10001));
        }

        public static T NextEnum<T>(this Random random) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");
            return Enum.GetValues(typeof(T)).OfType<T>().NextRandomItem();
        }
    }
}
