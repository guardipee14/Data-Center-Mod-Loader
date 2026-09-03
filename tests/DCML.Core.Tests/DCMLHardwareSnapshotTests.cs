using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLHardwareSnapshotTests
{
    [Fact]
    public void Capability_HasStableIdentifier()
    {
        Assert.Equal(
            "dcml.game.component-state",
            DCMLRuntimeCapabilities.GameComponentState);
    }

    [Fact]
    public void ComponentStateQuery_RequiresComponentType()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLGameComponentStateQuery(
                    " "));
    }

    [Fact]
    public void ComponentStateQuery_NormalizesMembers()
    {
        var query =
            new DCMLGameComponentStateQuery(
                " Il2Cpp.Server ",
                new[]
                {
                    " IP ",
                    "ip",
                    " isOn "
                },
                sceneName:
                    " BaseScene ",
                scope:
                    DCMLGameComponentScope.Both,
                maxResults:
                    12,
                skipResults:
                    2);

        Assert.Equal(
            "Il2Cpp.Server",
            query.ComponentTypeName);

        Assert.Equal(
            new[]
            {
                "IP",
                "isOn"
            },
            query.MemberNames);

        Assert.Equal(
            "BaseScene",
            query.SceneName);

        Assert.Equal(
            12,
            query.MaxResults);

        Assert.Equal(
            2,
            query.SkipResults);
    }

    [Fact]
    public void ComponentStateQuery_RejectsInvalidBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameComponentStateQuery(
                    "Il2Cpp.Server",
                    maxResults:
                        0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameComponentStateQuery(
                    "Il2Cpp.Server",
                    skipResults:
                        -1));
    }

    [Fact]
    public void GameValue_PreservesScalarData()
    {
        var text =
            new DCMLGameValue(
                DCMLGameValueKind.String,
                "System.String",
                stringValue:
                    "10.0.0.5");

        var number =
            new DCMLGameValue(
                DCMLGameValueKind.Number,
                "System.Single",
                numberValue:
                    12.5);

        var boolean =
            new DCMLGameValue(
                DCMLGameValueKind.Boolean,
                "System.Boolean",
                booleanValue:
                    true);

        Assert.Equal(
            "10.0.0.5",
            text.StringValue);

        Assert.Equal(
            12.5,
            number.NumberValue);

        Assert.Equal(
            true,
            boolean.BooleanValue);
    }

    [Fact]
    public void ComponentState_CopiesValuesCaseInsensitively()
    {
        var state =
            CreateState(
                "Il2Cpp.Server",
                ("IP", StringValue("10.0.0.1")));

        Assert.True(
            state.Values.ContainsKey(
                "ip"));

        Assert.Equal(
            "10.0.0.1",
            state.Values["IP"].StringValue);
    }

    [Fact]
    public void HardwareQuery_RequiresAtLeastOneSource()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DataCenterHardwareSnapshotQuery(
                    includeSceneObjects:
                        false,
                    includeResources:
                        false));
    }

    [Fact]
    public void HardwareQuery_UsesExpectedDefaults()
    {
        var query =
            new DataCenterHardwareSnapshotQuery(
                sceneName:
                    "BaseScene");

        Assert.Equal(
            "BaseScene",
            query.SceneName);

        Assert.True(
            query.IncludeSceneObjects);

        Assert.True(
            query.IncludeResources);

        Assert.Equal(
            64,
            query.MaxPerType);
    }

    [Fact]
    public async Task HardwareSnapshots_MapsServerScalarState()
    {
        var reader =
            new FakeReader(
                new Dictionary<string, IReadOnlyList<DCMLGameComponentState>>
                {
                    ["Il2Cpp.Server"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.Server",
                                ("IP", StringValue("10.0.0.2")),
                                ("ServerID", StringValue("srv-2")),
                                ("appID", IntegerValue(7)),
                                ("currentProcessingSpeed", NumberValue(22.5)),
                                ("maxProcessingSpeed", NumberValue(100)),
                                ("isOn", BooleanValue(true)),
                                ("isBroken", BooleanValue(false)),
                                ("eolTime", IntegerValue(900)),
                                ("serverType", IntegerValue(3)))
                        }
                });

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet snapshot =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        DataCenterServerSnapshot server =
            Assert.Single(
                snapshot.Servers);

        Assert.Equal("10.0.0.2", server.IP);
        Assert.Equal("srv-2", server.ServerID);
        Assert.Equal(7, server.AppID);
        Assert.Equal(22.5d, server.CurrentProcessingSpeed);
        Assert.Equal(100d, server.MaxProcessingSpeed);
        Assert.True(server.IsOn == true);
        Assert.True(server.IsBroken == false);
    }

    [Fact]
    public async Task HardwareSnapshots_MapsSwitchRouterAndFirewallKinds()
    {
        var reader =
            new FakeReader(
                new Dictionary<string, IReadOnlyList<DCMLGameComponentState>>
                {
                    ["Il2Cpp.NetworkSwitch"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.NetworkSwitch",
                                ("PortCount", IntegerValue(16)),
                                ("switchId", StringValue("sw-1")))
                        },
                    ["Il2Cpp.Router"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.Router",
                                ("PortCount", IntegerValue(20)),
                                ("switchId", StringValue("r-1")),
                                ("asn", IntegerValue(64512)),
                                ("nextRouteId", IntegerValue(4)))
                        },
                    ["Il2Cpp.Firewall"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.Firewall",
                                ("PortCount", IntegerValue(20)),
                                ("switchId", StringValue("fw-1")),
                                ("clusterIP", StringValue("10.0.0.254")))
                        }
                });

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet snapshot =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        Assert.Equal(
            new[]
            {
                "firewall",
                "router",
                "switch"
            },
            snapshot.NetworkDevices
                .Select(
                    value =>
                        value.Kind)
                .OrderBy(
                    value =>
                        value)
                .ToArray());

        Assert.Equal(
            64512,
            snapshot.NetworkDevices
                .Single(value => value.Kind == "router")
                .ASN);

        Assert.Equal(
            "10.0.0.254",
            snapshot.NetworkDevices
                .Single(value => value.Kind == "firewall")
                .ClusterIP);
    }

    [Fact]
    public async Task HardwareSnapshots_MapsRackSfpAndCableState()
    {
        var reader =
            new FakeReader(
                new Dictionary<string, IReadOnlyList<DCMLGameComponentState>>
                {
                    ["Il2Cpp.Rack"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.Rack",
                                ("arePositionTurnedOff", BooleanValue(false)),
                                ("targetVolume", NumberValue(0.75)))
                        },
                    ["Il2Cpp.SFPModule"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.SFPModule",
                                ("speed", NumberValue(10)),
                                ("sfpType", IntegerValue(2)),
                                ("positionInBox", IntegerValue(3)),
                                ("isInTheBox", BooleanValue(true)))
                        },
                    ["Il2Cpp.CableLink"] =
                        new[]
                        {
                            CreateState(
                                "Il2Cpp.CableLink",
                                ("CustomerID", IntegerValue(11)),
                                ("connectionSpeed", NumberValue(10)),
                                ("isFibrePort", BooleanValue(true)),
                                ("isSFPPort", BooleanValue(true)),
                                ("typeOfLink", EnumValue("Fiber")))
                        }
                });

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet snapshot =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        Assert.True(
            Assert.Single(snapshot.Racks).ArePositionsTurnedOff == false);

        Assert.Equal(
            10d,
            Assert.Single(snapshot.SfpModules).Speed);

        DataCenterCableSnapshot cable =
            Assert.Single(
                snapshot.Cables);

        Assert.True(cable.IsFibrePort == true);
        Assert.True(cable.IsSfpPort == true);
        Assert.Equal("Fiber", cable.TypeOfLink);
    }

    [Fact]
    public async Task HardwareSnapshots_PassesBothScopeAndRequestedScene()
    {
        var reader =
            new FakeReader(
                new Dictionary<string, IReadOnlyList<DCMLGameComponentState>>());

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        await service.CaptureAsync(
            new DataCenterHardwareSnapshotQuery(
                sceneName:
                    "BaseScene",
                includeSceneObjects:
                    true,
                includeResources:
                    true,
                maxPerType:
                    9));

        Assert.NotEmpty(
            reader.Queries);

        Assert.All(
            reader.Queries,
            query =>
            {
                Assert.Equal(
                    DCMLGameComponentScope.Both,
                    query.Scope);

                Assert.Equal(
                    "BaseScene",
                    query.SceneName);

                Assert.Equal(
                    9,
                    query.MaxResults);
            });
    }

    private static DCMLGameComponentState CreateState(
        string componentTypeName,
        params (string Name, DCMLGameValue Value)[] values)
    {
        return new DCMLGameComponentState(
            instanceId:
                1,
            name:
                "Object",
            sceneName:
                "BaseScene",
            hierarchyPath:
                "Objects/Object",
            activeInHierarchy:
                true,
            isResource:
                false,
            componentTypeName:
                componentTypeName,
            values:
                values.Select(
                    value =>
                        new KeyValuePair<string, DCMLGameValue>(
                            value.Name,
                            value.Value)));
    }

    private static DCMLGameValue StringValue(
        string value)
    {
        return new DCMLGameValue(
            DCMLGameValueKind.String,
            "System.String",
            stringValue:
                value);
    }

    private static DCMLGameValue BooleanValue(
        bool value)
    {
        return new DCMLGameValue(
            DCMLGameValueKind.Boolean,
            "System.Boolean",
            booleanValue:
                value);
    }

    private static DCMLGameValue IntegerValue(
        long value)
    {
        return new DCMLGameValue(
            DCMLGameValueKind.Integer,
            "System.Int32",
            integerValue:
                value);
    }

    private static DCMLGameValue NumberValue(
        double value)
    {
        return new DCMLGameValue(
            DCMLGameValueKind.Number,
            "System.Single",
            numberValue:
                value);
    }

    private static DCMLGameValue EnumValue(
        string value)
    {
        return new DCMLGameValue(
            DCMLGameValueKind.Enum,
            "Il2Cpp.CableLink+TypeOfLink",
            stringValue:
                value);
    }

    private sealed class FakeReader :
        IDCMLGameComponentStateReader
    {
        private readonly IReadOnlyDictionary<
            string,
            IReadOnlyList<DCMLGameComponentState>>
            _states;

        public FakeReader(
            IReadOnlyDictionary<
                string,
                IReadOnlyList<DCMLGameComponentState>>
                states)
        {
            _states =
                states;
        }

        public List<DCMLGameComponentStateQuery> Queries { get; } =
            new List<DCMLGameComponentStateQuery>();

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            Queries.Add(
                query);

            if (_states.TryGetValue(
                    query.ComponentTypeName,
                    out IReadOnlyList<DCMLGameComponentState>? values))
            {
                return Task.FromResult(values);
            }

            return Task.FromResult<IReadOnlyList<DCMLGameComponentState>>(
                Array.Empty<DCMLGameComponentState>());
        }
    }
}
