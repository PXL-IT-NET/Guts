using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamPartRepository _examPartRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly IPeriodRepository _periodRepository;
        private readonly IExamFactory _examFactory;
        private readonly IUserRepository _userRepository;

        public ExamService(IExamRepository examRepository,
            IExamPartRepository examPartRepository,
            IAssignmentRepository assignmentRepository,
            IAssignmentService assignmentService,
            IPeriodRepository periodRepository,
            IExamFactory examFactory,
            IUserRepository userRepository)
        {
            _examRepository = examRepository;
            _examPartRepository = examPartRepository;
            _assignmentRepository = assignmentRepository;
            _assignmentService = assignmentService;
            _periodRepository = periodRepository;
            _examFactory = examFactory;
            _userRepository = userRepository;
        }

        public async Task<Exam> CreateExamAsync(int courseId, string name)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            var newExam = _examFactory.CreateNew(courseId, currentPeriod.Id, name);
            var savedExam = await _examRepository.AddAsync(newExam);
            return savedExam;
        }

        public async Task<IReadOnlyList<Exam>> GetExamsAsync(int? courseId)
        {
            //TODO: write tests
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _examRepository.FindWithPartsAndEvaluationsAsync(currentPeriod.Id, courseId);
        }

        public async Task<Exam> GetExamAsync(int id)
        {
            return await _examRepository.LoadWithPartsAndEvaluationsAsync(id);
        }

        public async Task<ExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto)
        {
            //TODO: write tests
            Contracts.Require(examPartDto.AssignmentEvaluations.Count > 0,
                "An exam part must have at least one assignment evaluation.");
            var exam = await GetExamAsync(examId);
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

        public async Task<ExamPart> GetExamPartAsync(int examId, int examPartId)
        {
            var examPart = await _examPartRepository.LoadWithAssignmentEvaluationsAsync(examPartId);
            Contracts.Require(examPart.ExamId == examId, "Mismatch between exam id and exam part id.");
            return examPart;
        }

        public async Task DeleteExamPartAsync(int examId, int examPartId)
        {
            var examPartToDelete = await _examPartRepository.GetByIdAsync(examPartId);
            Contracts.Require(examPartToDelete.ExamId == examId, "Mismatch between exam id and exam part id.");
            await _examPartRepository.DeleteAsync(examPartToDelete);
        }

        public async Task<IList<dynamic>> CalculateExamScores(int examId)
        {
            //TODO: write tests
            var exam = await GetExamAsync(examId);
            //Get (and sort) all users
            var allUsers = await _userRepository.GetUsersOfCourseForCurrentPeriodAsync(exam.CourseId);
            allUsers = allUsers.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();

            //preload the assignments
            var assignmentDictionary = new Dictionary<int, Assignment>();
            var assignmentIds = exam.Parts.SelectMany(ep => ep.AssignmentEvaluations.Select(ae => ae.AssignmentId))
                .ToList();
            foreach (var assignmentId in assignmentIds)
            {
                var assignment = await _assignmentRepository.GetSingleWithTestsAndCourseAsync(assignmentId);
                assignmentDictionary.TryAdd(assignmentId, assignment);
            }

            var results = new List<dynamic>();
            foreach (var user in allUsers)
            {
                var result = new ExpandoObject();
                result.TryAdd("LastName", user.LastName);
                result.TryAdd("FirstName", user.FirstName);

                double total = 0.0;
                double totalMaximum = 0.0;

                foreach (var examPart in exam.Parts)
                {
                    var examPartTotalMaximum = examPart.AssignmentEvaluations.Sum(ae => ae.MaximumScore);
                    totalMaximum += examPartTotalMaximum;

                    double examPartTotal = 0.0;
                    foreach (var examPartAssignmentEvaluation in examPart.AssignmentEvaluations)
                    {
                        var assignment = assignmentDictionary[examPartAssignmentEvaluation.AssignmentId];
                        var numberOfTests = assignment.Tests.Count;
                        var resultDto = await _assignmentService.GetResultsForUserAsync(assignment.Id, user.Id, examPart.Deadline);
                        var numberOfPassedTests = resultDto?.TestResults?.Count(r => r.Passed) ?? 0;

                        double scorePerTest = examPartAssignmentEvaluation.MaximumScore /
                                              Convert.ToDouble(numberOfTests - examPartAssignmentEvaluation.NumberOfTestsAlreadyGreenAtStart);

                        double score =
                            Math.Max(numberOfPassedTests - examPartAssignmentEvaluation.NumberOfTestsAlreadyGreenAtStart, 0) *
                            scorePerTest;

                        examPartTotal += score;


                        result.TryAdd($"{assignment.Topic.Code}.{assignment.Code}_NbrPassed({numberOfTests})", numberOfPassedTests);
                        result.TryAdd($"{assignment.Topic.Code}.{assignment.Code}_Score({examPartAssignmentEvaluation.MaximumScore})", score);
                    }
                    result.TryAdd($"Total_{examPart.Name}({examPartTotalMaximum})", examPartTotal);
                    total += examPartTotal;

                }
                result.TryAdd($"Total({totalMaximum})", total);
                result.TryAdd("Total(20)", Math.Round((total / totalMaximum) * 20, MidpointRounding.AwayFromZero));

                results.Add(result);

            }
            return results;
        }
    }
}