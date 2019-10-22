using System;
using System.Collections;
using System.Collections.Generic;

namespace Guts.Business.Dtos
{
    public class ExamPartDto
    {
        public string Name { get; set; }
        public DateTime Deadline { get; set; }
        public IReadOnlyList<AssignmentEvaluationDto> AssignmentEvaluations { get; set; }
    }
}