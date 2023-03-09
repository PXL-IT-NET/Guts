﻿using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Business.Services.Assessment
{
    internal class ProjectTeamAssessmentService : IProjectTeamAssessmentService
    {
        private readonly IProjectTeamAssessmentRepository _repository;
        private readonly IProjectTeamAssessmentFactory _factory;
        private readonly IProjectAssessmentRepository _projectAssessmentRepository;
        private readonly IProjectTeamRepository _teamRepository;

        public ProjectTeamAssessmentService(
            IProjectTeamAssessmentRepository repository,
            IProjectTeamAssessmentFactory factory,
            IProjectAssessmentRepository projectAssessmentRepository,
            IProjectTeamRepository teamRepository)
        {
            _repository = repository;
            _factory = factory;
            _projectAssessmentRepository = projectAssessmentRepository;
            _teamRepository = teamRepository;
        }

        public async Task<IProjectTeamAssessment> GetOrCreateTeamAssessmentAsync(int projectAssessmentId, int projectTeamId)
        {
            IProjectTeamAssessment teamAssessment;
            try
            {
                teamAssessment = await _repository.LoadAsync(projectAssessmentId, projectTeamId);
            }
            catch (DataNotFoundException)
            {
                IProjectAssessment projectAssessment;
                try
                {
                    projectAssessment = await _projectAssessmentRepository.GetByIdAsync(projectAssessmentId);
                }
                catch (DataNotFoundException)
                {
                    throw new ContractException($"The team assessment could not be created. No project assessment with id '{projectAssessmentId}' could be found.");
                }

                IProjectTeam team;
                try
                {
                    team = await _teamRepository.LoadByIdAsync(projectTeamId);
                }
                catch (DataNotFoundException)
                {
                    throw new ContractException($"The team assessment could not be created. No team with id '{projectTeamId}' could be found.");
                }

                teamAssessment = _factory.CreateNew(projectAssessment, team);
                await _repository.AddAsync(teamAssessment);
            }

            return teamAssessment;
        }
    }
}