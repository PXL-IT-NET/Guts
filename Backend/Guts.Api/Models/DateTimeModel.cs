using System;
using AutoMapper;

namespace Guts.Api.Models
{
    public class DateTimeModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<DateTime, DateTimeModel>();
                CreateMap<DateTimeModel, DateTime>().ConstructUsing((model) =>
                    new DateTime(model.Year, model.Month, model.Day, model.Hour, model.Minute, model.Second));
            }
        }
    }
}