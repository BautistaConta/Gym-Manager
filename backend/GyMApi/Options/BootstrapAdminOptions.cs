namespace GymManager.API.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";
    public bool Enabled { get; set; }
    public string Nombre { get; set; } = "Administrador";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
