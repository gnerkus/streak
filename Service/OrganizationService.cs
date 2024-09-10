﻿using AutoMapper;
using Contracts;
using Shared;

namespace Service
{
    internal sealed class OrganizationService : IOrganizationService
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IRepositoryManager _repository;

        public OrganizationService(IRepositoryManager repository, ILoggerManager logger, IMapper
            mapper)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
        }

        public IEnumerable<OrganizationDto> GetAllOrganizations(bool trackChanges)
        {
            var orgs = _repository.Organization.GetAllOrganizations(trackChanges);
            var orgsDto = _mapper.Map<IEnumerable<OrganizationDto>>(orgs);
            return orgsDto;
        }
    }
}