using System.Collections.Generic;
using System.Threading.Tasks;
using DCML.Core.Models;

namespace DCML.Core.Abstractions;

public interface IDCMLGameComponentStateReader
{
    Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
        DCMLGameComponentStateQuery query);
}
