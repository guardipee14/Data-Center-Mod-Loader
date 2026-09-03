using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DCML.DataCenter.Models;

public enum DataCenterPhysicalCableEndpointKind
{
    Unknown = 0,
    Server = 1,
    Switch = 2,
    Router = 3,
    Firewall = 4,
    PatchPanel = 5,
    PatchPanelPort = 6,
    CustomerBase = 7
}

public enum DataCenterPhysicalCableEndpointSide
{
    Start = 0,
    End = 1
}

public sealed class DataCenterCablePersistenceEndpoint
{
    public DataCenterCablePersistenceEndpoint(
        DataCenterPhysicalCableEndpointSide side,
        int linkType,
        string? serverId,
        string? switchId,
        int? customerId,
        string? position = null)
    {
        Side = side;
        LinkType = linkType;
        ServerID = Normalize(serverId);
        SwitchID = Normalize(switchId);
        CustomerID = customerId;
        Position = Normalize(position);
    }

    public DataCenterPhysicalCableEndpointSide Side { get; }

    public int LinkType { get; }

    public string ServerID { get; }

    public string SwitchID { get; }

    public int? CustomerID { get; }

    public string Position { get; }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}

public sealed class DataCenterCablePersistenceRecord
{
    public DataCenterCablePersistenceRecord(
        int cableId,
        DataCenterCablePersistenceEndpoint start,
        DataCenterCablePersistenceEndpoint end)
    {
        if (cableId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cableId));
        }

        CableID = cableId;

        Start =
            start ??
            throw new ArgumentNullException(
                nameof(start));

        End =
            end ??
            throw new ArgumentNullException(
                nameof(end));

        if (
            Start.Side !=
                DataCenterPhysicalCableEndpointSide.Start
        )
        {
            throw new ArgumentException(
                "The start endpoint must use the Start side.",
                nameof(start));
        }

        if (
            End.Side !=
                DataCenterPhysicalCableEndpointSide.End
        )
        {
            throw new ArgumentException(
                "The end endpoint must use the End side.",
                nameof(end));
        }
    }

    public int CableID { get; }

    public DataCenterCablePersistenceEndpoint Start { get; }

    public DataCenterCablePersistenceEndpoint End { get; }
}

public sealed class DataCenterPhysicalCableEndpointResolution
{
    internal DataCenterPhysicalCableEndpointResolution(
        DataCenterCablePersistenceEndpoint raw,
        DataCenterPhysicalCableEndpointKind kind,
        string? persistentId,
        string? parentPersistentId)
    {
        Raw =
            raw ??
            throw new ArgumentNullException(
                nameof(raw));

        Kind = kind;

        PersistentID =
            string.IsNullOrWhiteSpace(
                persistentId)
                ? string.Empty
                : persistentId.Trim();

        ParentPersistentID =
            string.IsNullOrWhiteSpace(
                parentPersistentId)
                ? string.Empty
                : parentPersistentId.Trim();
    }

    public DataCenterCablePersistenceEndpoint Raw { get; }

    public DataCenterPhysicalCableEndpointKind Kind { get; }

    public string PersistentID { get; }

    public string ParentPersistentID { get; }

    public bool IsResolved =>
        Kind !=
            DataCenterPhysicalCableEndpointKind.Unknown &&
        PersistentID.Length > 0;

    public string IdentityKey =>
        IsResolved
            ? Kind.ToString() +
                ":" +
                PersistentID
            : "Unknown:" +
                Raw.Side;
}

public sealed class DataCenterPhysicalCableConnection
{
    public DataCenterPhysicalCableConnection(
        int cableId,
        DataCenterPhysicalCableEndpointResolution start,
        DataCenterPhysicalCableEndpointResolution end)
    {
        if (cableId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cableId));
        }

        CableID = cableId;

        Start =
            start ??
            throw new ArgumentNullException(
                nameof(start));

        End =
            end ??
            throw new ArgumentNullException(
                nameof(end));
    }

    public int CableID { get; }

    public DataCenterPhysicalCableEndpointResolution Start { get; }

    public DataCenterPhysicalCableEndpointResolution End { get; }

    public bool IsFullyResolved =>
        Start.IsResolved &&
        End.IsResolved;
}

public sealed class DataCenterCablePersistenceIndex
{
    private readonly HashSet<string> _serverIds;
    private readonly HashSet<string> _switchIds;
    private readonly HashSet<string> _routerIds;
    private readonly HashSet<string> _firewallIds;
    private readonly HashSet<string> _patchPanelIds;
    private readonly HashSet<int> _customerIds;

    public DataCenterCablePersistenceIndex(
        IEnumerable<string>? serverIds,
        IEnumerable<string>? switchIds,
        IEnumerable<string>? routerIds,
        IEnumerable<string>? firewallIds,
        IEnumerable<string>? patchPanelIds,
        IEnumerable<int>? customerIds)
    {
        _serverIds =
            NormalizeStrings(
                serverIds);

        _switchIds =
            NormalizeStrings(
                switchIds);

        _routerIds =
            NormalizeStrings(
                routerIds);

        _firewallIds =
            NormalizeStrings(
                firewallIds);

        _patchPanelIds =
            NormalizeStrings(
                patchPanelIds);

        _customerIds =
            customerIds is null
                ? new HashSet<int>()
                : new HashSet<int>(
                    customerIds);
    }

    public DataCenterPhysicalCableEndpointResolution Resolve(
        DataCenterCablePersistenceEndpoint endpoint)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(
                nameof(endpoint));
        }

        switch (endpoint.LinkType)
        {
            case 1:
                return
                    ResolveServer(
                        endpoint);

            case 2:
                return
                    ResolveNetworkSide(
                        endpoint);

            case 3:
                return
                    ResolveCustomer(
                        endpoint);

            default:
                return
                    Unknown(
                        endpoint);
        }
    }

    public DataCenterPhysicalCableConnection Resolve(
        DataCenterCablePersistenceRecord cable)
    {
        if (cable is null)
        {
            throw new ArgumentNullException(
                nameof(cable));
        }

        return
            new DataCenterPhysicalCableConnection(
                cable.CableID,
                Resolve(
                    cable.Start),
                Resolve(
                    cable.End));
    }

    private DataCenterPhysicalCableEndpointResolution ResolveServer(
        DataCenterCablePersistenceEndpoint endpoint)
    {
        if (
            endpoint.ServerID.Length > 0 &&
            _serverIds.Contains(
                endpoint.ServerID)
        )
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.Server,
                    endpoint.ServerID);
        }

        return
            Unknown(
                endpoint);
    }

    private DataCenterPhysicalCableEndpointResolution ResolveNetworkSide(
        DataCenterCablePersistenceEndpoint endpoint)
    {
        string id =
            endpoint.SwitchID;

        if (id.Length == 0)
        {
            return
                Unknown(
                    endpoint);
        }

        if (_routerIds.Contains(id))
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.Router,
                    id);
        }

        if (_firewallIds.Contains(id))
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.Firewall,
                    id);
        }

        if (_switchIds.Contains(id))
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.Switch,
                    id);
        }

        if (_patchPanelIds.Contains(id))
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.PatchPanel,
                    id);
        }

        string? patchPanelId =
            _patchPanelIds
                .Where(
                    value =>
                        id.StartsWith(
                            value + "_",
                            StringComparison.Ordinal))
                .OrderByDescending(
                    value =>
                        value.Length)
                .FirstOrDefault();

        if (
            !string.IsNullOrWhiteSpace(
                patchPanelId)
        )
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.PatchPanelPort,
                    id,
                    patchPanelId);
        }

        return
            Unknown(
                endpoint);
    }

    private DataCenterPhysicalCableEndpointResolution ResolveCustomer(
        DataCenterCablePersistenceEndpoint endpoint)
    {
        if (
            endpoint.CustomerID.HasValue &&
            _customerIds.Contains(
                endpoint.CustomerID.Value)
        )
        {
            return
                Resolved(
                    endpoint,
                    DataCenterPhysicalCableEndpointKind.CustomerBase,
                    endpoint.CustomerID.Value.ToString(
                        CultureInfo.InvariantCulture));
        }

        return
            Unknown(
                endpoint);
    }

    private static DataCenterPhysicalCableEndpointResolution Resolved(
        DataCenterCablePersistenceEndpoint endpoint,
        DataCenterPhysicalCableEndpointKind kind,
        string persistentId,
        string? parentPersistentId = null)
    {
        return
            new DataCenterPhysicalCableEndpointResolution(
                endpoint,
                kind,
                persistentId,
                parentPersistentId);
    }

    private static DataCenterPhysicalCableEndpointResolution Unknown(
        DataCenterCablePersistenceEndpoint endpoint)
    {
        return
            new DataCenterPhysicalCableEndpointResolution(
                endpoint,
                DataCenterPhysicalCableEndpointKind.Unknown,
                persistentId:
                    null,
                parentPersistentId:
                    null);
    }

    private static HashSet<string> NormalizeStrings(
        IEnumerable<string>? values)
    {
        return
            values is null
                ? new HashSet<string>(
                    StringComparer.Ordinal)
                : new HashSet<string>(
                    values
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value))
                        .Select(
                            value =>
                                value.Trim()),
                    StringComparer.Ordinal);
    }
}
