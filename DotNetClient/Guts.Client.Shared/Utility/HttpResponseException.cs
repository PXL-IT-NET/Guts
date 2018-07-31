using System;
using System.Net;

namespace Guts.Client.Shared.Utility
{
    public class HttpResponseException : Exception
    {
        public HttpStatusCode ResponseStatusCode { get; }

        public HttpResponseException(HttpStatusCode responseStatusCode, string message) : base(message)
        {
            ResponseStatusCode = responseStatusCode;
        }
    }
}