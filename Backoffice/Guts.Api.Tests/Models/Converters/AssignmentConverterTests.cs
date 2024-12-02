using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.Converters;
using Guts.Business.Dtos;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class AssignmentConverterTests
    {
        private AssignmentConverter _converter;


        [SetUp]
        public void Setup()
        {
            _converter = new AssignmentConverter();
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTopicOfAssignmentIsNotLoaded()
        {
            //Arrange
            Assignment assignment = new AssignmentBuilder().WithId().WithoutTopicLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfCourseOfTopicOfAssignmentIsNotLoaded()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithoutCourseLoaded().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTestsAreNotLoaded()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).WithoutTestsLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null),
                Throws.ArgumentException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldThrowArgumentExceptionIfTestRunInfoIsNotProvided()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToAssignmentDetailModel(assignment, null, null, null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldAlsoWorkWhenNoTestResultsAreProvided()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            //Act
            AssignmentDetailModel model = _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, null);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.Zero);
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldCorrectlyConvertTestRunInfoFields()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();
            DateTime utcNow = DateTime.UtcNow;

            AssignmentTestRunInfoDto testRunInfo = new AssignmentTestRunInfoDto
            {
                FirstRunDateTime = utcNow.AddDays(-1),
                LastRunDateTime = utcNow,
                NumberOfRuns = Random.Shared.NextPositive()
            };

            //Act
            AssignmentDetailModel model = _converter.ToAssignmentDetailModel(assignment, testRunInfo, null, null);

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
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            int numberOfTests = Random.Shared.Next(2, 10);
            Assignment assignment = new AssignmentBuilder()
                .WithId()
                .WithTopic(chapter)
                .WithRandomTests(numberOfTests)
                .Build();

            List<Test> tests = assignment.Tests.ToList();
            int numberOfTestResults = Random.Shared.Next(1, tests.Count / 2);
            List<TestResult> testResults = new List<TestResult>();
            for (int i = 0; i < numberOfTestResults; i++)
            {
                Test test = tests[i];
                testResults.Add(new TestResultBuilder().WithTest(test).Build());
            }

            //Act
            AssignmentDetailModel model = _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), testResults, null);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.EqualTo(assignment.Tests.Count));
        }

        [Test]
        public void ToAssignmentDetailModel_ShouldHaveASolutionFileModelForEachProvidedSolutionFile()
        {
            // Arrange
            Chapter chapter = new ChapterBuilder().WithCourse().Build();
            Assignment assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();
            List<SolutionFile> solutionFiles = new List<SolutionFile>
            {
                new SolutionFileBuilder().Build(),
                new SolutionFileBuilder().Build()
            };

            // Act
            AssignmentDetailModel model = _converter.ToAssignmentDetailModel(assignment, new AssignmentTestRunInfoDto(), null, solutionFiles);

            // Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.SolutionFiles, Is.Not.Null);
            Assert.That(model.SolutionFiles, Has.Count.EqualTo(solutionFiles.Count));
            for (int i = 0; i < solutionFiles.Count; i++)
            {
                Assert.That(model.SolutionFiles[i].FilePath, Is.EqualTo(solutionFiles[i].FilePath.FullPath));
                Assert.That(model.SolutionFiles[i].Content, Is.EqualTo(solutionFiles[i].Content));
            }
        }
    }
}