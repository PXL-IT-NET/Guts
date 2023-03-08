using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.ValueObjects
{
    public class AssessmentScore : ValueObject<AssessmentScore>
    {
        public static AssessmentScore NoAddedValue = new AssessmentScore(0);
        public static AssessmentScore WayBelowAverage = new AssessmentScore(1);
        public static AssessmentScore BelowAverage = new AssessmentScore(2);
        public static AssessmentScore Average = new AssessmentScore(3);
        public static AssessmentScore AboveAverage = new AssessmentScore(4);
        public static AssessmentScore WayAboveAverage = new AssessmentScore(5);

        public int Value { get; private set; }

        internal AssessmentScore(int value)
        {
            Contracts.Require(value > 0, "Assessment score must be positive");
            Contracts.Require(value < 5, "Assessment score must be less than or equal to 5");
            Value = value;
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