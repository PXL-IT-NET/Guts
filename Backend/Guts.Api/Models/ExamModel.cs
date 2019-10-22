using System;
using System.Collections.Generic;
using AutoMapper;
using Guts.Business.Dtos;
using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models
{
    public class ExamOutputModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Name { get; set; }
        public int MaximumScore { get; set; }
        public IList<ExamPartOutputModel> Parts { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Exam, ExamOutputModel>();
            }
        }
    }

    public class ExamCreationModel
    {
        public int CourseId { get; set; }
        public string Name { get; set; }
    }


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
}