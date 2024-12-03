using System;
using System.Collections.Generic;
using AutoMapper;
using Guts.Api.Models.ExamModels;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Api.Models.PeriodModels
{
    public class PeriodOutputModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime From { get; set; }
        public DateTime Until { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<IPeriod, PeriodOutputModel>();
            }
        }
    }
}