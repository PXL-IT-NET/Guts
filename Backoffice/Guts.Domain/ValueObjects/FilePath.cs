using System.Collections.Generic;
using System.IO;
using Guts.Common;
using Guts.Common.Extensions;

namespace Guts.Domain.ValueObjects
{
    public class FilePath : ValueObject<FilePath>
    {
        public string FullPath { get; private set; }

        public string BasePath => Path.GetDirectoryName(FullPath);

        public string BaseName => Path.GetFileNameWithoutExtension(FullPath);

        public string Extension => Path.GetExtension(FullPath);

        public FilePath(string fullPath)
        {
            Contracts.Require(fullPath.IsValidFilePath(), "FullPath is invalid.");
            Contracts.Require(!string.IsNullOrEmpty(Path.GetExtension(fullPath)), "FullPath has no extension.");
            FullPath = fullPath;
        }

        public static implicit operator string(FilePath filePath)
        {
            return filePath.FullPath;
        }

        public static implicit operator FilePath(string fullName)
        {
            return new FilePath(fullName);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FullPath;
        }
    }
}