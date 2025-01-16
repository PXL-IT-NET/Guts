using System;
using Guts.Common.Extensions;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.Tests.Builders
{
    internal class SolutionFileBuilder : BaseBuilder<SolutionFile>
    {
        public SolutionFileBuilder()
        {
            Item = SolutionFile.CreateNew(
                Random.Shared.NextPositive(),
                Random.Shared.NextPositive(),
                $"{Random.Shared.NextString()}.cs",
                Random.Shared.NextString());
        }

        public SolutionFileBuilder WithUser()
        {
            var user = new UserBuilder().WithId().Build();
            SetProperty(sf => sf.User, user);
            SetProperty(sf => sf.UserId, user.Id);
            return this;
        }

        public SolutionFileBuilder WithUserId(int userId)
        {
            SetProperty(sf => sf.UserId, userId);
            return this;
        }

        public SolutionFileBuilder WithAssignmentId(int assignmentId)
        {
            SetProperty(sf => sf.AssignmentId, assignmentId);
            return this;
        }

        public SolutionFileBuilder WithModifiedDateBefore(DateTime date)
        {
            SetProperty(sf => sf.ModifyDateTime, date.AddHours(-1));
            return this;
        }

        public SolutionFileBuilder WithPath(FilePath path)
        {
            SetProperty(sf => sf.FilePath, path);
            return this;
        }

        public SolutionFileBuilder WithContent(string content)
        {
            SetProperty(sf => sf.Content, content);
            return this;
        }
    }
}