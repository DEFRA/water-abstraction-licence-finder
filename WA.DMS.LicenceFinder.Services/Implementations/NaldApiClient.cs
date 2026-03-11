using System.Text.Json;
using System.Text.Json.Serialization;
using WA.DMS.LicenceFinder.Services.Models;

namespace WA.DMS.LicenceFinder.Services.Implementations;

public class NaldApiClient
{
    public NaldApiClient(string apiBaseUrl)
    {
        HttpClient = new HttpClient();
        HttpClient.BaseAddress = new Uri(apiBaseUrl);
    }
    
    private HttpClient HttpClient { get; set; }

    public async Task<NaldDataCollection> GetNaldDataAsync(short? regionCode)
    {
        var path = "/Extractor/NaldData/GetAll";

        if (regionCode != null)
        {
            path += $"?regionCode={regionCode}";            
        }
        
        var response = await HttpClient.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<NaldDataCollection>(
            content,
            GetSerializerOptions())!;
    }
    
    public async Task<NaldLicenceStatusData> GetNaldLicenceStatusDataAsync(short? regionCode)
    {
        var path = "/Extractor/NaldData/GetLicenceStatusData";
        
        if (regionCode != null)
        {
            path += $"?regionCode={regionCode}";            
        }
        
        var response = await HttpClient.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<NaldLicenceStatusData>(
            content,
            GetSerializerOptions())!;
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