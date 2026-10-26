using System;

namespace Guts.Business;

public class DataNotFoundException : Exception
{
    public DataNotFoundException()
    {
    }

    public DataNotFoundException(string message) : base(message)
    {
    }
}