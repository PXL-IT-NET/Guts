using System;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    public class ExamTests
    {
        private Random _random;
        private Exam.Factory _factory;
        private Exam _existingExam;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _random = new Random();
        }

        [SetUp]
        public void SetUp()
        {
            _factory = new Exam.Factory();
            var examId = _random.NextPositive();
            _existingExam = new ExamBuilder().WithId(examId).Build();
        }

        [Test]
        public void Factory_CreateNew_ShouldConstructExamCorrectly()
        {
            var validCourseId = _random.NextPositive();
            var validPeriodId = _random.NextPositive();
            var validName = _random.NextString();
            var exam = _factory.CreateNew(validCourseId, validPeriodId, validName);

            Assert.That(exam, Is.Not.Null);
            Assert.That(exam.Id, Is.EqualTo(0));
            Assert.That(exam.CourseId, Is.EqualTo(validCourseId));
            Assert.That(exam.Name, Is.EqualTo(validName));
            Assert.That(exam.MaximumScore, Is.EqualTo(20));
            Assert.That(exam.Parts, Is.Empty);
        }

        [TestCase(0, 1, "someName")] //invalid course
        [TestCase(-1, 1, "someName")]
        [TestCase(1, 1, null)] //invalid name
        [TestCase(1, 1, "")]
        [TestCase(1, 0, "someName")] //invalid period
        [TestCase(1, -1, "someName")]
        public void Factory_CreateNew_ShouldThrowContractExceptionOnInvalidInput(int courseId, int periodId, string name)
        {
            Assert.That(() => _factory.CreateNew(courseId, periodId, name), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddExamPart_ShouldCorrectlyCreateAndAddAnExamPart()
        {
            var validName = _random.NextString();
            var validDeadline = DateTime.UtcNow.AddDays(1);

            var addedPart = _existingExam.AddExamPart(validName, validDeadline);

            Assert.That(addedPart, Is.Not.Null);
            Assert.That(addedPart, Is.SameAs(_existingExam.Parts.FirstOrDefault()));
            Assert.That(addedPart.ExamId, Is.EqualTo(_existingExam.Id));
            Assert.That(addedPart.Name, Is.EqualTo(validName));
            Assert.That(addedPart.Deadline, Is.EqualTo(validDeadline));
        }
    }
}