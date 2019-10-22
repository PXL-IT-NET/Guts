using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain.PeriodAggregate
{
    public class Period : AggregateRoot
    {
        [Required]
        public string Description { get; set; }

        public DateTime From { get; set; }

        public DateTime Until { get; set; }
    }
}