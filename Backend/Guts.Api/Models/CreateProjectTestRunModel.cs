using System.ComponentModel.DataAnnotations;
using Guts.Business;

namespace Guts.Api.Models
{
    public class CreateProjectTestRunModel : CreateTestRunModelBase
    {
        [Required]
        public ProjectComponentDto ProjectComponent { get; set; }
    }
}