using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamMetadata : ValueObject<ExamMetadata>
{
    public const int MaxTitleLength = 200;

    public string Title { get; private set; }
    public string Description { get; private set; }
    public ExamAccessCode AccessCode { get; private set; }
    public string TemplateKey { get; private set; }

    private ExamMetadata()
    {
    }

    public ExamMetadata(string title, string description, string accessCode, string templateKey)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(title), "Exam title cannot be empty.");
        var trimmedTitle = title.Trim();
        Contracts.Require(trimmedTitle.Length <= MaxTitleLength, $"Exam title cannot exceed {MaxTitleLength} characters.");
        Contracts.Require(ExamTemplateCatalog.IsSupported(templateKey), $"Unsupported template key '{templateKey}'.");

        Title = trimmedTitle;
        Description = description ?? string.Empty;
        AccessCode = new ExamAccessCode(accessCode);
        TemplateKey = ExamTemplateCatalog.GetByKey(templateKey).Key;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Title;
        yield return Description;
        yield return AccessCode;
        yield return TemplateKey;
    }
}
