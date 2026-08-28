using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

public sealed class DataCenterComponentCatalog :
    IDataCenterComponentCatalog
{
    private readonly IDCMLGameObjectDiscovery
        _gameObjectDiscovery;

    public DataCenterComponentCatalog(
        IDCMLGameObjectDiscovery gameObjectDiscovery)
    {
        _gameObjectDiscovery =
            gameObjectDiscovery ??
            throw new ArgumentNullException(
                nameof(gameObjectDiscovery));
    }

    public DataCenterComponentCatalogSnapshot Scan(
        DataCenterComponentCatalogQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        IReadOnlyList<DCMLGameObjectInfo> objects =
            _gameObjectDiscovery.Find(
                new DCMLGameObjectQuery(
                    sceneName:
                        query.SceneName,
                    includeInactive:
                        query.IncludeInactive,
                    maxResults:
                        query.MaxObjects,
                    componentTypeNamePrefix:
                        query.TypeNamePrefix));

        var accumulators =
            new Dictionary<string, ComponentAccumulator>(
                StringComparer.Ordinal);

        foreach (
            DCMLGameObjectInfo gameObject in
            objects)
        {
            foreach (
                string componentTypeName in
                gameObject.ComponentTypeNames
                    .Distinct(
                        StringComparer.Ordinal))
            {
                if (
                    query.TypeNamePrefix.Length > 0 &&
                    !componentTypeName.StartsWith(
                        query.TypeNamePrefix,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                if (
                    !accumulators.TryGetValue(
                        componentTypeName,
                        out ComponentAccumulator? accumulator)
                )
                {
                    accumulator =
                        new ComponentAccumulator(
                            componentTypeName);

                    accumulators.Add(
                        componentTypeName,
                        accumulator);
                }

                accumulator.Add(
                    gameObject,
                    query.MaxExamplesPerType);
            }
        }

        IReadOnlyList<DataCenterComponentTypeInfo> componentTypes =
            accumulators
                .Values
                .Select(
                    value =>
                        value.ToInfo())
                .OrderBy(
                    value =>
                        value.TypeName,
                    StringComparer.Ordinal)
                .ToArray();

        return
            new DataCenterComponentCatalogSnapshot(
                query.SceneName,
                objects.Count,
                componentTypes);
    }

    private sealed class ComponentAccumulator
    {
        private readonly List<string>
            _examples =
                new List<string>();

        public ComponentAccumulator(
            string typeName)
        {
            TypeName =
                typeName;
        }

        public string TypeName { get; }

        public int ObjectCount { get; private set; }

        public int ActiveObjectCount { get; private set; }

        public int InactiveObjectCount { get; private set; }

        public void Add(
            DCMLGameObjectInfo gameObject,
            int maxExamples)
        {
            ObjectCount++;

            if (gameObject.ActiveInHierarchy)
            {
                ActiveObjectCount++;
            }
            else
            {
                InactiveObjectCount++;
            }

            if (
                _examples.Count >= maxExamples ||
                string.IsNullOrWhiteSpace(
                    gameObject.HierarchyPath) ||
                _examples.Contains(
                    gameObject.HierarchyPath,
                    StringComparer.Ordinal)
            )
            {
                return;
            }

            _examples.Add(
                gameObject.HierarchyPath);
        }

        public DataCenterComponentTypeInfo ToInfo()
        {
            return
                new DataCenterComponentTypeInfo(
                    TypeName,
                    ObjectCount,
                    ActiveObjectCount,
                    InactiveObjectCount,
                    _examples);
        }
    }
}
