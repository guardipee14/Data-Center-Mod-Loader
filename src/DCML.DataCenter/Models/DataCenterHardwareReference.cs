using System;
using DCML.Core.Models;

namespace DCML.DataCenter.Models;

public sealed class DataCenterHardwareReference
{
    public DataCenterHardwareReference(
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

    internal static DataCenterHardwareReference? FromCore(
        DCMLGameReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return
            new DataCenterHardwareReference(
                reference.InstanceId,
                reference.Name,
                reference.TypeName);
    }
}
