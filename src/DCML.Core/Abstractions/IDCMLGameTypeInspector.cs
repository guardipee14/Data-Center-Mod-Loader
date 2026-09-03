using DCML.Core.Models;

namespace DCML.Core.Abstractions;

public interface IDCMLGameTypeInspector
{
    DCMLGameTypeInspection? Inspect(
        DCMLGameTypeInspectionQuery query);
}
