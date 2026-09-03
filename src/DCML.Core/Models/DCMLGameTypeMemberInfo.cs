using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameTypeMemberInfo
{
    private readonly IReadOnlyList<DCMLGameParameterInfo>
        _parameters;

    public DCMLGameTypeMemberInfo(
        string kind,
        string name,
        string declaringTypeFullName,
        string? valueTypeFullName,
        string accessibility,
        bool isStatic,
        bool isAbstract,
        bool isInherited,
        bool canRead,
        bool canWrite,
        int genericArgumentCount,
        IEnumerable<DCMLGameParameterInfo>? parameters,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException(
                "A member kind is required.",
                nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A member name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(declaringTypeFullName))
        {
            throw new ArgumentException(
                "A declaring type is required.",
                nameof(declaringTypeFullName));
        }

        if (string.IsNullOrWhiteSpace(accessibility))
        {
            throw new ArgumentException(
                "Member accessibility is required.",
                nameof(accessibility));
        }

        if (genericArgumentCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(genericArgumentCount));
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            throw new ArgumentException(
                "A member signature is required.",
                nameof(signature));
        }

        Kind =
            kind.Trim();

        Name =
            name.Trim();

        DeclaringTypeFullName =
            declaringTypeFullName.Trim();

        ValueTypeFullName =
            valueTypeFullName?.Trim() ??
            string.Empty;

        Accessibility =
            accessibility.Trim();

        IsStatic =
            isStatic;

        IsAbstract =
            isAbstract;

        IsInherited =
            isInherited;

        CanRead =
            canRead;

        CanWrite =
            canWrite;

        GenericArgumentCount =
            genericArgumentCount;

        _parameters =
            parameters is null
                ? Array.Empty<DCMLGameParameterInfo>()
                : parameters
                    .OrderBy(
                        value =>
                            value.Position)
                    .ToArray();

        Signature =
            signature.Trim();
    }

    public string Kind { get; }

    public string Name { get; }

    public string DeclaringTypeFullName { get; }

    public string ValueTypeFullName { get; }

    public string Accessibility { get; }

    public bool IsStatic { get; }

    public bool IsAbstract { get; }

    public bool IsInherited { get; }

    public bool CanRead { get; }

    public bool CanWrite { get; }

    public int GenericArgumentCount { get; }

    public IReadOnlyList<DCMLGameParameterInfo> Parameters =>
        _parameters;

    public string Signature { get; }
}
