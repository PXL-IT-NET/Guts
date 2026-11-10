using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Guts.Common;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Domain.AssignmentAggregate;

public class Assignment : AggregateRoot
{
    private string _description;
    private readonly HashSet<TestCodeHash> _testCodeHashes = [];

    [Required]
    [MaxLength(20)]
    public string Code { get; set; }

    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(_description)) return _description;
            var alternativeDescription = Code;
            return alternativeDescription;
        }
        set => _description = value;
    }

    public virtual ICollection<Test> Tests { get; set; } = new HashSet<Test>();

    public virtual ICollection<TestRun> TestRuns { get; set; } = new HashSet<TestRun>();

    public virtual IReadOnlyCollection<TestCodeHash> TestCodeHashes => _testCodeHashes;

    public virtual Topic Topic { get; set; }
    public int TopicId { get; set; }

    public Assignment()
    {

    }

    public TestCodeHash AddTestCodeHash(string hash, int id = 0)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(hash), "Test code hash cannot be empty.");

        var existing = _testCodeHashes.FirstOrDefault(h => h.Hash == hash);
        if (existing != null)
        {
            return existing;
        }

        var newHash = new TestCodeHash
        {
            Id = id,
            AssignmentId = Id,
            Hash = hash
        };
        _testCodeHashes.Add(newHash);
        return newHash;
    }

    public (bool IsValid, bool HashWasAdded) ValidateOrRegisterTestCodeHash(string testCodeHash, bool isLector)
    {
        if (_testCodeHashes.Count == 0 && string.IsNullOrEmpty(testCodeHash))
        {
            return (IsValid: true, HashWasAdded: false);
        }

        if (_testCodeHashes.Any(ah => ah.Hash == testCodeHash))
        {
            return (IsValid: true, HashWasAdded: false);
        }

        if (isLector && !string.IsNullOrEmpty(testCodeHash))
        {
            AddTestCodeHash(testCodeHash);
            return (IsValid: true, HashWasAdded: true);
        }

        return (IsValid: false, HashWasAdded: false);
    }
}