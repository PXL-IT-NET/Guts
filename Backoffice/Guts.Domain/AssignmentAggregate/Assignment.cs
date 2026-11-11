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
    public int Id { get; set; }

    private string _description;
    private readonly HashSet<TestCodeHash> _testCodeHashes = [];
    private HashSet<Test> _tests;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; }

    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(_description) && !IsNumericDefault(_description))
            {
                return _description;
            }

            if (int.TryParse(Code, out int assignmentNumber) && assignmentNumber > 0)
            {
                return $"Assignment {assignmentNumber}";
            }

            return !string.IsNullOrEmpty(_description) ? _description : Code;
        }
        set => _description = value;
    }

    private bool IsNumericDefault(string description)
    {
        return int.TryParse(Code, out int assignmentNumber) && description == assignmentNumber.ToString();
    }

    public virtual IReadOnlyCollection<Test> Tests
    {
        get => _tests;
        init => _tests = value != null ? [.. value] : null;
    }

    public virtual ICollection<TestRun> TestRuns { get; set; } = new HashSet<TestRun>();

    public virtual IReadOnlyCollection<TestCodeHash> TestCodeHashes => _testCodeHashes;

    public virtual Topic Topic { get; set; }
    public int TopicId { get; set; }

    public Test AddTest(string testName, int id = 0)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(testName), "Test name cannot be empty.");
        _tests ??= [];

        var trimmedName = testName.Trim();
        var existing = _tests.FirstOrDefault(t => t.TestName == trimmedName);
        if (existing != null)
        {
            return existing;
        }

        var newTest = new Test
        {
            Id = id,
            AssignmentId = Id,
            TestName = trimmedName
        };
        _tests.Add(newTest);
        return newTest;
    }

    public void AddTest(Test test)
    {
        Contracts.Require(test != null, "Test cannot be null.");
        Contracts.Require(!string.IsNullOrWhiteSpace(test.TestName), "Test name cannot be empty.");
        _tests ??= [];

        var trimmedName = test.TestName.Trim();
        if (_tests.All(t => t.TestName != trimmedName))
        {
            test.AssignmentId = Id;
            _tests.Add(test);
        }
    }

    public void LoadTests(IEnumerable<Test> tests)
    {
        Contracts.Require(tests != null, "Tests collection cannot be null.");
        _tests = [.. tests];
    }

    public void ClearTests()
    {
        _tests = null;
    }

    public IReadOnlyList<Test> AddMissingTests(IEnumerable<string> testNames)
    {
        Contracts.Require(testNames != null, "Test names cannot be null.");
        _tests ??= [];

        var newlyCreatedTests = new List<Test>();
        foreach (var testName in testNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var trimmedName = testName.Trim();
            if (_tests.All(t => t.TestName != trimmedName))
            {
                var newTest = AddTest(trimmedName);
                newlyCreatedTests.Add(newTest);
            }
        }
        return newlyCreatedTests;
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

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}