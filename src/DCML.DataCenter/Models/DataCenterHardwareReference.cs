using System;
using DCML.Core.Models;

namespace DCML.DataCenter.Models;

public sealed class DataCenterHardwareReference
{
    public DataCenterHardwareReference(
        int instanceId,
        string? name,
        string? typeName,
        string? persistentId = null,
        string? identityKind = null)
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

        PersistentID =
            string.IsNullOrWhiteSpace(
                persistentId)
                ? string.Empty
                : persistentId.Trim();

        IdentityKind =
            string.IsNullOrWhiteSpace(
                identityKind)
                ? string.Empty
                : identityKind.Trim();
    }

    public int InstanceId { get; }

    public string Name { get; }

    public string TypeName { get; }

    public string PersistentID { get; }

    public string IdentityKind { get; }

    public bool HasRuntimeInstance =>
        InstanceId != 0;

    public bool HasPersistentIdentity =>
        PersistentID.Length > 0;

    public string IdentityKey =>
        HasPersistentIdentity
            ? "persistent:" +
                (
                    IdentityKind.Length == 0
                        ? "unknown"
                        : IdentityKind
                ) +
                ":" +
                PersistentID
            : "runtime:" +
                InstanceId;

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
