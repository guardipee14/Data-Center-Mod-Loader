using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

public sealed class DataCenterCablePersistenceSnapshot
{
    private readonly IReadOnlyList<DataCenterCablePersistenceRecord> _cables;
    private readonly IReadOnlyList<string> _serverIds;
    private readonly IReadOnlyList<string> _switchIds;
    private readonly IReadOnlyList<string> _routerIds;
    private readonly IReadOnlyList<string> _firewallIds;
    private readonly IReadOnlyList<string> _patchPanelIds;
    private readonly IReadOnlyList<int> _customerIds;

    public DataCenterCablePersistenceSnapshot(
        string sourcePath,
        long sourceLength,
        DateTime sourceLastWriteTimeUtc,
        IEnumerable<DataCenterCablePersistenceRecord>? cables,
        IEnumerable<string>? serverIds,
        IEnumerable<string>? switchIds,
        IEnumerable<string>? routerIds,
        IEnumerable<string>? firewallIds,
        IEnumerable<string>? patchPanelIds,
        IEnumerable<int>? customerIds)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "A persistence source path is required.",
                nameof(sourcePath));
        }

        if (sourceLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceLength));
        }

        SourcePath = sourcePath;
        SourceLength = sourceLength;
        SourceLastWriteTimeUtc = sourceLastWriteTimeUtc;
        _cables = cables?.ToArray() ?? Array.Empty<DataCenterCablePersistenceRecord>();
        _serverIds = NormalizeStrings(serverIds);
        _switchIds = NormalizeStrings(switchIds);
        _routerIds = NormalizeStrings(routerIds);
        _firewallIds = NormalizeStrings(firewallIds);
        _patchPanelIds = NormalizeStrings(patchPanelIds);
        _customerIds = customerIds?.Distinct().OrderBy(value => value).ToArray()
            ?? Array.Empty<int>();

        Index = new DataCenterCablePersistenceIndex(
            _serverIds,
            _switchIds,
            _routerIds,
            _firewallIds,
            _patchPanelIds,
            _customerIds);

        DataCenterPhysicalCableConnection[] connections =
            _cables.Select(Index.Resolve).ToArray();

        EndpointCount = connections.Length * 2;
        ResolvedEndpointCount =
            connections.Sum(
                value =>
                    (value.Start.IsResolved ? 1 : 0) +
                    (value.End.IsResolved ? 1 : 0));
        UnresolvedEndpointCount = EndpointCount - ResolvedEndpointCount;
    }

    public string SourcePath { get; }
    public long SourceLength { get; }
    public DateTime SourceLastWriteTimeUtc { get; }
    public IReadOnlyList<DataCenterCablePersistenceRecord> Cables => _cables;
    public IReadOnlyList<string> ServerIDs => _serverIds;
    public IReadOnlyList<string> SwitchIDs => _switchIds;
    public IReadOnlyList<string> RouterIDs => _routerIds;
    public IReadOnlyList<string> FirewallIDs => _firewallIds;
    public IReadOnlyList<string> PatchPanelIDs => _patchPanelIds;
    public IReadOnlyList<int> CustomerIDs => _customerIds;
    public DataCenterCablePersistenceIndex Index { get; }
    public int CableCount => _cables.Count;
    public int EndpointCount { get; }
    public int ResolvedEndpointCount { get; }
    public int UnresolvedEndpointCount { get; }
    public bool IsFullyResolved => UnresolvedEndpointCount == 0;

    private static IReadOnlyList<string> NormalizeStrings(
        IEnumerable<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
