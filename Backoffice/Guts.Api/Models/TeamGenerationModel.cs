﻿using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public class TeamGenerationModel
    {
        [Required, MinLength(3)]
        public string TeamBaseName { get; set; }

        [Required]
        public int NumberOfTeams { get; set; }
    }
}