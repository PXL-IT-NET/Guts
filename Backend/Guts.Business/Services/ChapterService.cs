using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Converters;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestResultConverter _testResultConverter;

        public ChapterService(IChapterRepository chapterRepository, 
            ICourseRepository courseRepository, 
            IPeriodRepository periodRepository,
            ITestResultRepository testResultRepository,
            ITestResultConverter testResultConverter)
        {
            _chapterRepository = chapterRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _testResultRepository = testResultRepository;
            _testResultConverter = testResultConverter;
        }

        public async Task<Chapter> GetOrCreateChapterAsync(string courseCode, int chapterNumber)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            Chapter chapter;
            try
            {
                chapter = await _chapterRepository.GetSingleAsync(courseCode, chapterNumber, currentPeriod.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await _courseRepository.GetSingleAsync(courseCode);
                chapter = new Chapter
                {
                    Number = chapterNumber,
                    CourseId = course.Id,
                    PeriodId = currentPeriod.Id
                };
                chapter = await _chapterRepository.AddAsync(chapter);
            }

            return chapter;
        }

        public async Task<Chapter> LoadChapterAsync(int courseId, int chapterNumber)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithExercisesAsync(courseId, chapterNumber, currentPeriod.Id);

            return chapter;
        }

        public async Task<Chapter> LoadChapterWithTestsAsync(int courseId, int chapterNumber)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithExercisesAndTestsAsync(courseId, chapterNumber, currentPeriod.Id);

            return chapter;
        }

        public async Task<IList<ExerciseResultDto>> GetResultsForUserAsync(int chapterId, int userId)
        {
            var lastTestResults = await _testResultRepository.GetLastTestResultsOfChapterAsync(chapterId, userId);

            return _testResultConverter.ToExerciseResultDto(lastTestResults);
        }

        public async Task<IList<ExerciseResultDto>> GetAverageResultsAsync(int chapterId)
        {
            var lastTestResults = await _testResultRepository.GetLastTestResultsOfChapterAsync(chapterId, null);

            return _testResultConverter.ToExerciseResultDto(lastTestResults);
        }

        public async Task<IList<Chapter>> GetChaptersOfCourseAsync(int courseId)
        {
            Period currentPeriod;
            try
            {
                currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            }
            catch (DataNotFoundException)
            {
                return new List<Chapter>();
            }

            var chapters = await _chapterRepository.GetByCourseIdAsync(courseId, currentPeriod.Id);
            return chapters.OrderBy(chapter => chapter.Number).ToList();
        }
    }
}