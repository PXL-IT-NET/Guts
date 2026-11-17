using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public static class ExamTemplateCatalog
{
    public const string DotNet10Console = "dotnet10-console";
    public const string DotNet10ClassLibNUnit = "dotnet10-classlib-nunit";
    public const string DotNet10ClassLibXUnit = "dotnet10-classlib-xunit";
    public const string DotNet10AspNetCoreWebApi = "dotnet10-aspnetcore-webapi";

    private static readonly IReadOnlyList<ExamTemplateProfile> Profiles =
    [
        new(DotNet10Console, ".NET 10 Console App", supportsWasmExecution: true),
        new(DotNet10ClassLibNUnit, ".NET 10 Class Library (NUnit)", supportsWasmExecution: true),
        new(DotNet10ClassLibXUnit, ".NET 10 Class Library (xUnit)", supportsWasmExecution: true),
        new(DotNet10AspNetCoreWebApi, ".NET 10 ASP.NET Core Web API (Compile & IntelliSense)", supportsWasmExecution: false)
    ];

    public static IReadOnlyList<ExamTemplateProfile> All => Profiles;

    public static bool IsSupported(string templateKey)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            return false;
        }

        var normalized = templateKey.Trim();
        return Profiles.Any(p => string.Equals(p.Key, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static ExamTemplateProfile GetByKey(string templateKey)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(templateKey), "Template key cannot be empty.");

        var normalized = templateKey.Trim();
        var profile = Profiles.FirstOrDefault(p => string.Equals(p.Key, normalized, StringComparison.OrdinalIgnoreCase));
        Contracts.Require(profile != null, $"Unsupported exam template key '{normalized}'.");
        return profile;
    }
}
