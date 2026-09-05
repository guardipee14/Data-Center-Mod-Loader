using DCML.Core.Models;

namespace DCML.Core.Abstractions;

/// <summary>
/// Optional package-source capability for describing an available package
/// version without staging, installing, updating, or authorizing any action.
/// </summary>
public interface IDCMLPackageUpdateMetadataSource :
    IDCMLPackageSource
{
    DCMLPackageUpdateMetadataResult GetUpdateMetadata(
        DCMLPackageSourceEntry entry);
}
