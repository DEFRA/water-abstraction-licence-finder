using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

public interface IGeneralApiClient
{
    public Task<List<DmsFileIdInformation>> GetDmsFileIdInformationAsync();

    public Task AddDmsFileIdInformationAsync(DmsFileIdInformation newDmsFileIdInformation);

    public Task SaveAbstractionLicenceFinderResultsAsync(List<LicenceMatchResult> results);
    
    public Task SaveImpoundmentLicenceFinderResultsAsync(List<ImpoundmentLicenceMatchResult> results);

    public Task ClearLicenceFinderResultsAsync(string licenceType);

    public Task SaveVersionFilesToDownloadAsync(List<DownloadInfoMissing> results);

    public Task SaveVersionFilesAsync(List<DownloadInfoAll> results);

    public Task ClearVersionFilesAsync();

    public Task ClearVersionFilesToDownloadAsync();

    public Task SaveInspectionReportFinderResultsAsync(List<DmsExtract> results);

    public Task ClearInspectionReportFinderResultsAsync();
}