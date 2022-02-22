using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Guts.Client.Core.TestTools
{
    public static class FileUtil
    {
        public static string GetFileHash(string filePath)
        {
            var content = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            //remove new lines to rule out differences between files on unix, linux, windows
            content = Regex.Replace(content, "\t|\r|\n|\\s", "");

            var fileBytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = MD5.Create().ComputeHash(fileBytes);
            return BitConverter.ToString(hashBytes);
        }
    }
}