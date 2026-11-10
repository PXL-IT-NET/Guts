using System;

namespace Guts.Application.Security;

public class TokenAccessPass
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}