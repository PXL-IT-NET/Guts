using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamPartRepository _examPartRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IExamFactory _examFactory;

        public ExamService(IExamRepository examRepository,
            IExamPartRepository examPartRepository,
            IAssignmentRepository assignmentRepository,
            IExamFactory examFactory)
        {
            _examRepository = examRepository;
            _examPartRepository = examPartRepository;
            _assignmentRepository = assignmentRepository;
            _examFactory = examFactory;
        }

        public async Task<Exam> CreateExamAsync(int courseId, string name)
        {
            var newExam = _examFactory.CreateNew(courseId, name);
            var savedExam = await _examRepository.AddAsync(newExam);
            return savedExam;
        }

        public async Task<Exam> GetExamAsync(int id)
        {
            return await _examRepository.LoadWithPartsAndEvaluations(id);
        }

        public async Task<ExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto)
        {
            //TODO: write tests
            var exam = await GetExamAsync(examId);
            var examPart = exam.AddExamPart(examPartDto.Name, examPartDto.Deadline);
            foreach (var evaluation in examPartDto.AssignmentEvaluations)
            {
                var assignment = await _assignmentRepository.GetByIdAsync(evaluation.AssignmentId);
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
    }
}