namespace DCML.Core.Models;

public enum DCMLPackageVersionChannelTransition
{
    StableToStable = 0,

    StableToPrerelease = 1,

    PrereleaseToPrerelease = 2,

    PrereleaseToStable = 3
}
