using DCML.DataCenter.Models;

namespace DCML.DataCenter.Abstractions;

public interface IDataCenterComponentCatalog
{
    DataCenterComponentCatalogSnapshot Scan(
        DataCenterComponentCatalogQuery query);
}
