using System.Text;

namespace Guts.Common.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidFileName(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "___";
            var fileNameBuilder = new StringBuilder(input.Trim());
            fileNameBuilder.Replace(' ', '_');
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                fileNameBuilder.Replace(invalidChar, '_');
            }

            return fileNameBuilder.ToString();
        }
    }
}