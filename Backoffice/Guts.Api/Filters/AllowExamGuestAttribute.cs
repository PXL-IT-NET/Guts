using System;

namespace Guts.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AllowExamGuestAttribute : Attribute
{
}
