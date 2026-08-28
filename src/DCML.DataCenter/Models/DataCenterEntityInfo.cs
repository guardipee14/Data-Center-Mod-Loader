using System;
using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.DataCenter.Models;

public sealed class DataCenterEntityInfo
{
    public DataCenterEntityInfo(
        DCMLGameObjectInfo source,
        string kind,
        string classificationRuleId)
    {
        Source =
            source ??
            throw new ArgumentNullException(
                nameof(source));

        Kind =
            string.IsNullOrWhiteSpace(kind)
                ? DataCenterEntityKinds.Unknown
                : kind.Trim();

        ClassificationRuleId =
            classificationRuleId?.Trim() ??
            string.Empty;
    }

    public DCMLGameObjectInfo Source { get; }

    public string Kind { get; }

    public string ClassificationRuleId { get; }

    public int InstanceId =>
        Source.InstanceId;

    public string Name =>
        Source.Name;

    public string SceneName =>
        Source.SceneName;

    public string HierarchyPath =>
        Source.HierarchyPath;

    public bool ActiveInHierarchy =>
        Source.ActiveInHierarchy;

    public IReadOnlyList<string> ComponentTypeNames =>
        Source.ComponentTypeNames;
}
