namespace DCML.Core.Models;

/// <summary>
/// Controls exceptional version transitions. The safe default does not allow
/// prerelease targets or downgrades.
/// </summary>
public sealed class DCMLPackageVersionPolicyOptions
{
    public DCMLPackageVersionPolicyOptions(
        bool allowPrerelease = false,
        bool allowDowngrade = false)
    {
        AllowPrerelease =
            allowPrerelease;

        AllowDowngrade =
            allowDowngrade;
    }

    public bool AllowPrerelease { get; }

    public bool AllowDowngrade { get; }

    public static DCMLPackageVersionPolicyOptions SafeDefault { get; } =
        new DCMLPackageVersionPolicyOptions();
}
