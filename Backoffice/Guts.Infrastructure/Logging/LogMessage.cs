using System;

namespace Guts.Infrastructure.Logging
{
    public struct LogMessage
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; set; }
    }
}