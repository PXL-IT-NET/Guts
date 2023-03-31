using System;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static T NextRandomItem<T>(this IEnumerable<T> enumerable)
        {
            var random = new Random();
            List<T> list = enumerable.ToList();
            int index = random.Next(0, list.Count);
            return list.ElementAt(index);
        }
    }
}