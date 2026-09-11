namespace GymManager.API.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = [];
}
