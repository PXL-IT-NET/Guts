using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Business.Services.Exam
{
    internal class ExamService(
        IExamRepository examRepository,
        IExamPartRepository examPartRepository,
        IAssignmentRepository assignmentRepository,
        IPeriodRepository periodRepository,
        IExamFactory examFactory,
        IUserRepository userRepository,
        IExamTestResultLoader examTestResultLoader)
        : IExamService
    {
        public async Task<IReadOnlyList<IExam>> GetExamsAsync(int courseId, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);
            return await examRepository.FindWithPartsAndEvaluationsAsync(courseId, period.Id);
        }

        public async Task<IExam> CreateExamAsync(int courseId, string name)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(null);
            IExam newExam = examFactory.CreateNew(courseId, period.Id, name);
            IExam savedExam = await examRepository.AddAsync(newExam);
            return savedExam;
        }

        public async Task<IExam> GetExamAsync(int id)
        {
            return await examRepository.LoadDeepAsync(id);
        }

        public async Task<IExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto)
        {
            Contracts.Require(examPartDto.AssignmentEvaluations.Count > 0,
                "An exam part must have at least one assignment evaluation.");
            IExam exam = await GetExamAsync(examId);
            IExamPart examPart = exam.AddExamPart(examPartDto.Name, examPartDto.Deadline);
            foreach (var evaluation in examPartDto.AssignmentEvaluations)
            {
                var assignment = await assignmentRepository.GetSingleWithTestsAsync(evaluation.AssignmentId);
                examPart.AddAssignmentEvaluation(assignment, evaluation.MaximumScore,
                    evaluation.NumberOfTestsAlreadyGreenAtStart);
            }
            examPart = await examPartRepository.AddAsync(examPart);
            return examPart;
        }

        public async Task DeleteExamPartAsync(IExam exam, int examPartId)
        {
            exam.DeleteExamPart(examPartId);
            await examPartRepository.DeleteByIdAsync(examPartId);
        }

        public async Task<IList<ExpandoObject>> CalculateExamScoresForCsvAsync(int examId)
        {
            IExam exam = await GetExamAsync(examId);
            var examResults = await examTestResultLoader.GetExamResultsAsync(exam);
            var allUsers = await userRepository.GetUsersOfCourseForCurrentPeriodAsync(exam.CourseId);

            var scores = new List<IExamScore>();
            foreach (var user in allUsers)
            {
                var score = exam.CalculateScoreForUser(user, examResults);
                scores.Add(score);
            }

            return scores.Select(score => score.ToCsvRecord()).ToList();
        }
    }
}