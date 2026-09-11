using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services
{
	public class JwtService
	{
		private readonly JwtOptions _options;

		public JwtService(IOptions<JwtOptions> options)
		{
			_options = options.Value;
		}

		public string GenerateToken(Usuario user)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
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
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddDays(7),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
