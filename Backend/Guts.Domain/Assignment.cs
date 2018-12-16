using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Assignment : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        public ICollection<Test> Tests { get; set; }

        public ICollection<TestRun> TestRuns { get; set; }

        public ICollection<TestCodeHash> TestCodeHashes { get; set; }

        public Assignment()
        {
            Tests = new HashSet<Test>();
            TestRuns = new HashSet<TestRun>();
            TestCodeHashes = new HashSet<TestCodeHash>();
        }
    }
}