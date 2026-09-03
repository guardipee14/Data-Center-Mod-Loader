using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameComponentStateQuery
{
    public const int DefaultMaxResults = 256;
    public const int MaximumMaxResults = 16384;

    private readonly IReadOnlyList<string> _memberNames;
    private readonly IReadOnlyList<int> _componentInstanceIds;
    private readonly IReadOnlyList<int> _gameObjectInstanceIds;

    public DCMLGameComponentStateQuery(
        string componentTypeName,
        IEnumerable<string>? memberNames = null,
        string? sceneName = null,
        DCMLGameComponentScope scope = DCMLGameComponentScope.Scene,
        bool includeInactive = true,
        int maxResults = DefaultMaxResults,
        int skipResults = 0,
        IEnumerable<int>? componentInstanceIds = null,
        IEnumerable<int>? gameObjectInstanceIds = null)
    {
        if (string.IsNullOrWhiteSpace(componentTypeName))
        {
            throw new ArgumentException(
                "A component type name is required.",
                nameof(componentTypeName));
        }

        if (!Enum.IsDefined(typeof(DCMLGameComponentScope), scope))
        {
            throw new ArgumentOutOfRangeException(nameof(scope));
        }

        if (maxResults <= 0 || maxResults > MaximumMaxResults)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        if (skipResults < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skipResults));
        }

        ComponentTypeName = componentTypeName.Trim();
        _memberNames = memberNames is null
            ? Array.Empty<string>()
            : memberNames
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        _componentInstanceIds =
            componentInstanceIds is null
                ? Array.Empty<int>()
                : componentInstanceIds
                    .Distinct()
                    .ToArray();

        _gameObjectInstanceIds =
            gameObjectInstanceIds is null
                ? Array.Empty<int>()
                : gameObjectInstanceIds
                    .Distinct()
                    .ToArray();

        SceneName = string.IsNullOrWhiteSpace(sceneName)
            ? string.Empty
            : sceneName.Trim();

        Scope = scope;
        IncludeInactive = includeInactive;
        MaxResults = maxResults;
        SkipResults = skipResults;
    }

    public string ComponentTypeName { get; }

    public IReadOnlyList<string> MemberNames => _memberNames;

    public IReadOnlyList<int> ComponentInstanceIds =>
        _componentInstanceIds;

    public IReadOnlyList<int> GameObjectInstanceIds =>
        _gameObjectInstanceIds;

    public string SceneName { get; }

    public DCMLGameComponentScope Scope { get; }

    public bool IncludeInactive { get; }

    public int MaxResults { get; }

    public int SkipResults { get; }
}
