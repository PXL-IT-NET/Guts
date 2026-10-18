using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.ValueObjects
{
    public class AssessmentScore : ValueObject<AssessmentScore>
    {
        public static readonly AssessmentScore NullScore = new AssessmentScore(-1);
        public static readonly AssessmentScore NoAddedValue = new AssessmentScore(0);
        public static readonly AssessmentScore WayBelowAverage = new AssessmentScore(1);
        public static readonly AssessmentScore BelowAverage = new AssessmentScore(2);
        public static readonly AssessmentScore Average = new AssessmentScore(3);
        public static readonly AssessmentScore AboveAverage = new AssessmentScore(4);
        public static readonly AssessmentScore WayAboveAverage = new AssessmentScore(5);

        public int Value { get; private set; }

        internal AssessmentScore(int value)
        {
            Contracts.Require(value <= 5, "Assessment score must be less than or equal to 5");
            Value = value;
            if (value < 0)
            {
                Value = -1;
            }
        }

        public static implicit operator int(AssessmentScore score)
        {
            return score.Value;
        }

        public static implicit operator AssessmentScore(int value)
        {
            return new AssessmentScore(value);
        }

        public static implicit operator AssessmentScore(double value)
        {
            int roundedValue = Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));
            return new AssessmentScore(roundedValue);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}