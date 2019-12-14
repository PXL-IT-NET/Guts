using System;
using System.Runtime.Serialization;

namespace Guts.Business
{
    [Serializable]
    public class DataNotFoundException : ApplicationException
    {
        public DataNotFoundException()
        {
        }

        public DataNotFoundException(string message) : base(message)
        {
        }

        protected DataNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}