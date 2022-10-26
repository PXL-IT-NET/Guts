using System;
using System.Text;

namespace Guts.Api.Extensions;

public static class StringExtensions
{
    public static string TryFromBase64(this string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return value;
        }
    }
}