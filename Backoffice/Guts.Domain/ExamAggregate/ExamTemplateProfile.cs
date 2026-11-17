using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamTemplateProfile : ValueObject<ExamTemplateProfile>
{
    public string Key { get; private set; }
    public string DisplayName { get; private set; }
    public bool SupportsWasmExecution { get; private set; }

    private ExamTemplateProfile()
    {
    }

    public ExamTemplateProfile(string key, string displayName, bool supportsWasmExecution)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(key), "Template key cannot be empty.");
        Contracts.Require(!string.IsNullOrWhiteSpace(displayName), "Template display name cannot be empty.");

        Key = key.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        SupportsWasmExecution = supportsWasmExecution;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Key;
    }
}
