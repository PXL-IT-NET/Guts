using System;
using System.Linq;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        public ChapterContentsModel ToChapterContentsModel(Chapter chapter)
        {
            if (chapter.Exercises == null)
            {
                throw new ArgumentException("Chapter should have exercises loaded", nameof(chapter));
            }

            if (chapter.Exercises.Any(ex => ex.Tests == null))
            {
                throw new ArgumentException("All exercises of the chapter should have their tests loaded", nameof(chapter));
            }

            var model = new ChapterContentsModel
            {
                Exercises = chapter.Exercises.Select(ex => new ExerciseSummaryModel
                {
                    ExerciseId = ex.Id,
                    Number = ex.Number,
                    NumberOfTests = ex.Tests.Count
                }).ToList()
            };
            return model;
        }
    }
}
