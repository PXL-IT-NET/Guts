using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamAccessCode : ValueObject<ExamAccessCode>
{
    public const int MinLength = 4;
    public const int MaxLength = 32;

    public string Value { get; private set; }

    private ExamAccessCode()
    {
    }

    public ExamAccessCode(string value)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(value), "Exam access code cannot be empty.");
        var trimmed = value.Trim();
        Contracts.Require(
            trimmed.Length is >= MinLength and <= MaxLength,
            $"Exam access code must be between {MinLength} and {MaxLength} characters.");

        Value = trimmed;
    }

    public bool Matches(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        byte[] expectedBytes = Encoding.UTF8.GetBytes(Value);
        byte[] actualBytes = Encoding.UTF8.GetBytes(candidate.Trim());
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
