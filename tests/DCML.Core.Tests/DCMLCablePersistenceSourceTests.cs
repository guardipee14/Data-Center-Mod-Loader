using System;
using System.IO;
using System.Threading.Tasks;
using DCML.DataCenter;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLCablePersistenceSourceTests
{
    [Fact]
    public void Snapshot_ReportsResolutionCounts()
    {
        var snapshot =
            new DataCenterCablePersistenceSnapshot(
                "C:\\saves\\known.save",
                1234,
                DateTime.UtcNow,
                new[]
                {
                    new DataCenterCablePersistenceRecord(
                        1,
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.Start,
                            1,
                            "Server_1",
                            string.Empty,
                            7),
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.End,
                            2,
                            string.Empty,
                            "Router_1",
                            -1))
                },
                new[] { "Server_1" },
                new[] { "Router_1" },
                new[] { "Router_1" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { 7 });

        Assert.Equal(1, snapshot.CableCount);
        Assert.Equal(2, snapshot.EndpointCount);
        Assert.Equal(2, snapshot.ResolvedEndpointCount);
        Assert.Equal(0, snapshot.UnresolvedEndpointCount);
        Assert.True(snapshot.IsFullyResolved);
    }

    [Fact]
    public async Task Topology_CaptureAsyncCombinesExplicitPersistenceSource()
    {
        var persistence =
            new DataCenterCablePersistenceSnapshot(
                "C:\\saves\\known.save",
                1000,
                DateTime.UtcNow,
                new[]
                {
                    new DataCenterCablePersistenceRecord(
                        831,
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.Start,
                            3,
                            string.Empty,
                            string.Empty,
                            0),
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.End,
                            2,
                            string.Empty,
                            "Router_1",
                            -1))
                },
                Array.Empty<string>(),
                new[] { "Router_1" },
                new[] { "Router_1" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { 0 });

        var topology =
            new DataCenterHardwareTopology(
                new EmptySnapshots(),
                componentStateReader: null,
                persistenceSource:
                    new FixedPersistenceSource(persistence));

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(graph.NetworkConnectionEdges);

        Assert.Equal(831, edge.PhysicalCableID);
        Assert.True(edge.IsBidirectional);
        Assert.Equal("0", edge.Source.PersistentID);
        Assert.Equal("Router_1", edge.Target.PersistentID);
    }

    [Fact]
    public async Task Topology_CaptureAsyncWithoutPersistenceSourceRemainsLiveOnly()
    {
        var topology =
            new DataCenterHardwareTopology(
                new EmptySnapshots());

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        Assert.Empty(graph.NetworkConnectionEdges);
    }

    [Fact]
    public void DataCenterProject_DoesNotReferenceNrbfPackages()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", ".."));

        string project =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.DataCenter",
                    "DCML.DataCenter.csproj"));

        Assert.DoesNotContain(
            "System.Formats.Nrbf",
            project);

        Assert.DoesNotContain(
            "System.Reflection.Metadata",
            project);
    }

    [Fact]
    public void PersistenceSourceInterface_IsDecoderAgnostic()
    {
        Assert.NotNull(
            typeof(IDataCenterCablePersistenceSource)
                .GetProperty("SourcePath"));

        Assert.NotNull(
            typeof(IDataCenterCablePersistenceSource)
                .GetMethod("ReadAsync"));
    }

    [Fact]
    public void Snapshot_RejectsMissingSourcePath()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DataCenterCablePersistenceSnapshot(
                    " ",
                    0,
                    DateTime.UtcNow,
                    Array.Empty<DataCenterCablePersistenceRecord>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<int>()));
    }

    [Fact]
    public void Snapshot_PreservesExplicitSourceMetadata()
    {
        DateTime timestamp =
            new DateTime(
                2026,
                9,
                3,
                7,
                0,
                0,
                DateTimeKind.Utc);

        var snapshot =
            new DataCenterCablePersistenceSnapshot(
                "C:\\saves\\known.save",
                1168415,
                timestamp,
                Array.Empty<DataCenterCablePersistenceRecord>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<int>());

        Assert.Equal(1168415, snapshot.SourceLength);
        Assert.Equal(timestamp, snapshot.SourceLastWriteTimeUtc);
    }

    private static DataCenterCablePersistenceEndpoint Endpoint(
        DataCenterPhysicalCableEndpointSide side,
        int type,
        string serverId,
        string switchId,
        int? customerId)
    {
        return new DataCenterCablePersistenceEndpoint(
            side,
            type,
            serverId,
            switchId,
            customerId);
    }

    private sealed class EmptySnapshots :
        IDataCenterHardwareSnapshots
    {
        public Task<DataCenterHardwareSnapshotSet> CaptureAsync(
            DataCenterHardwareSnapshotQuery query)
        {
            return Task.FromResult(
                new DataCenterHardwareSnapshotSet(
                    Array.Empty<DataCenterServerSnapshot>(),
                    Array.Empty<DataCenterRackSnapshot>(),
                    Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                    Array.Empty<DataCenterSfpModuleSnapshot>(),
                    Array.Empty<DataCenterCableSnapshot>()));
        }
    }

    private sealed class FixedPersistenceSource :
        IDataCenterCablePersistenceSource
    {
        private readonly DataCenterCablePersistenceSnapshot _snapshot;

        public FixedPersistenceSource(
            DataCenterCablePersistenceSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public string SourcePath => _snapshot.SourcePath;

        public Task<DataCenterCablePersistenceSnapshot> ReadAsync()
        {
            return Task.FromResult(_snapshot);
        }
    }
}
