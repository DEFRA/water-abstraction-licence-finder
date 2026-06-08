using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

public interface IInventoryApiClient
{
    Task<List<FileMetadata>> GetAllWithMetadataAsync(string startAfter, int take);
}