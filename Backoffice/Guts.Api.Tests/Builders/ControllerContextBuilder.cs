using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Tests.Builders;

internal class ControllerContextBuilder
{
    private readonly ControllerContext _context;
    private readonly Random _random;

    public ControllerContextBuilder()
    {
        _random = new();
        _context = new() { HttpContext = new DefaultHttpContext() };
        _context.HttpContext.User = new(new ClaimsIdentity(new List<Claim>()));
    }

    public ControllerContextBuilder WithUser(string nameIdentifier)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nameIdentifier)
        };
        _context.HttpContext.User = new(new ClaimsIdentity(claims.Union(_context.HttpContext.User.Claims)));
        return this;
    }

    public ControllerContextBuilder WithRole(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        _context.HttpContext.User = new(new ClaimsIdentity(claims.Union(_context.HttpContext.User.Claims)));
        return this;
    }

    public ControllerContextBuilder WithUserWithoutNameIdentifier()
    {
        _context.HttpContext.User = new(new ClaimsIdentity(new List<Claim>()));
        return this;
    }

    public ControllerContextBuilder WithClientIp()
    {
        var maxValue = Convert.ToInt64("FFFFFFFF", 16);
        var address = Convert.ToInt64(_random.NextDouble() * maxValue);
        _context.HttpContext.Connection.RemoteIpAddress = new(address);
        return this;
    }

    public ControllerContextBuilder WithClientIp(string ipString)
    {
        var ipAddress = IPAddress.Parse(ipString);
        _context.HttpContext.Connection.RemoteIpAddress = ipAddress;
        return this;
    }

    public ControllerContextBuilder WithEmail(string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        _context.HttpContext.User = new(new ClaimsIdentity(claims.Union(_context.HttpContext.User.Claims)));
        return this;
    }

    public ControllerContext Build()
    {
        return _context;
    }

        
}