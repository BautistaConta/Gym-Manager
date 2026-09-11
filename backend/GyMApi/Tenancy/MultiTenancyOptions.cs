namespace GymManager.API.Tenancy;

public sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";
    public string PilotGymId { get; set; } = null!;
}
