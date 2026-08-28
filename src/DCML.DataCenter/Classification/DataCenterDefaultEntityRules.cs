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
