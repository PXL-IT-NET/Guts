﻿using System.IO;
using System.Linq;
using System.Text;

namespace Guts.Common.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidFilePath(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "___";
            var fileNameBuilder = new StringBuilder(input.Trim());
            fileNameBuilder.Replace(' ', '_');
            foreach (char invalidChar in Path.GetInvalidPathChars())
            {
                fileNameBuilder.Replace(invalidChar, '_');
            }

            return fileNameBuilder.ToString();
        }

        public static bool IsValidFilePath(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            char[] inputChars = input.ToCharArray();


            if (inputChars.Intersect(Path.GetInvalidPathChars()).Any())
            {
                return false;
            }

            return true;
        }
    }
}