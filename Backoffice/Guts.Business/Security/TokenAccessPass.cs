using System;

namespace Guts.Business.Security
{
    public class TokenAccessPass
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}