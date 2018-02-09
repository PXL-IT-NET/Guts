using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Period : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime From { get; set; }

        public DateTime Until { get; set; }
    }
}