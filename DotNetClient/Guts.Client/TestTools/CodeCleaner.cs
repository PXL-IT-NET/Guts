using System.Text.RegularExpressions;

namespace Guts.Client.TestTools
{
    public class CodeCleaner
    {
        public static string StripComments(string code)
        {
            var blockCommentPattern = @"/\*(.*?)\*/";
            var lineCommentPattern = @"//(.*?)(\r?\n|$)";
            var commentRegEx = new Regex($"{blockCommentPattern}|{lineCommentPattern}", RegexOptions.Singleline);
            return commentRegEx.Replace(code, "");
        }
    }
}