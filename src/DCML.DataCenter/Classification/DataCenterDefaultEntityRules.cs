using System.Collections.Generic;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Classification;

public static class DataCenterDefaultEntityRules
{
    public static IReadOnlyList<DataCenterEntityRule> Create()
    {
        return
            new[]
            {
                // Evidence-backed physical/gameplay identities.
                //
                // These exact type matches are intentionally conservative.
                // Related helpers, save-data classes, mounting positions,
                // configuration UI, and transceiver modules are NOT promoted
                // to top-level semantic entities here.
                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.server.component",
                    kind:
                        DataCenterEntityKinds.Server,
                    priority:
                        400,
                    componentTypeName:
                        "Il2Cpp.Server"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.rack.component",
                    kind:
                        DataCenterEntityKinds.Rack,
                    priority:
                        400,
                    componentTypeName:
                        "Il2Cpp.Rack"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.router.component",
                    kind:
                        DataCenterEntityKinds.NetworkDevice,
                    priority:
                        400,
                    componentTypeName:
                        "Il2Cpp.Router"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.firewall.component",
                    kind:
                        DataCenterEntityKinds.NetworkDevice,
                    priority:
                        400,
                    componentTypeName:
                        "Il2Cpp.Firewall"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.network-switch.component",
                    kind:
                        DataCenterEntityKinds.NetworkDevice,
                    priority:
                        390,
                    componentTypeName:
                        "Il2Cpp.NetworkSwitch"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.cable-link.component",
                    kind:
                        DataCenterEntityKinds.Cable,
                    priority:
                        400,
                    componentTypeName:
                        "Il2Cpp.CableLink"),

                // Lower-priority inheritance-aware fallbacks make the
                // recommended API friendly to Data Center subclasses and
                // compatible mod-added derived gameplay types.
                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.network-device.inherited",
                    kind:
                        DataCenterEntityKinds.NetworkDevice,
                    priority:
                        350,
                    componentTypeAssignableTo:
                        "Il2Cpp.NetworkSwitch"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.server.inherited",
                    kind:
                        DataCenterEntityKinds.Server,
                    priority:
                        340,
                    componentTypeAssignableTo:
                        "Il2Cpp.Server"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.rack.inherited",
                    kind:
                        DataCenterEntityKinds.Rack,
                    priority:
                        340,
                    componentTypeAssignableTo:
                        "Il2Cpp.Rack"),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.cable.inherited",
                    kind:
                        DataCenterEntityKinds.Cable,
                    priority:
                        340,
                    componentTypeAssignableTo:
                        "Il2Cpp.CableLink"),

                // Existing UI recommendations remain lower priority than
                // exact gameplay component identities.
                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.ui.component",
                    kind:
                        DataCenterEntityKinds.UserInterface,
                    priority:
                        200,
                    componentTypePrefix:
                        "UnityEngine.UI."),

                new DataCenterEntityRule(
                    id:
                        "dcml.datacenter.ui.canvas",
                    kind:
                        DataCenterEntityKinds.UserInterface,
                    priority:
                        100,
                    hierarchyStartsWith:
                        "Canvas")
            };
    }
}
