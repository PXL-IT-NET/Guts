using System.Linq;
using AutoMapper;
using Guts.Api.Models.ExamModels;
using Guts.Api.Models.PeriodModels;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Mappings
{
    [TestFixture]
    internal class ExamModelMapperTests
    {
        private IMapper _mapper;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(PeriodOutputModel).Assembly);
            });
            _mapper = config.CreateMapper();
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
            var model = _mapper.Map<ExamOutputModel>(exam);

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
    }
}