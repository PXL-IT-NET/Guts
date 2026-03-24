using System;

namespace Guts.Api.Models.PeriodModels
{
    public class PeriodOutputModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime From { get; set; }
        public DateTime Until { get; set; }
        public bool IsActive { get; set; }
    }
}