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

    private readonly DataCenterTypeHierarchy?
        _typeHierarchy;

    private readonly IReadOnlyList<DataCenterEntityRule>
        _rules;

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery)
        : this(
            gameObjectDiscovery,
            null,
            DataCenterDefaultEntityRules.Create())
    {
    }

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery,
        IDCMLGameTypeCatalog? gameTypeCatalog)
        : this(
            gameObjectDiscovery,
            gameTypeCatalog,
            DataCenterDefaultEntityRules.Create())
    {
    }

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery,
        IEnumerable<DataCenterEntityRule> rules)
        : this(
            gameObjectDiscovery,
            null,
            rules)
    {
    }

    public DataCenterEntityDiscovery(
        IDCMLGameObjectDiscovery gameObjectDiscovery,
        IDCMLGameTypeCatalog? gameTypeCatalog,
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

        _typeHierarchy =
            gameTypeCatalog is null
                ? null
                : new DataCenterTypeHierarchy(
                    gameTypeCatalog);

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

        var results =
            new List<DataCenterEntityInfo>();

        int skipResults =
            0;

        while (
            results.Count <
            query.MaxResults
        )
        {
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
                            DCMLGameObjectQuery.MaximumMaxResults,
                        skipResults:
                            skipResults));

            foreach (
                DCMLGameObjectInfo source in
                objects)
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

            if (
                results.Count >=
                    query.MaxResults ||
                objects.Count <
                    DCMLGameObjectQuery.MaximumMaxResults
            )
            {
                break;
            }

            skipResults =
                checked(
                    skipResults +
                    DCMLGameObjectQuery.MaximumMaxResults);
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

        Func<string, string, bool>? isAssignableTo =
            null;

        if (_typeHierarchy is not null)
        {
            isAssignableTo =
                _typeHierarchy.IsAssignableTo;
        }

        foreach (
            DataCenterEntityRule rule in
            _rules)
        {
            if (
                rule.IsMatch(
                    source,
                    isAssignableTo)
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
