using System.IdentityModel.Tokens.Jwt;
using GymApi.Models.Roles;
using GymManager.API.Models;
using GymManager.API.Services;
using GymManager.API.Tenancy;
using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Tenancy;

public class JwtServiceTests
{
    [Fact]
    public void Generated_token_contains_the_users_gym_id()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "test_key_with_at_least_32_characters_12345",
            Issuer = "tests",
            Audience = "tests"
        });
        var user = new Usuario
        {
            Id = "507f1f77bcf86cd799439011",
            GymId = "gym-a",
            Nombre = "Test",
            Email = "test@example.com",
            EmailNormalizado = "test@example.com",
            PasswordHash = "hash",
            Rol = RolUsuario.Admin
        };

        var token = new JwtSecurityTokenHandler().ReadJwtToken(new JwtService(options).GenerateToken(user));

        Assert.Equal("gym-a", token.Claims.Single(claim => claim.Type == GymClaims.GymId).Value);
    }
}
