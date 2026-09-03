using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLHardwareDefinitionInstanceSplitTests
{
    [Fact]
    public void SnapshotSource_ReportsResourceDefinition()
    {
        Assert.Equal(
            DataCenterHardwareSnapshotSource.ResourceDefinition,
            CreateServer(true).Source);
    }

    [Fact]
    public void SnapshotSource_ReportsSceneInstance()
    {
        Assert.Equal(
            DataCenterHardwareSnapshotSource.SceneInstance,
            CreateServer(false).Source);
    }

    [Fact]
    public void SnapshotSet_SplitsServers()
    {
        var set = CreateSet();
        Assert.Single(set.ServerDefinitions);
        Assert.Single(set.ServerInstances);
    }

    [Fact]
    public void SnapshotSet_SplitsRacks()
    {
        var set = CreateSet();
        Assert.Single(set.RackDefinitions);
        Assert.Single(set.RackInstances);
    }

    [Fact]
    public void SnapshotSet_SplitsNetworkDevices()
    {
        var set = CreateSet();
        Assert.Single(set.NetworkDeviceDefinitions);
        Assert.Single(set.NetworkDeviceInstances);
    }

    [Fact]
    public void SnapshotSet_SplitsSfpModules()
    {
        var set = CreateSet();
        Assert.Single(set.SfpModuleDefinitions);
        Assert.Single(set.SfpModuleInstances);
    }

    [Fact]
    public void SnapshotSet_SplitsCables()
    {
        var set = CreateSet();
        Assert.Single(set.CableDefinitions);
        Assert.Single(set.CableInstances);
    }

    [Fact]
    public void SnapshotSet_PreservesCombinedCollections()
    {
        var set = CreateSet();
        Assert.Equal(2, set.Servers.Count);
        Assert.Equal(2, set.Racks.Count);
        Assert.Equal(2, set.NetworkDevices.Count);
        Assert.Equal(2, set.SfpModules.Count);
        Assert.Equal(2, set.Cables.Count);
    }

    private static DataCenterHardwareSnapshotSet CreateSet()
    {
        return new DataCenterHardwareSnapshotSet(
            new[]
            {
                CreateServer(true),
                CreateServer(false)
            },
            new[]
            {
                new DataCenterRackSnapshot(1, "rack-def", "", true, false, 0),
                new DataCenterRackSnapshot(2, "rack-live", "BaseScene", false, false, 0)
            },
            new[]
            {
                new DataCenterNetworkDeviceSnapshot(
                    1, "switch-def", "", true, "switch",
                    16, false, false, 0, "sw-def", 0, false,
                    null, null, null),
                new DataCenterNetworkDeviceSnapshot(
                    2, "switch-live", "BaseScene", false, "switch",
                    16, true, false, 0, "sw-live", 0, true,
                    null, null, null)
            },
            new[]
            {
                new DataCenterSfpModuleSnapshot(1, "sfp-def", "", true, 2, 0, 0, true),
                new DataCenterSfpModuleSnapshot(2, "sfp-live", "BaseScene", false, 2, 0, 0, false)
            },
            new[]
            {
                new DataCenterCableSnapshot(
                    1, "cable-def", "", true,
                    0, 0, 2, true, false, false, true,
                    0, 0, "", "None"),
                new DataCenterCableSnapshot(
                    2, "cable-live", "BaseScene", false,
                    0, 0, 2, true, false, false, true,
                    0, 0, "", "None")
            });
    }

    private static DataCenterServerSnapshot CreateServer(bool resource)
    {
        return new DataCenterServerSnapshot(
            1,
            resource ? "server-def" : "server-live",
            resource ? "" : "BaseScene",
            resource,
            "0.0.0.0",
            "",
            0,
            0,
            0.05,
            false,
            false,
            0,
            0);
    }
}
