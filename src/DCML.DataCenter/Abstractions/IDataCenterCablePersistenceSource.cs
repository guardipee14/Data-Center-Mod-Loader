using System.Threading.Tasks;
using DCML.DataCenter.Models;

namespace DCML.DataCenter.Abstractions;

public interface IDataCenterCablePersistenceSource
{
    string SourcePath { get; }

    Task<DataCenterCablePersistenceSnapshot> ReadAsync();
}
