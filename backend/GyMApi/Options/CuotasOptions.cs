namespace GymManager.API.Options;

public sealed class CuotasOptions
{
    public const string SectionName = "Cuotas";
    public string TimeZoneId { get; set; } = "America/Argentina/Buenos_Aires";
    public int DiasProximoAVencer { get; set; } = 5;
}
