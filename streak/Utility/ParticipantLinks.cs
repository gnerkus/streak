﻿using Contracts;
using Entities.Models;
using Microsoft.Net.Http.Headers;
using Shared;

namespace streak.Utility
{
    public class ParticipantLinks : IParticipantLinks
    {
        private readonly IDataShaper<ParticipantDto> _dataShaper;
        private readonly LinkGenerator _linkGenerator;

        public ParticipantLinks(LinkGenerator linkGenerator,
            IDataShaper<ParticipantDto> dataShaper)
        {
            _linkGenerator = linkGenerator;
            _dataShaper = dataShaper;
        }

        public LinkResponse TryGenerateLinks(IEnumerable<ParticipantDto> participantsDto,
            string fields, Guid orgId,
            HttpContext httpContext)
        {
            var shapedParticipants = ShapeData(participantsDto, fields);
            if (ShouldGenerateLinks(httpContext))
                return ReturnLinkedParticipants(participantsDto, fields, orgId, httpContext,
                    shapedParticipants);
            return ReturnShapedParticipants(shapedParticipants);
        }

        private LinkResponse ReturnLinkedParticipants(IEnumerable<ParticipantDto> participantsDto,
            string fields, Guid orgId, HttpContext httpContext, List<Entity> shapedParticipants)
        {
            var participantDtoList = participantsDto.ToList();
            for (var index = 0; index < participantDtoList.Count(); index++)
            {
                var participantLinks = CreateLinksForParticipant(httpContext, orgId,
                    participantDtoList[index].Id, fields);
                shapedParticipants[index].Add("Links", participantLinks);
            }

            var participantCollection = new LinkCollectionWrapper<Entity>(shapedParticipants);
            var linkedParticipants =
                CreateLinksForParticipants(httpContext, participantCollection);
            return new LinkResponse { HasLinks = true, LinkedEntities = linkedParticipants };
        }

        private List<Link> CreateLinksForParticipant(HttpContext httpContext, Guid orgId, Guid
            id, string fields = "")
        {
            var links = new List<Link>
            {
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "GetParticipantForOrganization",
                        "Organizations",
                        new { orgId, id, fields }
                    ),
                    "self",
                    "GET"
                ),
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "DeleteParticipantForOrg",
                        "Organizations",
                        new { orgId, id }
                    ),
                    "delete_participant",
                    "DELETE"
                ),
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "UpdateParticipantForOrg",
                        "Organizations",
                        new { orgId, id }
                    ),
                    "update_participant",
                    "PUT"
                ),
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "PartiallyUpdateParticipantForOrg",
                        "Organizations",
                        new { orgId, id }
                    ),
                    "partially_update_participant",
                    "PATCH"
                )
            };

            return links;
        }

        private LinkCollectionWrapper<Entity> CreateLinksForParticipants(HttpContext httpContext,
            LinkCollectionWrapper<Entity> participantsWrapper)
        {
            participantsWrapper.Links.Add(new Link(_linkGenerator.GetUriByAction(httpContext,
                    "GetParticipantsForOrganization", "Organizations", new { }),
                "self",
                "GET"));
            return participantsWrapper;
        }

        private LinkResponse ReturnShapedParticipants(List<Entity> shapedParticipants)
        {
            return new LinkResponse { ShapedEntities = shapedParticipants };
        }

        private bool ShouldGenerateLinks(HttpContext httpContext)
        {
            var mediaType = (MediaTypeHeaderValue)httpContext.Items["AcceptHeaderMediaType"];
            return mediaType.SubTypeWithoutSuffix.EndsWith("hateoas",
                StringComparison.InvariantCultureIgnoreCase);
        }

        private List<Entity> ShapeData(IEnumerable<ParticipantDto> participantsDto, string fields)
        {
            return _dataShaper.ShapeData(participantsDto, fields)
                .Select(e => e.Entity)
                .ToList();
        }
    }
}