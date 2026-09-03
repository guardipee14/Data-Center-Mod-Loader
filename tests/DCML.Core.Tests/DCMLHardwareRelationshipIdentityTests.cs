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

public sealed class DCMLHardwareRelationshipIdentityTests
{
    [Fact]
    public void ReferenceKind_IsAdditiveStableValue()
    {
        Assert.Equal(
            8,
            (int) DCMLGameValueKind.Reference);
    }

    [Fact]
    public void GameReference_PreservesHostNeutralIdentity()
    {
        var reference =
            new DCMLGameReference(
                42,
                "CableLink (42)",
                " Il2Cpp.CableLink ");

        Assert.Equal(
            42,
            reference.InstanceId);

        Assert.Equal(
            "CableLink (42)",
            reference.Name);

        Assert.Equal(
            "Il2Cpp.CableLink",
            reference.TypeName);
    }

    [Fact]
    public void GameValue_PreservesReferenceValue()
    {
        var reference =
            new DCMLGameReference(
                7,
                "Server",
                "Il2Cpp.Server");

        var value =
            new DCMLGameValue(
                DCMLGameValueKind.Reference,
                "Il2Cpp.Server",
                referenceValue:
                    reference);

        Assert.Same(
            reference,
            value.ReferenceValue);
    }

    [Fact]
    public void SfpSnapshot_PreservesLinkRelationship()
    {
        var link =
            new DataCenterHardwareReference(
                9,
                "CableLink (9)",
                "Il2Cpp.CableLink");

        var snapshot =
            new DataCenterSfpModuleSnapshot(
                1,
                "SFP_RJ45",
                "BaseScene",
                false,
                2,
                0,
                0,
                false,
                link);

        Assert.Same(
            link,
            snapshot.Link);
    }

    [Fact]
    public void CableSnapshot_PreservesParentRelationships()
    {
        var server =
            new DataCenterHardwareReference(
                2,
                "Server",
                "Il2Cpp.Server");

        var networkSwitch =
            new DataCenterHardwareReference(
                3,
                "Switch",
                "Il2Cpp.NetworkSwitch");

        var snapshot =
            new DataCenterCableSnapshot(
                1,
                "Cable",
                "BaseScene",
                false,
                0,
                0,
                2,
                true,
                false,
                false,
                true,
                0,
                0,
                "",
                "None",
                parentServer:
                    server,
                parentSwitch:
                    networkSwitch);

        Assert.Same(
            server,
            snapshot.ParentServer);

        Assert.Same(
            networkSwitch,
            snapshot.ParentSwitch);
    }

    [Fact]
    public async Task HardwareService_MapsSfpLinkReference()
    {
        var reader =
            new FakeReader();

        reader.Add(
            "Il2Cpp.SFPModule",
            CreateState(
                "Il2Cpp.SFPModule",
                ("link",
                    ReferenceValue(
                        11,
                        "CableLink (11)",
                        "Il2Cpp.CableLink"))));

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet result =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        DataCenterSfpModuleSnapshot sfp =
            Assert.Single(
                result.SfpModules);

        Assert.NotNull(
            sfp.Link);

        Assert.Equal(
            11,
            sfp.Link!.InstanceId);
    }

    [Fact]
    public async Task HardwareService_MapsCableParentReferences()
    {
        var reader =
            new FakeReader();

        reader.Add(
            "Il2Cpp.CableLink",
            CreateState(
                "Il2Cpp.CableLink",
                ("parentServer",
                    ReferenceValue(
                        21,
                        "Server",
                        "Il2Cpp.Server")),
                ("parentSwitch",
                    ReferenceValue(
                        22,
                        "Switch",
                        "Il2Cpp.NetworkSwitch")),
                ("insertedSFP",
                    ReferenceValue(
                        23,
                        "SFP",
                        "Il2Cpp.SFPModule"))));

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet result =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery());

        DataCenterCableSnapshot cable =
            Assert.Single(
                result.Cables);

        Assert.Equal(
            21,
            cable.ParentServer!.InstanceId);

        Assert.Equal(
            22,
            cable.ParentSwitch!.InstanceId);

        Assert.Equal(
            23,
            cable.InsertedSfp!.InstanceId);
    }

    [Fact]
    public async Task HardwareService_RequestsProvenReferenceMembers()
    {
        var reader =
            new FakeReader();

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        await service.CaptureAsync(
            new DataCenterHardwareSnapshotQuery());

        DCMLGameComponentStateQuery sfpQuery =
            reader.Queries.Single(
                value =>
                    value.ComponentTypeName ==
                    "Il2Cpp.SFPModule");

        DCMLGameComponentStateQuery cableQuery =
            reader.Queries.Single(
                value =>
                    value.ComponentTypeName ==
                    "Il2Cpp.CableLink");

        Assert.Contains(
            "link",
            sfpQuery.MemberNames);

        Assert.Contains(
            "parentServer",
            cableQuery.MemberNames);

        Assert.Contains(
            "parentSwitch",
            cableQuery.MemberNames);

        Assert.Contains(
            "parentPatchPanel",
            cableQuery.MemberNames);

        Assert.Contains(
            "parentInternet",
            cableQuery.MemberNames);

        Assert.Contains(
            "insertedSFP",
            cableQuery.MemberNames);
    }

    private static DCMLGameComponentState CreateState(
        string typeName,
        params (string Name, DCMLGameValue Value)[] values)
    {
        return
            new DCMLGameComponentState(
                1,
                "Object",
                "BaseScene",
                "Object",
                true,
                false,
                typeName,
                values.Select(
                    value =>
                        new KeyValuePair<string, DCMLGameValue>(
                            value.Name,
                            value.Value)));
    }

    private static DCMLGameValue ReferenceValue(
        int instanceId,
        string name,
        string typeName)
    {
        return
            new DCMLGameValue(
                DCMLGameValueKind.Reference,
                typeName,
                referenceValue:
                    new DCMLGameReference(
                        instanceId,
                        name,
                        typeName));
    }

    private sealed class FakeReader :
        IDCMLGameComponentStateReader
    {
        private readonly Dictionary<
            string,
            IReadOnlyList<DCMLGameComponentState>>
            _values =
                new Dictionary<
                    string,
                    IReadOnlyList<DCMLGameComponentState>>(
                    StringComparer.OrdinalIgnoreCase);

        public List<DCMLGameComponentStateQuery> Queries { get; } =
            new List<DCMLGameComponentStateQuery>();

        public void Add(
            string typeName,
            params DCMLGameComponentState[] values)
        {
            _values[typeName] =
                values;
        }

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            Queries.Add(
                query);

            if (
                _values.TryGetValue(
                    query.ComponentTypeName,
                    out IReadOnlyList<DCMLGameComponentState>? values)
            )
            {
                return
                    Task.FromResult(
                        values);
            }

            return
                Task.FromResult<IReadOnlyList<DCMLGameComponentState>>(
                    Array.Empty<DCMLGameComponentState>());
        }
    }
}
