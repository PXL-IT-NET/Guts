using Guts.Common;
using Guts.Domain.CourseAggregate;
using Guts.Domain.ExamAggregate;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;

namespace Guts.Domain.PeriodAggregate
{
    public class Period : AggregateRoot, IPeriod
    {
        private string _description;

        [Required]
        public string Description
        {
            get => _description;
            set
            {
                Contracts.Require(!string.IsNullOrEmpty(value), "The 'description' cannot be empty.");
                _description = value;
            }
        }

        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        private Period(string description, DateTime from, DateTime until)
        {
            Contracts.Require(from < until, "The 'from' date must be before the 'until' date.");

            Description = description;
            From = from;
            Until = until;
        }

        public void Update(string description, DateTime from, DateTime until, IList<IPeriod> allPeriods)
        {
            //check if the new dates overlap with another period
            List<IPeriod> otherPeriods = allPeriods.Where(p => p.Id != this.Id).ToList();
            foreach (IPeriod otherPeriod in otherPeriods)
            {
                Contracts.Require(from < otherPeriod.Until && until > otherPeriod.From,
                    $"The changes would make this period overlap with the period '{otherPeriod.Description}'.");
            }

            Description = description;
            From = from;
            Until = until;
        }

        public class Factory : IPeriodFactory
        {
            public Period CreateNew(string description, DateTime from, DateTime until, IList<IPeriod> existingPeriods)
            {
                //check if the new period overlaps with an existing period
                foreach (IPeriod existingPeriod in existingPeriods)
                {

                    bool isBefore = from < existingPeriod.From && until < existingPeriod.From;
                    bool isAfter = from > existingPeriod.Until && until > existingPeriod.Until;
                    Contracts.Require(isBefore || isAfter,
                        $"The new period overlaps with the period '{existingPeriod.Description}'.");
                }

                return new Period(description, from, until);
            }
        }
    }
}