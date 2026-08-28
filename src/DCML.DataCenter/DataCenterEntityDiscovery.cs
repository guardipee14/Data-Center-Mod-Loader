using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Classification;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

public sealed class DataCenterEntityDiscovery :
    IDataCenterEntityDiscovery
{
    private readonly IDCMLGameObjectDiscovery
        _gameObjectDiscovery;

    private readonly IReadOnlyList<DataCenterEntityRule>
        _rules;

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery)
        : this(
            gameObjectDiscovery,
            DataCenterDefaultEntityRules.Create())
    {
    }

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery,
        IEnumerable<DataCenterEntityRule> rules)
    {
        _gameObjectDiscovery =
            gameObjectDiscovery ??
            throw new ArgumentNullException(
                nameof(gameObjectDiscovery));

        if (rules is null)
        {
            throw new ArgumentNullException(
                nameof(rules));
        }

        _rules =
            rules
                .OrderByDescending(
                    value =>
                        value.Priority)
                .ThenBy(
                    value =>
                        value.Id,
                    StringComparer.Ordinal)
                .ToArray();
    }

    public IReadOnlyList<DataCenterEntityInfo> Find(
        DataCenterEntityQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        IReadOnlyList<DCMLGameObjectInfo> objects =
            _gameObjectDiscovery.Find(
                new DCMLGameObjectQuery(
                    nameContains:
                        query.NameContains,
                    sceneName:
                        query.SceneName,
                    componentTypeName:
                        query.ComponentTypeName,
                    includeInactive:
                        query.IncludeInactive,
                    maxResults:
                        DCMLGameObjectQuery.MaximumMaxResults));

        var results =
            new List<DataCenterEntityInfo>();

        foreach (
            DCMLGameObjectInfo source in objects)
        {
            DataCenterEntityInfo entity =
                Classify(
                    source);

            if (
                !query.IncludeUnknown &&
                string.Equals(
                    entity.Kind,
                    DataCenterEntityKinds.Unknown,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            if (
                query.Kind.Length > 0 &&
                !string.Equals(
                    entity.Kind,
                    query.Kind,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            results.Add(
                entity);

            if (
                results.Count >=
                query.MaxResults
            )
            {
                break;
            }
        }

        return
            results.ToArray();
    }

    public DataCenterEntityInfo Classify(
        DCMLGameObjectInfo source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(
                nameof(source));
        }

        foreach (
            DataCenterEntityRule rule in
            _rules)
        {
            if (
                rule.IsMatch(
                    source)
            )
            {
                return
                    new DataCenterEntityInfo(
                        source,
                        rule.Kind,
                        rule.Id);
            }
        }

        return
            new DataCenterEntityInfo(
                source,
                DataCenterEntityKinds.Unknown,
                string.Empty);
    }
}
