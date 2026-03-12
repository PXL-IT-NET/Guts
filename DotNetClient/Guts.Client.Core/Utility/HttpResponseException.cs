using System.Net;
using System.Text;

namespace Guts.Client.Core.Utility;

public class HttpResponseException(string requestUri, HttpStatusCode responseStatusCode, string message)
    : Exception(message)
{
    public HttpStatusCode ResponseStatusCode { get; } = responseStatusCode;
    public string RequestUri { get; } = requestUri;

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Unexpected HTTP response.");
        builder.AppendLine($"Request uri: {RequestUri}.");
        builder.AppendLine($"Response status code: {ResponseStatusCode}.");
        builder.Append(base.ToString());
        return builder.ToString();
    }
}