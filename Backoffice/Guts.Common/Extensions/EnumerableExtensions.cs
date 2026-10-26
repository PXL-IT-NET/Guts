using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Guts.Common.Extensions;

public static class EnumerableExtensions
{
    public static T NextRandomItem<T>(this IEnumerable<T> enumerable)
    {
        List<T> list = enumerable.ToList();
        int index = RandomNumberGenerator.GetInt32(0, list.Count);
        return list[index];
    }
}