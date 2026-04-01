using System.ComponentModel;
using Guts.Api.Mcp.Services;
using ModelContextProtocol.Server;

namespace Guts.Api.Mcp.Resources;

[McpServerResourceType]
public class AttributeGuideResources(IAttributeGuideService guideService)
{
    [McpServerResource(Name = "nunit_attributes_guide", UriTemplate = "guts://guides/nunit-attributes", MimeType = "text/markdown")]
    [Description("Reference guide for Guts.Client.NUnit monitored attributes.")]
    public string DescribeNUnitAttributes()
    {
        return guideService.GetNUnitGuide();
    }

    [McpServerResource(Name = "xunit_attributes_guide", UriTemplate = "guts://guides/xunit-attributes", MimeType = "text/markdown")]
    [Description("Reference guide for Guts.Client.XUnit monitored attributes.")]
    public string DescribeXUnitAttributes()
    {
        return guideService.GetXUnitGuide();
    }
}