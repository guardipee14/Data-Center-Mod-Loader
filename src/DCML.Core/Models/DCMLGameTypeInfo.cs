using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameTypeInfo
{
    private readonly IReadOnlyList<string>
        _interfaceFullNames;

    public DCMLGameTypeInfo(
        string fullName,
        string? namespaceName,
        string name,
        string assemblyName,
        string? baseTypeFullName,
        bool isClass,
        bool isInterface,
        bool isEnum,
        bool isValueType,
        bool isAbstract,
        IEnumerable<string>? interfaceFullNames)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "A full type name is required.",
                nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A type name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new ArgumentException(
                "An assembly name is required.",
                nameof(assemblyName));
        }

        FullName =
            fullName.Trim();

        NamespaceName =
            namespaceName?.Trim() ??
            string.Empty;

        Name =
            name.Trim();

        AssemblyName =
            assemblyName.Trim();

        BaseTypeFullName =
            baseTypeFullName?.Trim() ??
            string.Empty;

        IsClass =
            isClass;

        IsInterface =
            isInterface;

        IsEnum =
            isEnum;

        IsValueType =
            isValueType;

        IsAbstract =
            isAbstract;

        _interfaceFullNames =
            interfaceFullNames is null
                ? Array.Empty<string>()
                : interfaceFullNames
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value.Trim())
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();
    }

    public string FullName { get; }

    public string NamespaceName { get; }

    public string Name { get; }

    public string AssemblyName { get; }

    public string BaseTypeFullName { get; }

    public bool IsClass { get; }

    public bool IsInterface { get; }

    public bool IsEnum { get; }

    public bool IsValueType { get; }

    public bool IsAbstract { get; }

    public IReadOnlyList<string> InterfaceFullNames =>
        _interfaceFullNames;

    public string Kind =>
        IsEnum
            ? "enum"
            : IsInterface
                ? "interface"
                : IsClass
                    ? "class"
                    : IsValueType
                        ? "value-type"
                        : "type";
}
