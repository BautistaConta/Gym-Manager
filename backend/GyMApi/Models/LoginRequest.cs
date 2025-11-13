namespace GymManager.API.Models
{
    public class LoginRequest
    {
        public required string Email { get; set; }
        public string Password { get; set; }
    }
}
