namespace GymManager.API.Models
{
    public class RegisterRequest
    {
        public required string Nombre { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? Tipo { get; set; }
    }
}
