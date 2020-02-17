using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services.Exam
{
    internal class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamPartRepository _examPartRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IExamFactory _examFactory;
        private readonly IUserRepository _userRepository;
        private readonly IExamTestResultLoader _examTestResultLoader;

        public ExamService(IExamRepository examRepository,
            IExamPartRepository examPartRepository,
            IAssignmentRepository assignmentRepository,
            IPeriodRepository periodRepository,
            IExamFactory examFactory,
            IUserRepository userRepository, 
            IExamTestResultLoader examTestResultLoader)
        {
            _examRepository = examRepository;
            _examPartRepository = examPartRepository;
            _assignmentRepository = assignmentRepository;
            _periodRepository = periodRepository;
            _examFactory = examFactory;
            _userRepository = userRepository;
            _examTestResultLoader = examTestResultLoader;
        }

        public async Task<Domain.ExamAggregate.Exam> CreateExamAsync(int courseId, string name)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            var newExam = _examFactory.CreateNew(courseId, currentPeriod.Id, name);
            var savedExam = await _examRepository.AddAsync(newExam);
            return savedExam;
        }

        public async Task<IExam> GetExamAsync(int id)
        {
            return await _examRepository.LoadDeepAsync(id);
        }

        public async Task<IReadOnlyList<Domain.ExamAggregate.Exam>> GetExamsAsync(int? courseId)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _examRepository.FindWithPartsAndEvaluationsAsync(currentPeriod.Id, courseId);
        }

        public async Task<IExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto)
        {
            Contracts.Require(examPartDto.AssignmentEvaluations.Count > 0,
                "An exam part must have at least one assignment evaluation.");
            IExam exam = await GetExamAsync(examId);
            var examPart = exam.AddExamPart(examPartDto.Name, examPartDto.Deadline);
            foreach (var evaluation in examPartDto.AssignmentEvaluations)
            {
                var assignment = await _assignmentRepository.GetSingleWithTestsAsync(evaluation.AssignmentId);
                examPart.AddAssignmentEvaluation(assignment, evaluation.MaximumScore,
                    evaluation.NumberOfTestsAlreadyGreenAtStart);
            }
            examPart = await _examPartRepository.AddAsync(examPart);
            return examPart;
        }

        public async Task DeleteExamPartAsync(IExam exam, int examPartId)
        {
            exam.DeleteExamPart(examPartId);
            await _examPartRepository.DeleteByIdAsync(examPartId);
        }

        public async Task<IList<ExpandoObject>> CalculateExamScoresForCsvAsync(int examId)
        {
            IExam exam = await GetExamAsync(examId);
            var examResults = await _examTestResultLoader.GetExamResultsAsync(exam);
            var allUsers = await _userRepository.GetUsersOfCourseForCurrentPeriodAsync(exam.CourseId);

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