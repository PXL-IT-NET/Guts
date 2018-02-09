using System;

namespace Guts.Data
{
    public class DataNotFoundException : ApplicationException
    {
        public DataNotFoundException()
        {
        }

        public DataNotFoundException(string message) : base(message)
        {
        }
    }
}