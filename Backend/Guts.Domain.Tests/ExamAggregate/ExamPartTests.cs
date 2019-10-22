using System;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    public class ExamPartTests
    {
        private Random _random;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _random = new Random();
        }

        [Test]
        public void Constructor_ShouldConstructValidExamPart()
        {
            var validExamId = _random.NextPositive();
            var validName = _random.NextString();
            var validDeadline = DateTime.UtcNow.AddDays(1);

            var examPart = new ExamPart(validExamId, validName, validDeadline);
            
            Assert.That(examPart, Is.Not.Null);
            Assert.That(examPart.Id, Is.EqualTo(0));
            Assert.That(examPart.ExamId, Is.EqualTo(validExamId));
            Assert.That(examPart.Name, Is.EqualTo(validName));
            Assert.That(examPart.Deadline, Is.EqualTo(validDeadline));
            Assert.That(examPart.AssignmentEvaluations, Is.Empty);
        }

        [TestCase(-1, "some name")]
        [TestCase(1, null)]
        [TestCase(1, "")]
        public void Constructor_ShouldThrowContractExceptionOnInvalidInput(int examId, string name)
        {
            var validDeadline = DateTime.UtcNow.AddDays(1);
            Assert.That(() => new ExamPart(examId, name, validDeadline), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void Constructor_ShouldThrowContractExceptionWhenDeadlineIsNotUtc()
        {
            var deadline = DateTime.UtcNow.AddDays(1);
            var localDeadline = deadline.ToLocalTime();
            Assert.That(() => new ExamPart(1, "some name", localDeadline), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddAssignmentEvaluation_ShouldCorrectlyCreateAndAddAnEvaluation()
        {
            var existingAssignment = new AssignmentBuilder().WithId().WithRandomTests(5).Build();
            var existingExamPart = new ExamPartBuilder().WithId().Build();
            var maximumScore = _random.Next(5, 101);
            var numberOfTestsAlreadyGreenAtStart = _random.Next(1, 5);

            var addedEvaluation = existingExamPart.AddAssignmentEvaluation(existingAssignment, maximumScore,
                numberOfTestsAlreadyGreenAtStart);

            Assert.That(addedEvaluation, Is.Not.Null);
            Assert.That(addedEvaluation, Is.SameAs(existingExamPart.AssignmentEvaluations.FirstOrDefault()));
            Assert.That(addedEvaluation.AssignmentId, Is.EqualTo(existingAssignment.Id));
            Assert.That(addedEvaluation.ExamPartId, Is.EqualTo(existingExamPart.Id));
            Assert.That(addedEvaluation.MaximumScore, Is.EqualTo(maximumScore));
            Assert.That(addedEvaluation.NumberOfTestsAlreadyGreenAtStart, Is.EqualTo(numberOfTestsAlreadyGreenAtStart));
        }
    }
}