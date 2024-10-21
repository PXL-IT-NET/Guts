using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guts.Common;

namespace Guts.Domain.ValueObjects
{
    public class Code : ValueObject<Code>
    {
        public const int MaxLength = 64;

        public string Value { get; }

        public Code(string value)
        {
            Contracts.Require(!string.IsNullOrEmpty(value), "A code cannot be empty");
            Contracts.Require(value!.Length <= MaxLength, $"A code can not contain more than {MaxLength} characters");

            var regex = new Regex(@"^[\w-\.]+$");
            Contracts.Require(regex.IsMatch(value), "A code may only consist of letters, numbers, underscores, hyphens or periods");

            Value = value;
        }

        public static implicit operator string(Code code)
        {
            return code.Value;
        }

        public static implicit operator Code(string value)
        {
            return new Code(value);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}