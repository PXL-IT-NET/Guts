using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models.ProjectModels;

public class TeamEditModel
{
    [Required, MinLength(1)]  
    public string Name { get; set; }
}