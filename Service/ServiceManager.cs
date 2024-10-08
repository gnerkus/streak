﻿using AutoMapper;
using Contracts;
using Entities;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Service
{
    public sealed class ServiceManager : IServiceManager
    {
        private readonly Lazy<IAuthenticationService> _authenticationService;
        private readonly Lazy<ILeaderboardService> _leaderboardService;
        private readonly Lazy<IOrganizationService> _orgService;
        private readonly Lazy<IParticipantService> _participantService;
        private readonly Lazy<IScoreService> _scoreService;

        public ServiceManager(IRepositoryManager repositoryManager, ILoggerManager
                loggerManager, IMapper mapper, IParticipantLinks participantLinks,
            UserManager<User>
                userManager, IOptions<JwtConfiguration> configuration, IScoreLinks scoreLinks,
            ILeaderboardLinks leaderboardLinks)
        {
            _orgService = new Lazy<IOrganizationService>(() => new OrganizationService
                (repositoryManager, loggerManager, mapper));
            _participantService = new Lazy<IParticipantService>(() => new ParticipantService
                (repositoryManager, loggerManager, mapper, participantLinks));
            _leaderboardService = new Lazy<ILeaderboardService>(() => new LeaderboardService
                (repositoryManager, loggerManager, mapper, leaderboardLinks));
            _scoreService = new Lazy<IScoreService>(() => new ScoreService
                (repositoryManager, loggerManager, mapper, scoreLinks));
            _authenticationService = new Lazy<IAuthenticationService>(() => new
                AuthenticationService(loggerManager, mapper, userManager, configuration));
        }

        public IOrganizationService OrganizationService => _orgService.Value;
        public IParticipantService ParticipantService => _participantService.Value;
        public ILeaderboardService LeaderboardService => _leaderboardService.Value;
        public IScoreService ScoreService => _scoreService.Value;
        public IAuthenticationService AuthenticationService => _authenticationService.Value;
    }
}