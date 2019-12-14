using System;
using System.Collections;
using System.Collections.Generic;

namespace Guts.Business.Dtos
{
    public class ExamPartDto
    {
        private DateTime _deadline;
        public string Name { get; set; }

        public DateTime Deadline
        {
            get => _deadline;
            set => _deadline = value.ToUniversalTime();
        }

        public IReadOnlyList<AssignmentEvaluationDto> AssignmentEvaluations { get; set; }
    }
}