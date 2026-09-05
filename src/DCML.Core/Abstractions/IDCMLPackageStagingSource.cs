using DCML.Core.Models;

namespace DCML.Core.Abstractions;

/// <summary>
/// Optional package-source capability for explicitly staging one already
/// available source entry into a caller-controlled local directory.
/// </summary>
public interface IDCMLPackageStagingSource :
    IDCMLPackageSource
{
    DCMLPackageStageResult StagePackage(
        DCMLPackageSourceEntry entry,
        string stagingRoot);
}
