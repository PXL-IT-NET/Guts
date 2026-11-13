using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models;

public class CourseSaveModel
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; }

    [Required]
    public string Name { get; set; }
}
