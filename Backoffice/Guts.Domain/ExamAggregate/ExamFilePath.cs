using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamFilePath : ValueObject<ExamFilePath>
{
    public const int MaxLength = 260;

    private static readonly string[] DisallowedSegments = ["..", "bin", "obj", ".git"];

    public string Value { get; private set; }

    private ExamFilePath()
    {
    }

    public ExamFilePath(string path)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(path), "Exam file path cannot be empty.");

        var normalized = path.Trim().Replace('\\', '/');
        Contracts.Require(!normalized.StartsWith('/'), "Exam file path must be relative.");

        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        Contracts.Require(!string.IsNullOrEmpty(normalized), "Exam file path cannot be empty.");
        Contracts.Require(normalized.Length <= MaxLength, $"Exam file path cannot exceed {MaxLength} characters.");
        Contracts.Require(!normalized.Contains("//", StringComparison.Ordinal), "Exam file path cannot contain empty segments.");

        var segments = normalized.Split('/');
        foreach (var segment in segments)
        {
            Contracts.Require(!string.IsNullOrWhiteSpace(segment) && segment != ".", "Exam file path contains an invalid segment.");
            Contracts.Require(!IsDisallowedSegment(segment), $"Exam file path cannot contain disallowed segment '{segment}'.");
        }

        Value = normalized;
    }

    private static bool IsDisallowedSegment(string segment)
    {
        return DisallowedSegments.Any(disallowed =>
            string.Equals(segment, disallowed, StringComparison.OrdinalIgnoreCase));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
