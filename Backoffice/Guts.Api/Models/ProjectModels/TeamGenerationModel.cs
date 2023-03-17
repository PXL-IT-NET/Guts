using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models.ProjectModels
{
    public class TeamGenerationModel
    {
        [Required, MinLength(3)]
        public string TeamBaseName { get; set; }

        [Required, Range(1, 1000)]
        public int TeamNumberFrom { get; set; }

        [Required, Range(1, 1000)]
        public int TeamNumberTo { get; set; }
    }
}