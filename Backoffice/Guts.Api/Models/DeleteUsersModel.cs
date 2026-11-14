using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models;

public class DeleteUsersModel
{
    [Required]
    public IReadOnlyCollection<int> UserIds { get; set; } = [];
}

