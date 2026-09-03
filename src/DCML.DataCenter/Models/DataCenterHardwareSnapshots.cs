using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

public enum DataCenterHardwareSnapshotSource
{
    SceneInstance = 0,
    ResourceDefinition = 1
}

public abstract class DataCenterHardwareSnapshot
{
    protected DataCenterHardwareSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        int? componentInstanceId = null)
    {
        InstanceId = instanceId;
        GameObjectInstanceId = instanceId;
        ComponentInstanceId =
            componentInstanceId ??
            instanceId;
        Name = name ?? string.Empty;
        SceneName = sceneName ?? string.Empty;
        IsResource = isResource;
    }

    public int InstanceId { get; }
    public int GameObjectInstanceId { get; }
    public int ComponentInstanceId { get; }
    public string Name { get; }
    public string SceneName { get; }
    public bool IsResource { get; }

    public DataCenterHardwareSnapshotSource Source =>
        IsResource
            ? DataCenterHardwareSnapshotSource.ResourceDefinition
            : DataCenterHardwareSnapshotSource.SceneInstance;
}

public sealed class DataCenterServerSnapshot : DataCenterHardwareSnapshot
{
    public DataCenterServerSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        string? ip,
        string? serverId,
        int? appId,
        double? currentProcessingSpeed,
        double? maxProcessingSpeed,
        bool? isOn,
        bool? isBroken,
        int? eolTime,
        int? serverType,
        int? componentInstanceId = null)
        : base(
            instanceId,
            name,
            sceneName,
            isResource,
            componentInstanceId)
    {
        IP = ip;
        ServerID = serverId;
        AppID = appId;
        CurrentProcessingSpeed = currentProcessingSpeed;
        MaxProcessingSpeed = maxProcessingSpeed;
        IsOn = isOn;
        IsBroken = isBroken;
        EolTime = eolTime;
        ServerType = serverType;
    }

    public string? IP { get; }
    public string? ServerID { get; }
    public int? AppID { get; }
    public double? CurrentProcessingSpeed { get; }
    public double? MaxProcessingSpeed { get; }
    public bool? IsOn { get; }
    public bool? IsBroken { get; }
    public int? EolTime { get; }
    public int? ServerType { get; }
}

public sealed class DataCenterRackSnapshot : DataCenterHardwareSnapshot
{
    public DataCenterRackSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        bool? arePositionsTurnedOff,
        double? targetVolume,
        int? componentInstanceId = null)
        : base(
            instanceId,
            name,
            sceneName,
            isResource,
            componentInstanceId)
    {
        ArePositionsTurnedOff = arePositionsTurnedOff;
        TargetVolume = targetVolume;
    }

    public bool? ArePositionsTurnedOff { get; }
    public double? TargetVolume { get; }
}

public sealed class DataCenterNetworkDeviceSnapshot : DataCenterHardwareSnapshot
{
    public DataCenterNetworkDeviceSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        string kind,
        int? portCount,
        bool? isOn,
        bool? isBroken,
        int? eolTime,
        string? switchId,
        int? switchType,
        bool? vlanBaselineEstablished,
        int? asn,
        int? nextRouteId,
        string? clusterIp,
        int? componentInstanceId = null)
        : base(
            instanceId,
            name,
            sceneName,
            isResource,
            componentInstanceId)
    {
        Kind = string.IsNullOrWhiteSpace(kind) ? "switch" : kind.Trim();
        PortCount = portCount;
        IsOn = isOn;
        IsBroken = isBroken;
        EolTime = eolTime;
        SwitchID = switchId;
        SwitchType = switchType;
        VlanBaselineEstablished = vlanBaselineEstablished;
        ASN = asn;
        NextRouteID = nextRouteId;
        ClusterIP = clusterIp;
    }

    public string Kind { get; }
    public int? PortCount { get; }
    public bool? IsOn { get; }
    public bool? IsBroken { get; }
    public int? EolTime { get; }
    public string? SwitchID { get; }
    public int? SwitchType { get; }
    public bool? VlanBaselineEstablished { get; }
    public int? ASN { get; }
    public int? NextRouteID { get; }
    public string? ClusterIP { get; }
}

public sealed class DataCenterSfpModuleSnapshot : DataCenterHardwareSnapshot
{
    public DataCenterSfpModuleSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        double? speed,
        int? sfpType,
        int? positionInBox,
        bool? isInTheBox,
        DataCenterHardwareReference? link = null,
        int? componentInstanceId = null)
        : base(
            instanceId,
            name,
            sceneName,
            isResource,
            componentInstanceId)
    {
        Speed = speed;
        SfpType = sfpType;
        PositionInBox = positionInBox;
        IsInTheBox = isInTheBox;
        Link = link;
    }

    public double? Speed { get; }
    public int? SfpType { get; }
    public int? PositionInBox { get; }
    public bool? IsInTheBox { get; }
    public DataCenterHardwareReference? Link { get; }
}

public sealed class DataCenterCableSnapshot : DataCenterHardwareSnapshot
{
    public DataCenterCableSnapshot(
        int instanceId,
        string? name,
        string? sceneName,
        bool isResource,
        int? customerId,
        int? cableIdsOnLink,
        double? connectionSpeed,
        bool? isEndPoint,
        bool? isFibrePort,
        bool? isSfpPort,
        bool? isStartOrEnd,
        int? sfpTypeInserted,
        int? sfpTypeSupported,
        string? switchId,
        string? typeOfLink,
        DataCenterHardwareReference? insertedSfp = null,
        DataCenterHardwareReference? parentInternet = null,
        DataCenterHardwareReference? parentPatchPanel = null,
        DataCenterHardwareReference? parentServer = null,
        DataCenterHardwareReference? parentSwitch = null,
        int? componentInstanceId = null)
        : base(
            instanceId,
            name,
            sceneName,
            isResource,
            componentInstanceId)
    {
        CustomerID = customerId;
        CableIDsOnLink = cableIdsOnLink;
        ConnectionSpeed = connectionSpeed;
        IsEndPoint = isEndPoint;
        IsFibrePort = isFibrePort;
        IsSfpPort = isSfpPort;
        IsStartOrEnd = isStartOrEnd;
        SfpTypeInserted = sfpTypeInserted;
        SfpTypeSupported = sfpTypeSupported;
        SwitchID = switchId;
        TypeOfLink = typeOfLink;
        InsertedSfp = insertedSfp;
        ParentInternet = parentInternet;
        ParentPatchPanel = parentPatchPanel;
        ParentServer = parentServer;
        ParentSwitch = parentSwitch;
    }

    public int? CustomerID { get; }
    public int? CableIDsOnLink { get; }
    public double? ConnectionSpeed { get; }
    public bool? IsEndPoint { get; }
    public bool? IsFibrePort { get; }
    public bool? IsSfpPort { get; }
    public bool? IsStartOrEnd { get; }
    public int? SfpTypeInserted { get; }
    public int? SfpTypeSupported { get; }
    public string? SwitchID { get; }
    public string? TypeOfLink { get; }
    public DataCenterHardwareReference? InsertedSfp { get; }
    public DataCenterHardwareReference? ParentInternet { get; }
    public DataCenterHardwareReference? ParentPatchPanel { get; }
    public DataCenterHardwareReference? ParentServer { get; }
    public DataCenterHardwareReference? ParentSwitch { get; }
}

public sealed class DataCenterHardwareSnapshotSet
{
    private readonly IReadOnlyList<DataCenterServerSnapshot> _servers;
    private readonly IReadOnlyList<DataCenterRackSnapshot> _racks;
    private readonly IReadOnlyList<DataCenterNetworkDeviceSnapshot> _networkDevices;
    private readonly IReadOnlyList<DataCenterSfpModuleSnapshot> _sfpModules;
    private readonly IReadOnlyList<DataCenterCableSnapshot> _cables;

    public DataCenterHardwareSnapshotSet(
        IEnumerable<DataCenterServerSnapshot>? servers,
        IEnumerable<DataCenterRackSnapshot>? racks,
        IEnumerable<DataCenterNetworkDeviceSnapshot>? networkDevices,
        IEnumerable<DataCenterSfpModuleSnapshot>? sfpModules,
        IEnumerable<DataCenterCableSnapshot>? cables)
    {
        _servers = servers?.ToArray() ?? Array.Empty<DataCenterServerSnapshot>();
        _racks = racks?.ToArray() ?? Array.Empty<DataCenterRackSnapshot>();
        _networkDevices = networkDevices?.ToArray() ?? Array.Empty<DataCenterNetworkDeviceSnapshot>();
        _sfpModules = sfpModules?.ToArray() ?? Array.Empty<DataCenterSfpModuleSnapshot>();
        _cables = cables?.ToArray() ?? Array.Empty<DataCenterCableSnapshot>();
    }

    public IReadOnlyList<DataCenterServerSnapshot> Servers => _servers;
    public IReadOnlyList<DataCenterRackSnapshot> Racks => _racks;
    public IReadOnlyList<DataCenterNetworkDeviceSnapshot> NetworkDevices => _networkDevices;
    public IReadOnlyList<DataCenterSfpModuleSnapshot> SfpModules => _sfpModules;
    public IReadOnlyList<DataCenterCableSnapshot> Cables => _cables;

    public IReadOnlyList<DataCenterServerSnapshot> ServerDefinitions =>
        _servers.Where(value => value.IsResource).ToArray();

    public IReadOnlyList<DataCenterServerSnapshot> ServerInstances =>
        _servers.Where(value => !value.IsResource).ToArray();

    public IReadOnlyList<DataCenterRackSnapshot> RackDefinitions =>
        _racks.Where(value => value.IsResource).ToArray();

    public IReadOnlyList<DataCenterRackSnapshot> RackInstances =>
        _racks.Where(value => !value.IsResource).ToArray();

    public IReadOnlyList<DataCenterNetworkDeviceSnapshot> NetworkDeviceDefinitions =>
        _networkDevices.Where(value => value.IsResource).ToArray();

    public IReadOnlyList<DataCenterNetworkDeviceSnapshot> NetworkDeviceInstances =>
        _networkDevices.Where(value => !value.IsResource).ToArray();

    public IReadOnlyList<DataCenterSfpModuleSnapshot> SfpModuleDefinitions =>
        _sfpModules.Where(value => value.IsResource).ToArray();

    public IReadOnlyList<DataCenterSfpModuleSnapshot> SfpModuleInstances =>
        _sfpModules.Where(value => !value.IsResource).ToArray();

    public IReadOnlyList<DataCenterCableSnapshot> CableDefinitions =>
        _cables.Where(value => value.IsResource).ToArray();

    public IReadOnlyList<DataCenterCableSnapshot> CableInstances =>
        _cables.Where(value => !value.IsResource).ToArray();
}
