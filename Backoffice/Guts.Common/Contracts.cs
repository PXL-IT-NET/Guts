using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Guts.Common
{
    public static class Contracts
    {
        [DebuggerStepThrough]
        public static void Require([DoesNotReturnIf(false)] bool precondition, string message = "")
        {
            if (!precondition)
                throw new ContractException(message);
        }

        [DebuggerStepThrough]
        public static void Require([DoesNotReturnIf(false)] bool precondition, Func<string> message)
        {
            if (!precondition)
                throw new ContractException(message());
        }
    }
}
