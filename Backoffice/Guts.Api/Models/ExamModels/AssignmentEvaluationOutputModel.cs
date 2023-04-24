using AutoMapper;
using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models.ExamModels;

public class AssignmentEvaluationOutputModel
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int MaximumScore { get; set; }
    public int NumberOfTestsAlreadyGreenAtStart { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AssignmentEvaluation, AssignmentEvaluationOutputModel>();
        }
    }
}