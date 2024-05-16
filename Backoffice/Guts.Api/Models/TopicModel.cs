using System.Collections.Generic;
using AutoMapper;
using Guts.Api.Models.AssignmentModels;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Api.Models
{
    public class TopicModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<AssignmentModel> Assignments { get; set; } = new List<AssignmentModel>();
    }
}