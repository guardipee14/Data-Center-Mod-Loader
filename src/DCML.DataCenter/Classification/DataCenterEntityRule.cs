using System;
using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.DataCenter.Classification;

public sealed class DataCenterEntityRule
{
    public DataCenterEntityRule(
        string id,
        string kind,
        int priority = 0,
        string? nameContains = null,
        string? hierarchyStartsWith = null,
        string? hierarchyContains = null,
        string? componentTypeName = null,
        string? componentTypePrefix = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "A classification rule ID is required.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException(
                "A classification kind is required.",
                nameof(kind));
        }

        Id =
            id.Trim();

        Kind =
            kind.Trim();

        Priority =
            priority;

        NameContains =
            Normalize(
                nameContains);

        HierarchyStartsWith =
            Normalize(
                hierarchyStartsWith);

        HierarchyContains =
            Normalize(
                hierarchyContains);

        ComponentTypeName =
            Normalize(
                componentTypeName);

        ComponentTypePrefix =
            Normalize(
                componentTypePrefix);

        if (
            NameContains.Length == 0 &&
            HierarchyStartsWith.Length == 0 &&
            HierarchyContains.Length == 0 &&
            ComponentTypeName.Length == 0 &&
            ComponentTypePrefix.Length == 0
        )
        {
            throw new ArgumentException(
                "At least one classification matcher is required.");
        }
    }

    public string Id { get; }

    public string Kind { get; }

    public int Priority { get; }

    public string NameContains { get; }

    public string HierarchyStartsWith { get; }

    public string HierarchyContains { get; }

    public string ComponentTypeName { get; }

    public string ComponentTypePrefix { get; }

    public bool IsMatch(
        DCMLGameObjectInfo source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(
                nameof(source));
        }

        if (
            NameContains.Length > 0 &&
            source.Name.IndexOf(
                NameContains,
                StringComparison.OrdinalIgnoreCase) < 0
        )
        {
            return false;
        }

        if (
            HierarchyStartsWith.Length > 0 &&
            !source.HierarchyPath.StartsWith(
                HierarchyStartsWith,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        if (
            HierarchyContains.Length > 0 &&
            source.HierarchyPath.IndexOf(
                HierarchyContains,
                StringComparison.OrdinalIgnoreCase) < 0
        )
        {
            return false;
        }

        if (
            ComponentTypeName.Length > 0 &&
            !HasComponentType(
                source.ComponentTypeNames,
                ComponentTypeName)
        )
        {
            return false;
        }

        if (
            ComponentTypePrefix.Length > 0 &&
            !HasComponentPrefix(
                source.ComponentTypeNames,
                ComponentTypePrefix)
        )
        {
            return false;
        }

        return true;
    }

    private static bool HasComponentType(
        IReadOnlyList<string> names,
        string requested)
    {
        foreach (string value in names)
        {
            if (
                string.Equals(
                    value,
                    requested,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            int lastDot =
                value.LastIndexOf('.');

            if (
                lastDot >= 0 &&
                string.Equals(
                    value.Substring(
                        lastDot + 1),
                    requested,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasComponentPrefix(
        IReadOnlyList<string> names,
        string prefix)
    {
        foreach (string value in names)
        {
            if (
                value.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
