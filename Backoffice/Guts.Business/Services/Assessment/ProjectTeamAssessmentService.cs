﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
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

        public async Task<ProjectTeamAssessmentStatusDto> GetStatusAsync(int projectAssessmentId, int teamId)
        {
            IProjectTeamAssessment teamAssessment = await GetOrCreateTeamAssessmentAsync(projectAssessmentId, teamId);

            return new ProjectTeamAssessmentStatusDto
            {
                Id = teamAssessment.Id,
                TeamId = teamId,
                IsComplete = teamAssessment.IsComplete,
                PeersThatNeedToEvaluateOthers = teamAssessment.GetPeersThatNeedToEvaluateOthers().Select(user =>
                    new UserDto { FirstName = user.FirstName, LastName = user.LastName, Id = user.Id }).ToList(),
            };
        }

        public async Task<IReadOnlyList<IPeerAssessment>> GetPeerAssessmentsOfUserAsync(int projectAssessmentId, int teamId, int userId)
        {
            IProjectTeamAssessment teamAssessment = await GetOrCreateTeamAssessmentAsync(projectAssessmentId, teamId);

            IReadOnlyList<IPeerAssessment> storedAssessments =  teamAssessment.GetPeersAssessmentsOf(userId);
            IReadOnlyList<IPeerAssessment> missingAssessments = teamAssessment.GetMissingPeersAssessmentsOf(userId);

            return storedAssessments.Concat(missingAssessments).ToList();
        }

        public async Task SavePeerAssessmentsOfUserAsync(int projectAssessmentId, int teamId, int userId, IReadOnlyList<PeerAssessmentDto> peerAssessments)
        {
            IProjectTeamAssessment teamAssessment = await GetOrCreateTeamAssessmentAsync(projectAssessmentId, teamId);
            foreach (PeerAssessmentDto dto in peerAssessments)
            {
                Contracts.Require(dto.UserId == userId, $"Only peer assessments of user with id '{userId}' are allowed.");
                IPeerAssessment peerAssessment = teamAssessment.AddOrReplacePeerAssessment(userId, dto.SubjectId,
                    dto.CooperationScore, dto.ContributionScore, dto.EffortScore);
                peerAssessment.Explanation = dto.Explanation;
            }

            teamAssessment.ValidateAssessmentsOf(userId);

            await _repository.UpdateAsync(teamAssessment);
        }
    }
}