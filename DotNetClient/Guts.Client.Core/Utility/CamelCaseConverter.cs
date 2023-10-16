using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace Guts.Client.Core.Utility
{
    internal class CamelCaseConverter
    {
        public string ToNormalSentence(string camelCaseSentence)
        {
            if (string.IsNullOrEmpty(camelCaseSentence))
                return camelCaseSentence;

            string[] subSentences = camelCaseSentence.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (subSentences.Length > 1)
            {
                return string.Join(" - ", subSentences.Select(ToNormalSentence));
            }
            else
            {
                // Use a regular expression to split the words in the camelCase string
                string spacedSentence = Regex.Replace(camelCaseSentence, @"(\B[A-Z])", " $1");

                // Lowercase the entire string
                spacedSentence = spacedSentence.ToLower();

                // Capitalize the first letter
                return char.ToUpper(spacedSentence[0]) + spacedSentence.Substring(1);
            }
        }
    }
}