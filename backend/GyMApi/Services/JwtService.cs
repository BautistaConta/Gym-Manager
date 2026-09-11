using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GymManager.API.Models;
using GymManager.API.Tenancy;

namespace GymManager.API.Services
{
	public class JwtService
	{
		private readonly IConfiguration _config;

		public JwtService(IConfiguration config)
		{
			_config = config;
		}

		public string GenerateToken(Usuario user)
		{
			var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no está configurado.");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim("nombre", user.Nombre ?? string.Empty),
                new Claim(GymClaims.GymId, user.GymId),
                new Claim(ClaimTypes.Role, user.Rol.ToString())
			};

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddDays(7),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
