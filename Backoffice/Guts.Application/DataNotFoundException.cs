using System;

namespace Guts.Application;

public class DataNotFoundException : Exception
{
    public DataNotFoundException()
    {
    }

    public DataNotFoundException(string message) : base(message)
    {
    }
}