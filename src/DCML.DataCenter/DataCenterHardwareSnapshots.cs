using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

public sealed class DataCenterHardwareSnapshots :
    IDataCenterHardwareSnapshots
{
    private static readonly string[] ServerMembers =
    {
        "IP", "ServerID", "appID", "currentProcessingSpeed",
        "maxProcessingSpeed", "isOn", "isBroken", "eolTime", "serverType"
    };

    private static readonly string[] RackMembers =
    {
        "arePositionTurnedOff", "targetVolume"
    };

    private static readonly string[] SwitchMembers =
    {
        "PortCount", "isOn", "isBroken", "eolTime",
        "switchId", "switchType", "vlanBaselineEstablished"
    };

    private static readonly string[] RouterMembers =
    {
        "PortCount", "isOn", "isBroken", "eolTime",
        "switchId", "switchType", "vlanBaselineEstablished",
        "asn", "nextRouteId"
    };

    private static readonly string[] FirewallMembers =
    {
        "PortCount", "isOn", "isBroken", "eolTime",
        "switchId", "switchType", "vlanBaselineEstablished", "clusterIP"
    };

    private static readonly string[] SfpMembers =
    {
        "speed", "sfpType", "positionInBox", "isInTheBox", "link"
    };

    private static readonly string[] CableMembers =
    {
        "CustomerID", "cableIDsOnLink", "connectionSpeed",
        "isEndPoint", "isFibrePort", "isSFPPort", "isStartOrEnd",
        "sfpTypeInserted", "sfpTypeSupported", "switchID", "typeOfLink",
        "insertedSFP", "parentInternet", "parentPatchPanel",
        "parentServer", "parentSwitch"
    };

    private readonly IDCMLGameComponentStateReader _reader;

    public DataCenterHardwareSnapshots(
        IDCMLGameComponentStateReader reader)
    {
        _reader =
            reader ??
            throw new ArgumentNullException(
                nameof(reader));
    }

    public async Task<DataCenterHardwareSnapshotSet> CaptureAsync(
        DataCenterHardwareSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        DCMLGameComponentScope scope =
            GetScope(query);

        IReadOnlyList<DCMLGameComponentState> servers =
            await ReadAsync(
                "Il2Cpp.Server",
                ServerMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> racks =
            await ReadAsync(
                "Il2Cpp.Rack",
                RackMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> switches =
            await ReadAsync(
                "Il2Cpp.NetworkSwitch",
                SwitchMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> routers =
            await ReadAsync(
                "Il2Cpp.Router",
                RouterMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> firewalls =
            await ReadAsync(
                "Il2Cpp.Firewall",
                FirewallMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> sfpModules =
            await ReadAsync(
                "Il2Cpp.SFPModule",
                SfpMembers,
                query,
                scope).ConfigureAwait(false);

        IReadOnlyList<DCMLGameComponentState> cables =
            await ReadAsync(
                "Il2Cpp.CableLink",
                CableMembers,
                query,
                scope).ConfigureAwait(false);

        return new DataCenterHardwareSnapshotSet(
            servers.Select(CreateServer),
            racks.Select(CreateRack),
            switches.Select(value => CreateNetworkDevice(value, "switch"))
                .Concat(routers.Select(value => CreateNetworkDevice(value, "router")))
                .Concat(firewalls.Select(value => CreateNetworkDevice(value, "firewall")))
                .OrderBy(value => value.IsResource)
                .ThenBy(value => value.SceneName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Name, StringComparer.OrdinalIgnoreCase),
            sfpModules.Select(CreateSfp),
            cables.Select(CreateCable));
    }

    private Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
        string componentTypeName,
        IEnumerable<string> members,
        DataCenterHardwareSnapshotQuery query,
        DCMLGameComponentScope scope)
    {
        return _reader.ReadAsync(
            new DCMLGameComponentStateQuery(
                componentTypeName: componentTypeName,
                memberNames: members,
                sceneName: query.SceneName,
                scope: scope,
                includeInactive: true,
                maxResults: query.MaxPerType));
    }

    private static DCMLGameComponentScope GetScope(
        DataCenterHardwareSnapshotQuery query)
    {
        if (query.IncludeSceneObjects && query.IncludeResources)
        {
            return DCMLGameComponentScope.Both;
        }

        return query.IncludeResources
            ? DCMLGameComponentScope.Resource
            : DCMLGameComponentScope.Scene;
    }

    private static DataCenterServerSnapshot CreateServer(
        DCMLGameComponentState state)
    {
        return new DataCenterServerSnapshot(
            state.InstanceId,
            state.Name,
            state.SceneName,
            state.IsResource,
            GetString(state, "IP"),
            GetString(state, "ServerID"),
            GetInt(state, "appID"),
            GetNumber(state, "currentProcessingSpeed"),
            GetNumber(state, "maxProcessingSpeed"),
            GetBoolean(state, "isOn"),
            GetBoolean(state, "isBroken"),
            GetInt(state, "eolTime"),
            GetInt(state, "serverType"),
            state.ComponentInstanceId);
    }

    private static DataCenterRackSnapshot CreateRack(
        DCMLGameComponentState state)
    {
        return new DataCenterRackSnapshot(
            state.InstanceId,
            state.Name,
            state.SceneName,
            state.IsResource,
            GetBoolean(state, "arePositionTurnedOff"),
            GetNumber(state, "targetVolume"),
            state.ComponentInstanceId);
    }

    private static DataCenterNetworkDeviceSnapshot CreateNetworkDevice(
        DCMLGameComponentState state,
        string kind)
    {
        return new DataCenterNetworkDeviceSnapshot(
            state.InstanceId,
            state.Name,
            state.SceneName,
            state.IsResource,
            kind,
            GetInt(state, "PortCount"),
            GetBoolean(state, "isOn"),
            GetBoolean(state, "isBroken"),
            GetInt(state, "eolTime"),
            GetString(state, "switchId"),
            GetInt(state, "switchType"),
            GetBoolean(state, "vlanBaselineEstablished"),
            GetInt(state, "asn"),
            GetInt(state, "nextRouteId"),
            GetString(state, "clusterIP"),
            state.ComponentInstanceId);
    }

    private static DataCenterSfpModuleSnapshot CreateSfp(
        DCMLGameComponentState state)
    {
        return new DataCenterSfpModuleSnapshot(
            state.InstanceId,
            state.Name,
            state.SceneName,
            state.IsResource,
            GetNumber(state, "speed"),
            GetInt(state, "sfpType"),
            GetInt(state, "positionInBox"),
            GetBoolean(state, "isInTheBox"),
            GetReference(state, "link"),
            state.ComponentInstanceId);
    }

    private static DataCenterCableSnapshot CreateCable(
        DCMLGameComponentState state)
    {
        return new DataCenterCableSnapshot(
            state.InstanceId,
            state.Name,
            state.SceneName,
            state.IsResource,
            GetInt(state, "CustomerID"),
            GetInt(state, "cableIDsOnLink"),
            GetNumber(state, "connectionSpeed"),
            GetBoolean(state, "isEndPoint"),
            GetBoolean(state, "isFibrePort"),
            GetBoolean(state, "isSFPPort"),
            GetBoolean(state, "isStartOrEnd"),
            GetInt(state, "sfpTypeInserted"),
            GetInt(state, "sfpTypeSupported"),
            GetString(state, "switchID"),
            GetString(state, "typeOfLink"),
            GetReference(state, "insertedSFP"),
            GetReference(state, "parentInternet"),
            GetReference(state, "parentPatchPanel"),
            GetReference(state, "parentServer"),
            GetReference(state, "parentSwitch"),
            state.ComponentInstanceId);
    }

    private static DataCenterHardwareReference? GetReference(
        DCMLGameComponentState state,
        string name)
    {
        if (
            !state.Values.TryGetValue(
                name,
                out DCMLGameValue? value) ||
            value.Kind !=
                DCMLGameValueKind.Reference
        )
        {
            return null;
        }

        return
            DataCenterHardwareReference.FromCore(
                value.ReferenceValue);
    }

    private static string? GetString(
        DCMLGameComponentState state,
        string name)
    {
        if (!state.Values.TryGetValue(name, out DCMLGameValue? value))
        {
            return null;
        }

        return value.Kind == DCMLGameValueKind.String ||
               value.Kind == DCMLGameValueKind.Enum
            ? value.StringValue
            : null;
    }

    private static bool? GetBoolean(
        DCMLGameComponentState state,
        string name)
    {
        return state.Values.TryGetValue(name, out DCMLGameValue? value) &&
               value.Kind == DCMLGameValueKind.Boolean
            ? value.BooleanValue
            : null;
    }

    private static int? GetInt(
        DCMLGameComponentState state,
        string name)
    {
        if (!state.Values.TryGetValue(name, out DCMLGameValue? value) ||
            value.Kind != DCMLGameValueKind.Integer ||
            value.IntegerValue is null ||
            value.IntegerValue < int.MinValue ||
            value.IntegerValue > int.MaxValue)
        {
            return null;
        }

        return (int)value.IntegerValue.Value;
    }

    private static double? GetNumber(
        DCMLGameComponentState state,
        string name)
    {
        if (!state.Values.TryGetValue(name, out DCMLGameValue? value))
        {
            return null;
        }

        if (value.Kind == DCMLGameValueKind.Number)
        {
            return value.NumberValue;
        }

        if (value.Kind == DCMLGameValueKind.Integer &&
            value.IntegerValue is not null)
        {
            return value.IntegerValue.Value;
        }

        return null;
    }
}
