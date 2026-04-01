using System;
using System.IO;

namespace Guts.Api.Mcp.Services;

public class AttributeGuideService : IAttributeGuideService
{
    private const string NUnitGuideResourceName = "Guts.Api.Mcp.Resources.Guides.NUnitGuide.md";
    private const string XUnitGuideResourceName = "Guts.Api.Mcp.Resources.Guides.XUnitGuide.md";

    public string GetNUnitGuide()
    {
        return ReadEmbeddedGuide(NUnitGuideResourceName);
    }

    public string GetXUnitGuide()
    {
        return ReadEmbeddedGuide(XUnitGuideResourceName);
    }

    private static string ReadEmbeddedGuide(string resourceName)
    {
        var assembly = typeof(AttributeGuideService).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded guide resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}