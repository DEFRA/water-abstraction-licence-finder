using System.Text.Json;
using System.Text.Json.Serialization;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Models;

namespace WA.DMS.LicenceFinder.Services.Implementations;

public class DmsApiClient : IDmsApiClient
{
    public DmsApiClient(string apiBaseUrl)
    {
        HttpClient = new HttpClient();
        HttpClient.BaseAddress = new Uri(apiBaseUrl);
    }
    
    private HttpClient HttpClient { get; set; }

    public async Task<List<DmsFileIdInformation>> GetDmsFileIdInformationAsync()
    {
        var path = "/Extractor/Dms/GetFileIds";

        var response = await HttpClient.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DmsFileIdInformation>>(
            content,
            GetSerializerOptions())!;
    }
    
    public async Task AddDmsFileIdInformationAsync(DmsFileIdInformation newDmsFileIdInformation)
    {
        var path = "/Extractor/Dms/AddFileIdInformation";
        var json = JsonSerializer.Serialize(newDmsFileIdInformation, GetSerializerOptions());
        
        var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(new Uri(HttpClient.BaseAddress!, path), httpContent);
        response.EnsureSuccessStatusCode();
    }

    // TODO - In time this should come from the other project as a NuGet reference
    private static JsonSerializerOptions GetSerializerOptions()
    {
        _options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        return _options;
    }
    
    private static JsonSerializerOptions? _options;
}