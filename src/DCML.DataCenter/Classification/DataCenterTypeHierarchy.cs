using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.DataCenter.Classification;

internal sealed class DataCenterTypeHierarchy
{
    private readonly IDCMLGameTypeCatalog _catalog;

    private readonly object _syncRoot =
        new object();

    private IReadOnlyDictionary<string, DCMLGameTypeInfo>?
        _types;

    public DataCenterTypeHierarchy(
        IDCMLGameTypeCatalog catalog)
    {
        _catalog =
            catalog ??
            throw new ArgumentNullException(
                nameof(catalog));
    }

    public bool IsAssignableTo(
        string candidateTypeName,
        string targetTypeName)
    {
        if (
            string.IsNullOrWhiteSpace(candidateTypeName) ||
            string.IsNullOrWhiteSpace(targetTypeName)
        )
        {
            return false;
        }

        if (
            IsSameTypeName(
                candidateTypeName,
                targetTypeName)
        )
        {
            return true;
        }

        IReadOnlyDictionary<string, DCMLGameTypeInfo> types =
            GetTypes();

        string current =
            candidateTypeName.Trim();

        var visited =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        while (
            current.Length > 0 &&
            visited.Add(
                current)
        )
        {
            if (
                !TryGetType(
                    types,
                    current,
                    out DCMLGameTypeInfo? info) ||
                info is null
            )
            {
                return false;
            }

            foreach (
                string interfaceTypeName in
                info.InterfaceFullNames)
            {
                if (
                    IsSameTypeName(
                        interfaceTypeName,
                        targetTypeName)
                )
                {
                    return true;
                }
            }

            string baseType =
                info.BaseTypeFullName;

            if (baseType.Length == 0)
            {
                return false;
            }

            if (
                IsSameTypeName(
                    baseType,
                    targetTypeName)
            )
            {
                return true;
            }

            current =
                baseType;
        }

        return false;
    }

    private IReadOnlyDictionary<string, DCMLGameTypeInfo>
        GetTypes()
    {
        if (_types is not null)
        {
            return
                _types;
        }

        lock (_syncRoot)
        {
            if (_types is not null)
            {
                return
                    _types;
            }

            IReadOnlyList<DCMLGameTypeInfo> catalogTypes =
                _catalog.Find(
                    new DCMLGameTypeQuery(
                        fullNameStartsWith:
                            "Il2Cpp.",
                        maxResults:
                            DCMLGameTypeQuery.MaximumMaxResults));

            var result =
                new Dictionary<string, DCMLGameTypeInfo>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (
                DCMLGameTypeInfo type in
                catalogTypes)
            {
                result[type.FullName] =
                    type;
            }

            _types =
                result;

            return
                _types;
        }
    }

    private static bool TryGetType(
        IReadOnlyDictionary<string, DCMLGameTypeInfo> types,
        string requestedTypeName,
        out DCMLGameTypeInfo? info)
    {
        if (
            types.TryGetValue(
                requestedTypeName,
                out info)
        )
        {
            return true;
        }

        string requestedSimpleName =
            GetSimpleTypeName(
                requestedTypeName);

        foreach (
            KeyValuePair<string, DCMLGameTypeInfo> pair in
            types)
        {
            if (
                string.Equals(
                    GetSimpleTypeName(
                        pair.Key),
                    requestedSimpleName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                info =
                    pair.Value;

                return true;
            }
        }

        info =
            null;

        return false;
    }

    private static bool IsSameTypeName(
        string left,
        string right)
    {
        if (
            string.Equals(
                left,
                right,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return
            string.Equals(
                GetSimpleTypeName(
                    left),
                GetSimpleTypeName(
                    right),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSimpleTypeName(
        string value)
    {
        int lastDot =
            value.LastIndexOf('.');

        return
            lastDot >= 0
                ? value.Substring(
                    lastDot + 1)
                : value;
    }
}
