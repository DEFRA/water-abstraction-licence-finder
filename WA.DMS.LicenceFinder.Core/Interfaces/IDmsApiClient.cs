using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

public interface IDmsApiClient
{
    public Task<List<DmsFileIdInformation>> GetDmsFileIdInformationAsync();

    public Task AddDmsFileIdInformationAsync(DmsFileIdInformation newDmsFileIdInformation);
}