using System;

namespace Guts.Common
{
    public class ContractException : Exception
    {
        public ContractException()
        {
        }

        public ContractException(string message)
            : base(message)
        {
        }

        public ContractException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}