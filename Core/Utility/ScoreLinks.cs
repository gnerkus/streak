﻿using Contracts;
using Entities;
using Microsoft.Net.Http.Headers;
using Shared;

namespace streak.Utility
{
    public class ScoreLinks : IScoreLinks
    {
        private readonly IDataShaper<ScoreDto> _dataShaper;
        private readonly LinkGenerator _linkGenerator;

        public ScoreLinks(LinkGenerator linkGenerator, IDataShaper<ScoreDto> dataShaper)
        {
            _linkGenerator = linkGenerator;
            _dataShaper = dataShaper;
        }

        public LinkResponse TryGenerateLinks(IEnumerable<ScoreDto> scoresDto, string
            fields, HttpContext httpContext)
        {
            var shapedScores = ShapeData(scoresDto, fields);
            if (ShouldGenerateLinks(httpContext))
                return ReturnLinkedScores(scoresDto, fields, httpContext,
                    shapedScores);
            return ReturnShapedScores(shapedScores);
        }

        private LinkResponse ReturnLinkedScores(IEnumerable<ScoreDto> scoresDto, string fields,
            HttpContext httpContext, List<Entity> shapedScores)
        {
            var scoreDtoList = scoresDto.ToList();
            for (var index = 0; index < scoreDtoList.Count; index++)
            {
                var scoreLinks = CreateLinksForScore(httpContext, scoreDtoList[index].Id, fields);
                shapedScores[index].Add("Links", scoreLinks);
            }

            var scoreCollection = new LinkCollectionWrapper<Entity>(shapedScores);
            var linkedScores = CreateLinksForScores(httpContext, scoreCollection);
            return new LinkResponse { HasLinks = true, LinkedEntities = linkedScores };
        }

        private List<Link> CreateLinksForScore(HttpContext httpContext, Guid
            id, string fields = "")
        {
            const string scoreController = "Score";
            var links = new List<Link>
            {
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "GetScore",
                        scoreController,
                        new { id, fields }
                    )!,
                    "self",
                    "GET"
                ),
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "DeleteScore",
                        scoreController,
                        new { id }
                    )!,
                    "delete_score",
                    "DELETE"
                ),
                new(
                    _linkGenerator.GetUriByAction(
                        httpContext,
                        "UpdateScore",
                        scoreController,
                        new { id }
                    )!,
                    "update_score",
                    "PUT"
                )
            };

            return links;
        }

        private LinkCollectionWrapper<Entity> CreateLinksForScores(HttpContext httpContext,
            LinkCollectionWrapper<Entity> scoresWrapper)
        {
            scoresWrapper.Links.Add(new Link(_linkGenerator.GetUriByAction(httpContext,
                    "GetScores", "Score", new { })!,
                "self",
                "GET"));
            return scoresWrapper;
        }

        private static LinkResponse ReturnShapedScores(List<Entity> shapedScores)
        {
            return new LinkResponse { ShapedEntities = shapedScores };
        }

        private static bool ShouldGenerateLinks(HttpContext httpContext)
        {
            var acceptMediaType = httpContext.Items["AcceptHeaderMediaType"];
            if (acceptMediaType is null) return false;
            var mediaType = (MediaTypeHeaderValue)acceptMediaType;
            return mediaType.SubTypeWithoutSuffix.EndsWith("hateoas",
                StringComparison.InvariantCultureIgnoreCase);
        }

        private List<Entity> ShapeData(IEnumerable<ScoreDto> scoresDto, string fields)
        {
            return _dataShaper.ShapeData(scoresDto, fields)
                .Select(e => e.Entity)
                .ToList();
        }
    }
}