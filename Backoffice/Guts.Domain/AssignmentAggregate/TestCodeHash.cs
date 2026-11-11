using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain.AssignmentAggregate;

public class TestCodeHash : Entity
{
    public int Id { get; set; }

    [Required]
    public string Hash { get; set; }

    public virtual Assignment Assignment { get; set; }
    public int AssignmentId { get; set; }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}