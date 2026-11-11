using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guts.Common;

namespace Guts.Domain.ValueObjects;

public partial class Email : ValueObject<Email>
{
    public const int MaxLength = 256;

    public string Value { get; }

    public Email(string value)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(value), "An email address cannot be empty.");
        string trimmed = value.Trim();
        Contracts.Require(trimmed.Length <= MaxLength, $"An email address cannot contain more than {MaxLength} characters.");
        Contracts.Require(ValidEmailRegex().IsMatch(trimmed), "The email address is invalid.");

        Value = trimmed;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@(?!.*\.\.)[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ValidEmailRegex();

    public static implicit operator string(Email email)
    {
        return email?.Value;
    }

    public static implicit operator Email(string value)
    {
        return value == null ? null : new Email(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public override string ToString()
    {
        return Value;
    }
}
