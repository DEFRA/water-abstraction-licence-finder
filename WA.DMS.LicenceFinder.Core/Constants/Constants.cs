namespace WA.DMS.LicenceFinder.Core.Constants;

public static class Constants
{
    public const string Impoundment = "Impoundment";
    public const string Abstraction = "Abstraction";
    
    public static string GetLicenceType(string licenceType)
    {
        return licenceType switch
        {
            Impoundment => Impoundment,
            Abstraction => Abstraction,
            _ => throw new KeyNotFoundException(licenceType)
        };
    }
}