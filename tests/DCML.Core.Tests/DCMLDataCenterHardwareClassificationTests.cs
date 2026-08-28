using System.Linq;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Classification;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDataCenterHardwareClassificationTests
{
    [Fact]
    public void Defaults_ClassifyServerComponent()
    {
        AssertClassified(
            "Il2Cpp.Server",
            DataCenterEntityKinds.Server,
            "dcml.datacenter.server.component");
    }

    [Fact]
    public void Defaults_ClassifyRackComponent()
    {
        AssertClassified(
            "Il2Cpp.Rack",
            DataCenterEntityKinds.Rack,
            "dcml.datacenter.rack.component");
    }

    [Fact]
    public void Defaults_ClassifyNetworkSwitchComponent()
    {
        AssertClassified(
            "Il2Cpp.NetworkSwitch",
            DataCenterEntityKinds.NetworkDevice,
            "dcml.datacenter.network-switch.component");
    }

    [Fact]
    public void Defaults_ClassifyRouterComponent()
    {
        AssertClassified(
            "Il2Cpp.Router",
            DataCenterEntityKinds.NetworkDevice,
            "dcml.datacenter.router.component");
    }

    [Fact]
    public void Defaults_ClassifyFirewallComponent()
    {
        AssertClassified(
            "Il2Cpp.Firewall",
            DataCenterEntityKinds.NetworkDevice,
            "dcml.datacenter.firewall.component");
    }

    [Fact]
    public void Defaults_ClassifyCableLinkComponent()
    {
        AssertClassified(
            "Il2Cpp.CableLink",
            DataCenterEntityKinds.Cable,
            "dcml.datacenter.cable-link.component");
    }

    [Fact]
    public void Defaults_DoNotTreatRackMountAsRack()
    {
        AssertUnknown(
            "Il2Cpp.RackMount");
    }

    [Fact]
    public void Defaults_DoNotTreatSfpModuleAsNetworkDevice()
    {
        AssertUnknown(
            "Il2Cpp.SFPModule");
    }

    private static void AssertClassified(
        string componentType,
        string expectedKind,
        string expectedRuleId)
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new FakeDiscovery(
                    CreateObject(
                        componentType)));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        includeUnknown:
                            false)));

        Assert.Equal(
            expectedKind,
            result.Kind);

        Assert.Equal(
            expectedRuleId,
            result.ClassificationRuleId);
    }

    private static void AssertUnknown(
        string componentType)
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new FakeDiscovery(
                    CreateObject(
                        componentType)));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        includeUnknown:
                            true)));

        Assert.Equal(
            DataCenterEntityKinds.Unknown,
            result.Kind);

        Assert.Equal(
            string.Empty,
            result.ClassificationRuleId);
    }

    private static DCMLGameObjectInfo CreateObject(
        string componentType)
    {
        return
            new DCMLGameObjectInfo(
                1,
                componentType
                    .Split('.')
                    .Last(),
                "BaseScene",
                "Objects/Test/" +
                componentType
                    .Split('.')
                    .Last(),
                true,
                new[]
                {
                    componentType
                });
    }

    private sealed class FakeDiscovery :
        DCML.Core.Abstractions.IDCMLGameObjectDiscovery
    {
        private readonly DCMLGameObjectInfo[] _objects;

        public FakeDiscovery(
            params DCMLGameObjectInfo[] objects)
        {
            _objects =
                objects;
        }

        public System.Collections.Generic.IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            return
                _objects;
        }
    }
}
