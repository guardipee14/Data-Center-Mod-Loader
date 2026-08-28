using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.Core.Abstractions;

public interface IDCMLGameObjectDiscovery
{
    IReadOnlyList<DCMLGameObjectInfo> Find(
        DCMLGameObjectQuery query);
}
