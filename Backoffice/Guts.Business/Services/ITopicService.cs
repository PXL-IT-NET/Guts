﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services
{
    public interface ITopicService
    {
        Task<Topic> GetTopicAsync(string courseCode, string topicCode);

        Task<IList<Topic>> GetTopicsByCourseWithAssignmentsAndTestsAsync(int courseId);
    }
}