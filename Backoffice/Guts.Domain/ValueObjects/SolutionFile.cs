using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ValueObjects
{
    public class SolutionFile : ValueObject<SolutionFile>
    {
        public int AssignmentId { get; private set; }

        public virtual User User { get; private set; }
        public int UserId { get; private set; }

        public DateTime ModifyDateTime { get; private set; }

        public FilePath FilePath { get; private set; }

        public string Content { get; private set; }

        private SolutionFile() {}
        public static SolutionFile CreateNew(int assignmentId, int userId, FilePath filePath, string content)
        {
            Contracts.Require(assignmentId > 0, "The assignment Id must be a positive number.");
            Contracts.Require(userId > 0, "The user Id must be a positive number.");
            Contracts.Require(filePath != null, "The filePath cannot be empty.");
            Contracts.Require(filePath!.FullPath.Length <= 255, "The length of the filePath can not be more than 255.");
            Contracts.Require(content != null, "The content cannot be null.");
            var solutionFile = new SolutionFile
            {
                AssignmentId = assignmentId,
                UserId = userId,
                ModifyDateTime = DateTime.UtcNow,
                FilePath = filePath,
                Content = content
            };
            return solutionFile;
        }

        public bool IsNewVersionOf(SolutionFile previousFile)
        {
            if (previousFile == null) return true;
            if (previousFile.AssignmentId != AssignmentId) return false;
            if (previousFile.UserId != UserId) return false;
            if (previousFile.FilePath != FilePath) return false;
            if (previousFile.ModifyDateTime >= ModifyDateTime) return false;
            return previousFile.Content != Content;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AssignmentId;
            yield return UserId;
            yield return FilePath;
            yield return ModifyDateTime;
        }
    }
}