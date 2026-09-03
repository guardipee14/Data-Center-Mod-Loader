using System;

namespace DCML.Core.Models;

public sealed class DCMLGameParameterInfo
{
    public DCMLGameParameterInfo(
        int position,
        string? name,
        string typeFullName,
        bool isOptional,
        bool isOut,
        bool isByRef)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position));
        }

        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentException(
                "A parameter type is required.",
                nameof(typeFullName));
        }

        Position =
            position;

        Name =
            string.IsNullOrWhiteSpace(name)
                ? "arg" + position
                : name.Trim();

        TypeFullName =
            typeFullName.Trim();

        IsOptional =
            isOptional;

        IsOut =
            isOut;

        IsByRef =
            isByRef;
    }

    public int Position { get; }

    public string Name { get; }

    public string TypeFullName { get; }

    public bool IsOptional { get; }

    public bool IsOut { get; }

    public bool IsByRef { get; }
}
