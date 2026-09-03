using System;

namespace DCML.Core.Models;

public sealed class DCMLGameTypeInspectionQuery
{
    public const int DefaultMaxMembers =
        2048;

    public const int MaximumMaxMembers =
        16384;

    public DCMLGameTypeInspectionQuery(
        string typeFullName,
        string? assemblyName = null,
        bool includeInheritedMembers = true,
        int maxMembers = DefaultMaxMembers)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentException(
                "A full type name is required.",
                nameof(typeFullName));
        }

        if (
            maxMembers <= 0 ||
            maxMembers > MaximumMaxMembers
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMembers),
                maxMembers,
                $"Max members must be between 1 and {MaximumMaxMembers}.");
        }

        TypeFullName =
            typeFullName.Trim();

        AssemblyName =
            string.IsNullOrWhiteSpace(
                assemblyName)
                ? string.Empty
                : assemblyName.Trim();

        IncludeInheritedMembers =
            includeInheritedMembers;

        MaxMembers =
            maxMembers;
    }

    public string TypeFullName { get; }

    public string AssemblyName { get; }

    public bool IncludeInheritedMembers { get; }

    public int MaxMembers { get; }
}
