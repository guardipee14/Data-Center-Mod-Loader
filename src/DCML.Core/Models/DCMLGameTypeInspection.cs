using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameTypeInspection
{
    private readonly IReadOnlyList<string>
        _baseTypeFullNames;

    private readonly IReadOnlyList<string>
        _interfaceFullNames;

    private readonly IReadOnlyList<DCMLGameTypeMemberInfo>
        _members;

    public DCMLGameTypeInspection(
        string typeFullName,
        string assemblyName,
        IEnumerable<string>? baseTypeFullNames,
        IEnumerable<string>? interfaceFullNames,
        IEnumerable<DCMLGameTypeMemberInfo>? members,
        int totalMemberCount,
        bool atMemberLimit)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentException(
                "A full type name is required.",
                nameof(typeFullName));
        }

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new ArgumentException(
                "An assembly name is required.",
                nameof(assemblyName));
        }

        if (totalMemberCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalMemberCount));
        }

        TypeFullName =
            typeFullName.Trim();

        AssemblyName =
            assemblyName.Trim();

        _baseTypeFullNames =
            NormalizeNames(
                baseTypeFullNames,
                preserveOrder: true);

        _interfaceFullNames =
            NormalizeNames(
                interfaceFullNames,
                preserveOrder: false);

        _members =
            members is null
                ? Array.Empty<DCMLGameTypeMemberInfo>()
                : members.ToArray();

        TotalMemberCount =
            totalMemberCount;

        AtMemberLimit =
            atMemberLimit;
    }

    public string TypeFullName { get; }

    public string AssemblyName { get; }

    public IReadOnlyList<string> BaseTypeFullNames =>
        _baseTypeFullNames;

    public IReadOnlyList<string> InterfaceFullNames =>
        _interfaceFullNames;

    public IReadOnlyList<DCMLGameTypeMemberInfo> Members =>
        _members;

    public int TotalMemberCount { get; }

    public bool AtMemberLimit { get; }

    public IReadOnlyList<DCMLGameTypeMemberInfo> Fields =>
        Members
            .Where(
                value =>
                    value.Kind == "field")
            .ToArray();

    public IReadOnlyList<DCMLGameTypeMemberInfo> Properties =>
        Members
            .Where(
                value =>
                    value.Kind == "property")
            .ToArray();

    public IReadOnlyList<DCMLGameTypeMemberInfo> Methods =>
        Members
            .Where(
                value =>
                    value.Kind == "method")
            .ToArray();

    public IReadOnlyList<DCMLGameTypeMemberInfo> Constructors =>
        Members
            .Where(
                value =>
                    value.Kind == "constructor")
            .ToArray();

    private static IReadOnlyList<string> NormalizeNames(
        IEnumerable<string>? values,
        bool preserveOrder)
    {
        if (values is null)
        {
            return
                Array.Empty<string>();
        }

        IEnumerable<string> normalized =
            values
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value))
                .Select(
                    value =>
                        value.Trim())
                .Distinct(
                    StringComparer.Ordinal);

        if (!preserveOrder)
        {
            normalized =
                normalized.OrderBy(
                    value =>
                        value,
                    StringComparer.Ordinal);
        }

        return
            normalized.ToArray();
    }
}
