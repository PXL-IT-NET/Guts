using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models;
using Guts.Api.Models.ProjectModels;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Mappings;

[TestFixture]
internal class ExamModelMapperTests
{
    private Mapper _mapper;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mapper = new Mapper();
    }

    [Test]
    public void Map_Exam_To_ExamOutputModel()
    {
        //Arrange
        var assignmentEvaluation = new AssignmentEvaluationBuilder()
            .WithId()
            .Build();
        var examPart = new ExamPartBuilder()
            .WithId()
            .WithExamId()
            .WithAssignmentEvaluation(assignmentEvaluation)
            .Build();
        var exam = new ExamBuilder()
            .WithId(examPart.ExamId)
            .WithExamPart(examPart).Build();

        //Act
        var model = _mapper.MapToExamOutputModel(exam);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(exam.Id));
        Assert.That(model.Name, Is.EqualTo(exam.Name));
        Assert.That(model.CourseId, Is.EqualTo(exam.CourseId));
        Assert.That(model.MaximumScore, Is.EqualTo(exam.MaximumScore));
        Assert.That(model.Parts, Is.Not.Null.Or.Empty);

        var examPartModel = model.Parts.Single();
        Assert.That(examPartModel.Id, Is.EqualTo(examPart.Id));
        Assert.That(examPartModel.Name, Is.EqualTo(examPart.Name));
        Assert.That(examPartModel.Deadline, Is.EqualTo(examPart.Deadline));
        Assert.That(examPartModel.AssignmentEvaluations, Is.Not.Null.Or.Empty);

        var assignmentEvaluationModel = examPartModel.AssignmentEvaluations.Single();
        Assert.That(assignmentEvaluationModel.Id, Is.EqualTo(assignmentEvaluation.Id));
        Assert.That(assignmentEvaluationModel.AssignmentId, Is.EqualTo(assignmentEvaluation.AssignmentId));
        Assert.That(assignmentEvaluationModel.MaximumScore, Is.EqualTo(assignmentEvaluation.MaximumScore));
        Assert.That(assignmentEvaluationModel.NumberOfTestsAlreadyGreenAtStart, Is.EqualTo(assignmentEvaluation.NumberOfTestsAlreadyGreenAtStart));
    }

    [Test]
    public void MapToUserModel_ShouldMapIdAndTrimmedFullName()
    {
        //Arrange
        var user = new UserBuilder().WithId().Build();

        //Act
        var model = _mapper.MapToUserModel(user);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(user.Id));
        Assert.That(model.FullName, Is.EqualTo($"{user.FirstName} {user.LastName}".Trim()));
    }

    [Test]
    public void MapToAssignmentModel_WithTests_ShouldMapPropertiesAndTests()
    {
        //Arrange
        var assignment = new AssignmentBuilder().WithId().WithRandomTests(2).Build();

        //Act
        var model = _mapper.MapToAssignmentModel(assignment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.AssignmentId, Is.EqualTo(assignment.Id));
        Assert.That(model.Code, Is.EqualTo(assignment.Code));
        Assert.That(model.Description, Is.EqualTo(assignment.Description));
        Assert.That(model.Tests, Is.Not.Null);
        Assert.That(model.Tests, Has.Count.EqualTo(2));
    }

    [Test]
    public void MapToAssignmentModel_WithNullTests_ShouldMapNullTests()
    {
        //Arrange
        var assignment = new AssignmentBuilder().WithId().WithoutTestsLoaded().Build();

        //Act
        var model = _mapper.MapToAssignmentModel(assignment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.AssignmentId, Is.EqualTo(assignment.Id));
        Assert.That(model.Tests, Is.Null);
    }

    [Test]
    public void MapToTopicAssignmentModel_WithTopicAndTests_ShouldMapAllProperties()
    {
        //Arrange
        var chapter = new ChapterBuilder().WithId().Build();
        var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).WithRandomTests(3).Build();

        //Act
        var model = _mapper.MapToTopicAssignmentModel(assignment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.AssignmentId, Is.EqualTo(assignment.Id));
        Assert.That(model.Code, Is.EqualTo(assignment.Code));
        Assert.That(model.Description, Is.EqualTo(assignment.Description));
        Assert.That(model.TopicCode, Is.EqualTo(chapter.Code.Value));
        Assert.That(model.TopicDescription, Is.EqualTo(chapter.Description));
        Assert.That(model.NumberOfTests, Is.EqualTo(3));
        Assert.That(model.Tests, Has.Count.EqualTo(3));
    }

    [Test]
    public void MapToTopicAssignmentModel_WithoutTopicAndTests_ShouldHandleNulls()
    {
        //Arrange
        var assignment = new AssignmentBuilder().WithId().WithoutTopicLoaded().WithoutTestsLoaded().Build();

        //Act
        var model = _mapper.MapToTopicAssignmentModel(assignment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.TopicCode, Is.Null);
        Assert.That(model.TopicDescription, Is.Null);
        Assert.That(model.NumberOfTests, Is.EqualTo(0));
        Assert.That(model.Tests, Is.Null);
    }

    [Test]
    public void MapToTestModel_ShouldMapIdAndTestName()
    {
        //Arrange
        var test = new TestBuilder().WithId().Build();

        //Act
        var model = _mapper.MapToTestModel(test);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(test.Id));
        Assert.That(model.TestName, Is.EqualTo(test.TestName));
    }

    [Test]
    public void MapToPeriodOutputModel_FromPeriod_ShouldMapAllProperties()
    {
        //Arrange
        var period = new PeriodBuilder().WithId().Build();

        //Act
        var model = _mapper.MapToPeriodOutputModel(period);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(period.Id));
        Assert.That(model.Description, Is.EqualTo(period.Description));
        Assert.That(model.From, Is.EqualTo(period.From));
        Assert.That(model.Until, Is.EqualTo(period.Until));
        Assert.That(model.IsActive, Is.EqualTo(period.IsActive));
    }

    [Test]
    public void MapToPeriodOutputModel_FromExam_ShouldMapId()
    {
        //Arrange
        var exam = new ExamBuilder().WithId(Random.Shared.NextPositive()).Build();

        //Act
        var model = _mapper.MapToPeriodOutputModel(exam);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(exam.Id));
    }

    [Test]
    public void MapToProjectAssessmentModel_ShouldMapAllProperties()
    {
        //Arrange
        var assessment = new ProjectAssessmentMockBuilder().WithId().Build().Object;

        //Act
        var model = _mapper.MapToProjectAssessmentModel(assessment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(assessment.Id));
        Assert.That(model.Description, Is.EqualTo(assessment.Description));
        Assert.That(model.OpenOnUtc, Is.EqualTo(assessment.OpenOnUtc));
        Assert.That(model.DeadlineUtc, Is.EqualTo(assessment.DeadlineUtc));
    }

    [Test]
    public void MapToPeerAssessmentModel_ShouldMapAllProperties()
    {
        //Arrange
        var peerAssessment = new PeerAssessmentBuilder()
            .WithScores(AssessmentScore.AboveAverage, AssessmentScore.WayAboveAverage, AssessmentScore.BelowAverage)
            .Build();

        //Act
        var model = _mapper.MapToPeerAssessmentModel(peerAssessment);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.User.Id, Is.EqualTo(peerAssessment.User.Id));
        Assert.That(model.Subject.Id, Is.EqualTo(peerAssessment.Subject.Id));
        Assert.That(model.ContributionScore, Is.EqualTo(peerAssessment.ContributionScore.Value));
        Assert.That(model.EffortScore, Is.EqualTo(peerAssessment.EffortScore.Value));
        Assert.That(model.CooperationScore, Is.EqualTo(peerAssessment.CooperationScore.Value));
        Assert.That(model.IsSelfAssessment, Is.EqualTo(peerAssessment.IsSelfAssessment));
        Assert.That(model.Explanation, Is.EqualTo(peerAssessment.Explanation));
    }

    [Test]
    public void MapToPeerAssessmentDto_ShouldMapIdsScoresAndExplanation()
    {
        //Arrange
        var model = new PeerAssessmentModel
        {
            User = new UserModel { Id = Random.Shared.NextPositive(), FullName = "User Name" },
            Subject = new UserModel { Id = Random.Shared.NextPositive(), FullName = "Subject Name" },
            ContributionScore = 4,
            CooperationScore = 3,
            EffortScore = 5,
            IsSelfAssessment = false,
            Explanation = Random.Shared.NextString()
        };

        //Act
        var dto = _mapper.MapToPeerAssessmentDto(model);

        //Assert
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.UserId, Is.EqualTo(model.User.Id));
        Assert.That(dto.SubjectId, Is.EqualTo(model.Subject.Id));
        Assert.That(dto.ContributionScore, Is.EqualTo(model.ContributionScore));
        Assert.That(dto.CooperationScore, Is.EqualTo(model.CooperationScore));
        Assert.That(dto.EffortScore, Is.EqualTo(model.EffortScore));
        Assert.That(dto.Explanation, Is.EqualTo(model.Explanation));
    }

    [Test]
    public void MapToAssessmentSubResultModel_ShouldMapAllProperties()
    {
        //Arrange
        var subResultMock = new Mock<IAssessmentSubResult>();
        subResultMock.SetupGet(sr => sr.Value).Returns(3.5);
        subResultMock.SetupGet(sr => sr.SelfValue).Returns(4.0);
        subResultMock.SetupGet(sr => sr.PeerValue).Returns(3.25);
        subResultMock.SetupGet(sr => sr.Score).Returns(AssessmentScore.AboveAverage);
        subResultMock.SetupGet(sr => sr.SelfScore).Returns(AssessmentScore.AboveAverage);
        subResultMock.SetupGet(sr => sr.PeerScore).Returns(AssessmentScore.Average);
        subResultMock.SetupGet(sr => sr.AverageValue).Returns(3.0);
        subResultMock.SetupGet(sr => sr.AverageSelfValue).Returns(3.5);
        subResultMock.SetupGet(sr => sr.AveragePeerValue).Returns(2.8);

        //Act
        var model = _mapper.MapToAssessmentSubResultModel(subResultMock.Object);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Value, Is.EqualTo(3.5));
        Assert.That(model.SelfValue, Is.EqualTo(4.0));
        Assert.That(model.PeerValue, Is.EqualTo(3.25));
        Assert.That(model.Score, Is.EqualTo(AssessmentScore.AboveAverage.Value));
        Assert.That(model.SelfScore, Is.EqualTo(AssessmentScore.AboveAverage.Value));
        Assert.That(model.PeerScore, Is.EqualTo(AssessmentScore.Average.Value));
        Assert.That(model.AverageValue, Is.EqualTo(3.0));
        Assert.That(model.AverageSelfValue, Is.EqualTo(3.5));
        Assert.That(model.AveragePeerValue, Is.EqualTo(2.8));
    }

    [Test]
    public void MapToAssessmentResultModel_ShouldMapSubjectAssessmentsAndSubResults()
    {
        //Arrange
        var subject = new UserBuilder().WithId().Build();
        var selfAssessment = new PeerAssessmentBuilder().WithUserAndSubject(subject, subject).Build();
        var peerAssessment = new PeerAssessmentBuilder().Build();
        var subResultMock = new Mock<IAssessmentSubResult>();
        subResultMock.SetupGet(sr => sr.Score).Returns(AssessmentScore.Average);
        subResultMock.SetupGet(sr => sr.SelfScore).Returns(AssessmentScore.Average);
        subResultMock.SetupGet(sr => sr.PeerScore).Returns(AssessmentScore.Average);

        var resultMock = new Mock<IAssessmentResult>();
        resultMock.SetupGet(r => r.Subject).Returns(subject);
        resultMock.SetupGet(r => r.SelfAssessment).Returns(selfAssessment);
        resultMock.SetupGet(r => r.PeerAssessments).Returns(new List<IPeerAssessment> { peerAssessment });
        resultMock.SetupGet(r => r.AverageResult).Returns(subResultMock.Object);
        resultMock.SetupGet(r => r.EffortResult).Returns(subResultMock.Object);
        resultMock.SetupGet(r => r.CooperationResult).Returns(subResultMock.Object);
        resultMock.SetupGet(r => r.ContributionResult).Returns(subResultMock.Object);

        //Act
        var model = _mapper.MapToAssessmentResultModel(resultMock.Object);

        //Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Subject.Id, Is.EqualTo(subject.Id));
        Assert.That(model.SelfAssessment, Is.Not.Null);
        Assert.That(model.PeerAssessments, Has.Count.EqualTo(1));
        Assert.That(model.AverageResult, Is.Not.Null);
        Assert.That(model.EffortResult, Is.Not.Null);
        Assert.That(model.CooperationResult, Is.Not.Null);
        Assert.That(model.ContributionResult, Is.Not.Null);
    }
}