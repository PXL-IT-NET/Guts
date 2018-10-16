using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public abstract class CreateTestRunModelBase
    {
        [Required]
        public IEnumerable<TestResultModel> Results { get; set; }

        public string SourceCode { get; set; }
    }
}