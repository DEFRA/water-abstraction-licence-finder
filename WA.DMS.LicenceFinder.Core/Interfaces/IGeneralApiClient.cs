using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

public interface IGeneralApiClient
{
    public Task<List<DmsFileReaderResult>> GetDmsFileReaderResultsAsync(string licenceType);
    
    public Task<List<DmsFileIdInformation>> GetDmsFileIdInformationAsync();

    public Task AddDmsFileIdInformationAsync(DmsFileIdInformation newDmsFileIdInformation);

    public Task SaveLicenceFinderResultsAsync(List<LicenceMatchResult> results, string licenceType);

    public Task ClearLicenceFinderResultsAsync(string licenceType);

    public Task SaveVersionFilesToDownloadAsync(List<DownloadInfoMissing> results);

    public Task SaveVersionFilesAsync(List<DownloadInfoAll> results);

    public Task ClearVersionFilesAsync();

    public Task ClearVersionFilesToDownloadAsync();

    public Task<List<LicenceMatchResult>> GetLicenceFinderResultsAsync(int skip, int take, string licenceType);
    
    public Task SaveInspectionReportFinderResultsAsync(List<DmsExtract> results);

    public Task ClearInspectionReportFinderResultsAsync();

    public Task<(List<DmsExtract> Data, string ImportDate)> GetDmsExtractAsync(
        int skip,
        int take,
        string extractType);
}