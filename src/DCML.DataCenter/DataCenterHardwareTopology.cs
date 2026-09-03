using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

public sealed class DataCenterHardwareTopology :
    IDataCenterHardwareTopology
{
    private const string CableTypeName =
        "Il2Cpp.CableLink";

    private const string SfpTypeName =
        "Il2Cpp.SFPModule";

    private static readonly string[] CableDetailMembers =
    {
        "CustomerID",
        "cableIDsOnLink",
        "connectionSpeed",
        "isEndPoint",
        "isFibrePort",
        "isSFPPort",
        "isStartOrEnd",
        "sfpTypeInserted",
        "sfpTypeSupported",
        "switchID",
        "typeOfLink",
        "insertedSFP",
        "parentInternet",
        "parentPatchPanel",
        "parentServer",
        "parentSwitch"
    };

    private readonly IDataCenterHardwareSnapshots
        _hardwareSnapshots;

    private readonly IDCMLGameComponentStateReader?
        _componentStateReader;

    public DataCenterHardwareTopology(
        IDataCenterHardwareSnapshots hardwareSnapshots)
        : this(
            hardwareSnapshots,
            componentStateReader:
                null)
    {
    }

    public DataCenterHardwareTopology(
        IDataCenterHardwareSnapshots hardwareSnapshots,
        IDCMLGameComponentStateReader? componentStateReader)
    {
        _hardwareSnapshots =
            hardwareSnapshots ??
            throw new ArgumentNullException(
                nameof(hardwareSnapshots));

        _componentStateReader =
            componentStateReader;
    }

    public async Task<DataCenterHardwareTopologyGraph> CaptureAsync(
        DataCenterHardwareSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        DataCenterHardwareSnapshotSet snapshot =
            await _hardwareSnapshots
                .CaptureAsync(
                    query)
                .ConfigureAwait(
                    false);

        if (
            _componentStateReader is null ||
            !query.IncludeSceneObjects
        )
        {
            return Build(snapshot);
        }

        HashSet<int> targetInstanceIds =
            snapshot.SfpModuleInstances
                .Where(value => value.Link is not null)
                .Select(value => value.Link!.InstanceId)
                .ToHashSet();

        if (targetInstanceIds.Count == 0)
        {
            return Build(snapshot);
        }

        var sceneTargets =
            snapshot.CableInstances
                .Where(
                    value =>
                        targetInstanceIds.Contains(
                            value.ComponentInstanceId))
                .GroupBy(value => value.ComponentInstanceId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        DataCenterCableSnapshot value =
                            group.First();

                        return
                            new DataCenterHardwareReference(
                                value.ComponentInstanceId,
                                value.Name,
                                CableTypeName);
                    });

        var unresolvedIds =
            targetInstanceIds
                .Where(value => !sceneTargets.ContainsKey(value))
                .ToHashSet();

        SearchResult sceneSearch =
            unresolvedIds.Count == 0
                ? SearchResult.Empty(
                    exhausted:
                        false)
                : await SearchTargetsAsync(
                    unresolvedIds,
                    query.SceneName,
                    DCMLGameComponentScope.Scene)
                    .ConfigureAwait(false);

        foreach (
            KeyValuePair<int, DataCenterHardwareReference> match in
            sceneSearch.Matches)
        {
            sceneTargets[match.Key] =
                match.Value;
        }

        unresolvedIds.ExceptWith(
            sceneSearch.Matches.Keys);

        SearchResult nonSceneSearch =
            SearchResult.Empty(
                exhausted:
                    false);

        if (unresolvedIds.Count > 0)
        {
            nonSceneSearch =
                await SearchTargetsAsync(
                    unresolvedIds,
                    sceneName:
                        null,
                    DCMLGameComponentScope.Resource)
                    .ConfigureAwait(false);
        }

        IReadOnlyDictionary<int, DataCenterCableSnapshot>
            targetedCableDetails =
                await ReadTargetedCableDetailsAsync(
                    sceneTargets.Keys,
                    query.SceneName)
                    .ConfigureAwait(
                        false);

        return
            BuildCore(
                snapshot,
                sceneTargets,
                nonSceneSearch.Matches,
                targetedCableDetails,
                sceneSearch.PagesRead,
                sceneSearch.CandidatesScanned,
                sceneSearch.Exhausted,
                nonSceneSearch.PagesRead,
                nonSceneSearch.CandidatesScanned,
                nonSceneSearch.Exhausted,
                sceneTargets.Count,
                targetedCableDetails.Count);
    }

    public static DataCenterHardwareTopologyGraph Build(
        DataCenterHardwareSnapshotSet snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(
                nameof(snapshot));
        }

        HashSet<int> targetInstanceIds =
            snapshot.SfpModuleInstances
                .Where(value => value.Link is not null)
                .Select(value => value.Link!.InstanceId)
                .ToHashSet();

        Dictionary<int, DataCenterHardwareReference> sceneTargets =
            snapshot.CableInstances
                .Where(
                    value =>
                        targetInstanceIds.Contains(
                            value.ComponentInstanceId))
                .GroupBy(value => value.ComponentInstanceId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        DataCenterCableSnapshot value =
                            group.First();

                        return
                            new DataCenterHardwareReference(
                                value.ComponentInstanceId,
                                value.Name,
                                CableTypeName);
                    });

        return
            BuildCore(
                snapshot,
                sceneTargets,
                new Dictionary<int, DataCenterHardwareReference>(),
                new Dictionary<int, DataCenterCableSnapshot>(),
                cableSearchPages:
                    0,
                cableCandidatesScanned:
                    0,
                cableSearchExhausted:
                    false,
                nonSceneCableSearchPages:
                    0,
                nonSceneCableCandidatesScanned:
                    0,
                nonSceneCableSearchExhausted:
                    false,
                targetedCableDetailRequestedCount:
                    0,
                targetedCableDetailFoundCount:
                    0);
    }

    private async Task<IReadOnlyDictionary<int, DataCenterCableSnapshot>>
        ReadTargetedCableDetailsAsync(
            IEnumerable<int> componentInstanceIds,
            string? sceneName)
    {
        if (_componentStateReader is null)
        {
            return
                new Dictionary<int, DataCenterCableSnapshot>();
        }

        int[] targetIds =
            componentInstanceIds
                .Distinct()
                .ToArray();

        if (targetIds.Length == 0)
        {
            return
                new Dictionary<int, DataCenterCableSnapshot>();
        }

        IReadOnlyList<DCMLGameComponentState> states =
            await _componentStateReader
                .ReadAsync(
                    new DCMLGameComponentStateQuery(
                        componentTypeName:
                            CableTypeName,
                        memberNames:
                            CableDetailMembers,
                        sceneName:
                            sceneName,
                        scope:
                            DCMLGameComponentScope.Scene,
                        includeInactive:
                            true,
                        maxResults:
                            Math.Min(
                                targetIds.Length,
                                DCMLGameComponentStateQuery.MaximumMaxResults),
                        skipResults:
                            0,
                        componentInstanceIds:
                            targetIds))
                .ConfigureAwait(
                    false);

        return
            states
                .GroupBy(
                    state =>
                        state.ComponentInstanceId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        ToCableSnapshot(
                            group.First()));
    }

    private static DataCenterCableSnapshot ToCableSnapshot(
        DCMLGameComponentState state)
    {
        return
            new DataCenterCableSnapshot(
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

    private static bool? GetBoolean(
        DCMLGameComponentState state,
        string name)
    {
        if (
            !state.Values.TryGetValue(
                name,
                out DCMLGameValue? value) ||
            value.Kind !=
                DCMLGameValueKind.Boolean
        )
        {
            return null;
        }

        return value.BooleanValue;
    }

    private static int? GetInt(
        DCMLGameComponentState state,
        string name)
    {
        if (
            !state.Values.TryGetValue(
                name,
                out DCMLGameValue? value) ||
            value.Kind !=
                DCMLGameValueKind.Integer ||
            !value.IntegerValue.HasValue
        )
        {
            return null;
        }

        long integer =
            value.IntegerValue.Value;

        if (
            integer < int.MinValue ||
            integer > int.MaxValue
        )
        {
            return null;
        }

        return
            (int) integer;
    }

    private static double? GetNumber(
        DCMLGameComponentState state,
        string name)
    {
        if (
            !state.Values.TryGetValue(
                name,
                out DCMLGameValue? value)
        )
        {
            return null;
        }

        if (
            value.Kind ==
                DCMLGameValueKind.Number &&
            value.NumberValue.HasValue
        )
        {
            return value.NumberValue.Value;
        }

        if (
            value.Kind ==
                DCMLGameValueKind.Integer &&
            value.IntegerValue.HasValue
        )
        {
            return value.IntegerValue.Value;
        }

        return null;
    }

    private static string? GetString(
        DCMLGameComponentState state,
        string name)
    {
        if (
            !state.Values.TryGetValue(
                name,
                out DCMLGameValue? value)
        )
        {
            return null;
        }

        if (
            value.Kind ==
                DCMLGameValueKind.String ||
            value.Kind ==
                DCMLGameValueKind.Enum
        )
        {
            return value.StringValue;
        }

        return null;
    }

    private async Task<SearchResult> SearchTargetsAsync(
        HashSet<int> targetInstanceIds,
        string? sceneName,
        DCMLGameComponentScope scope)
    {
        if (_componentStateReader is null)
        {
            return
                SearchResult.Empty(
                    exhausted:
                        false);
        }

        var matches =
            new Dictionary<int, DataCenterHardwareReference>();

        var unresolvedIds =
            new HashSet<int>(
                targetInstanceIds);

        int pagesRead = 0;
        int candidatesScanned = 0;
        int skipResults = 0;
        bool exhausted = false;

        while (unresolvedIds.Count > 0)
        {
            IReadOnlyList<DCMLGameComponentState> page =
                await _componentStateReader
                    .ReadAsync(
                        new DCMLGameComponentStateQuery(
                            componentTypeName:
                                CableTypeName,
                            memberNames:
                                Array.Empty<string>(),
                            sceneName:
                                sceneName,
                            scope:
                                scope,
                            includeInactive:
                                true,
                            maxResults:
                                DCMLGameComponentStateQuery.MaximumMaxResults,
                            skipResults:
                                skipResults))
                    .ConfigureAwait(
                        false);

            pagesRead++;
            candidatesScanned += page.Count;

            foreach (
                DCMLGameComponentState state in
                page)
            {
                if (
                    !unresolvedIds.Contains(
                        state.ComponentInstanceId)
                )
                {
                    continue;
                }

                matches[state.ComponentInstanceId] =
                    new DataCenterHardwareReference(
                        state.ComponentInstanceId,
                        state.Name,
                        CableTypeName);

                unresolvedIds.Remove(
                    state.ComponentInstanceId);

                if (unresolvedIds.Count == 0)
                {
                    break;
                }
            }

            if (unresolvedIds.Count == 0)
            {
                break;
            }

            if (
                page.Count == 0 ||
                page.Count <
                    DCMLGameComponentStateQuery.MaximumMaxResults
            )
            {
                exhausted =
                    true;

                break;
            }

            skipResults +=
                page.Count;
        }

        return
            new SearchResult(
                matches,
                pagesRead,
                candidatesScanned,
                exhausted);
    }

    private static DataCenterHardwareTopologyGraph BuildCore(
        DataCenterHardwareSnapshotSet snapshot,
        IReadOnlyDictionary<int, DataCenterHardwareReference> sceneTargets,
        IReadOnlyDictionary<int, DataCenterHardwareReference> nonSceneTargets,
        IReadOnlyDictionary<int, DataCenterCableSnapshot> targetedCableDetails,
        int cableSearchPages,
        int cableCandidatesScanned,
        bool cableSearchExhausted,
        int nonSceneCableSearchPages,
        int nonSceneCableCandidatesScanned,
        bool nonSceneCableSearchExhausted,
        int targetedCableDetailRequestedCount,
        int targetedCableDetailFoundCount)
    {
        var nodes =
            new List<DataCenterHardwareTopologyNode>();

        foreach (
            DataCenterSfpModuleSnapshot sfp in
            snapshot.SfpModuleInstances)
        {
            nodes.Add(
                new DataCenterHardwareTopologyNode(
                    sfp.ComponentInstanceId,
                    sfp.Name,
                    SfpTypeName,
                    "sfp"));
        }

        foreach (
            DataCenterHardwareReference cable in
            sceneTargets.Values
                .OrderBy(value => value.InstanceId))
        {
            nodes.Add(
                new DataCenterHardwareTopologyNode(
                    cable.InstanceId,
                    cable.Name,
                    CableTypeName,
                    "cable"));
        }

        var edges =
            new List<DataCenterHardwareTopologyEdge>();

        foreach (
            DataCenterSfpModuleSnapshot sfp in
            snapshot.SfpModuleInstances)
        {
            if (sfp.Link is null)
            {
                continue;
            }

            bool sceneResolved =
                sceneTargets.TryGetValue(
                    sfp.Link.InstanceId,
                    out DataCenterHardwareReference?
                        sceneTarget);

            DataCenterHardwareReference?
                nonSceneTarget =
                    null;

            bool nonSceneObserved =
                !sceneResolved &&
                nonSceneTargets.TryGetValue(
                    sfp.Link.InstanceId,
                    out nonSceneTarget);

            DataCenterHardwareTopologyTargetLocation location =
                sceneResolved
                    ? DataCenterHardwareTopologyTargetLocation.SceneObject
                    : nonSceneObserved
                        ? DataCenterHardwareTopologyTargetLocation.NonSceneObject
                        : DataCenterHardwareTopologyTargetLocation.Unknown;

            string? observedName =
                sceneTarget?.Name ??
                nonSceneTarget?.Name;

            targetedCableDetails.TryGetValue(
                sfp.Link.InstanceId,
                out DataCenterCableSnapshot?
                    targetCableDetail);

            edges.Add(
                new DataCenterHardwareTopologyEdge(
                    relationship:
                        "sfp-link",
                    source:
                        new DataCenterHardwareReference(
                            sfp.ComponentInstanceId,
                            sfp.Name,
                            SfpTypeName),
                    target:
                        sfp.Link,
                    targetResolved:
                        sceneResolved,
                    resolvedTargetName:
                        observedName,
                    targetLocation:
                        location,
                    targetCable:
                        targetCableDetail));
        }

        return
            new DataCenterHardwareTopologyGraph(
                nodes
                    .OrderBy(
                        value => value.Kind,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.InstanceId),
                edges
                    .OrderBy(
                        value => value.Relationship,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.Source.InstanceId)
                    .ThenBy(value => value.Target.InstanceId),
                cableSearchPages,
                cableCandidatesScanned,
                cableSearchExhausted,
                nonSceneCableSearchPages,
                nonSceneCableCandidatesScanned,
                nonSceneTargets.Count,
                nonSceneCableSearchExhausted,
                targetedCableDetailRequestedCount,
                targetedCableDetailFoundCount);
    }

    private sealed class SearchResult
    {
        public SearchResult(
            IReadOnlyDictionary<int, DataCenterHardwareReference> matches,
            int pagesRead,
            int candidatesScanned,
            bool exhausted)
        {
            Matches = matches;
            PagesRead = pagesRead;
            CandidatesScanned = candidatesScanned;
            Exhausted = exhausted;
        }

        public IReadOnlyDictionary<int, DataCenterHardwareReference> Matches { get; }

        public int PagesRead { get; }

        public int CandidatesScanned { get; }

        public bool Exhausted { get; }

        public static SearchResult Empty(
            bool exhausted)
        {
            return
                new SearchResult(
                    new Dictionary<int, DataCenterHardwareReference>(),
                    0,
                    0,
                    exhausted);
        }
    }
}
