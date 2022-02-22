using System;
using System.Net;
using System.Text;

namespace Guts.Client.Core.Utility
{
    public class HttpResponseException : Exception
    {
        public HttpStatusCode ResponseStatusCode { get; }
        public string RequestUri { get; }

        public HttpResponseException(string requestUri, HttpStatusCode responseStatusCode, string message) : base(message)
        {
            ResponseStatusCode = responseStatusCode;
            RequestUri = requestUri;
        }

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
}