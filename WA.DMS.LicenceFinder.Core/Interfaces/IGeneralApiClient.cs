using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

public interface IGeneralApiClient
{
    public Task<List<DmsFileIdInformation>> GetDmsFileIdInformationAsync();

    public Task AddDmsFileIdInformationAsync(DmsFileIdInformation newDmsFileIdInformation);

    public Task SaveLicenceFinderResultsAsync(List<LicenceMatchResult> results);

    public Task ClearLicenceFinderResultsAsync();

    public Task SaveVersionFilesToDownloadAsync(List<DownloadInfoMissing> results);

    public Task SaveVersionFilesAsync(List<DownloadInfoAll> results);

    public Task ClearVersionFilesAsync();

    public Task ClearVersionFilesToDownloadAsync();
}