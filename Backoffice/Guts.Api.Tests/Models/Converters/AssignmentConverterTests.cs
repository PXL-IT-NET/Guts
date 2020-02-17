using System;
using System.Collections.Generic;
using Guts.Api.Models.Converters;
using Guts.Business.Dtos;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class AssignmentConverterTests
    {
        private AssignmentConverter _converter;
        private Random _random;


        [SetUp]
        public void Setup()
        {
            _converter = new AssignmentConverter();
            _random = new Random();
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTopicOfAssignmentIsNotLoaded()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().WithoutTopicLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfCourseOfTopicOfAssignmentIsNotLoaded()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithoutCourseLoaded().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTestsAreNotLoaded()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().WithoutTestsLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTestRunInfoIsNotProvided()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, null, null, null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldAlsoWorkWhenNoTestResultsAreProvided()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act
            var model = _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.Zero);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldCorrectlyConvertTestRunInfoFields()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();
            var utcNow = DateTime.UtcNow;

            var testRunInfo = new AssignmentTestRunInfoDto
            {
                FirstRunDateTime = utcNow.AddDays(-1),
                LastRunDateTime = utcNow,
                NumberOfRuns = _random.NextPositive()
            };

            //Act
            var model = _converter.ToAssignmentDetailModel(assignment, testRunInfo, null, null);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.FirstRun, Is.EqualTo(testRunInfo.FirstRunDateTime));
            Assert.That(model.LastRun, Is.EqualTo(testRunInfo.LastRunDateTime));
            Assert.That(model.NumberOfRuns, Is.EqualTo(testRunInfo.NumberOfRuns));
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldHaveATestResultForEachTest()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var numberOfTests = _random.Next(2, 10);
            var assignment = new AssignmentBuilder()
                .WithId()
                .WithTopic(chapter)
                .WithRandomTests(numberOfTests)
                .Build();

            //Act
            var model = _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.EqualTo(assignment.Tests.Count));
        }
    }
}