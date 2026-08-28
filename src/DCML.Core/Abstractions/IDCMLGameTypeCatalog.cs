using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.Core.Abstractions;

public interface IDCMLGameTypeCatalog
{
    IReadOnlyList<DCMLGameTypeInfo> Find(
        DCMLGameTypeQuery query);
}
