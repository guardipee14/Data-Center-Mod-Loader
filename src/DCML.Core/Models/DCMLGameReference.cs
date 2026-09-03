using System;

namespace DCML.Core.Models;

public sealed class DCMLGameReference
{
    public DCMLGameReference(
        int instanceId,
        string? name,
        string? typeName)
    {
        InstanceId =
            instanceId;

        Name =
            name ??
            string.Empty;

        TypeName =
            string.IsNullOrWhiteSpace(
                typeName)
                ? string.Empty
                : typeName.Trim();
    }

    public int InstanceId { get; }

    public string Name { get; }

    public string TypeName { get; }
}
