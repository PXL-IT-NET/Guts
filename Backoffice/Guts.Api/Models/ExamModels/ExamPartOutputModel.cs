using System;
using System.Collections.Generic;
using AutoMapper;
using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models.ExamModels;

public class ExamPartOutputModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Deadline { get; set; }

    public IList<AssignmentEvaluationOutputModel> AssignmentEvaluations { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ExamPart, ExamPartOutputModel>();
        }
    }
}