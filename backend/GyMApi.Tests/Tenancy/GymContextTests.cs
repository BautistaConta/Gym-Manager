using System.Security.Claims;
using GymManager.API.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Tenancy;

public class GymContextTests
{
    [Fact]
    public void Authenticated_request_uses_gym_id_claim()
    {
        var accessor = AuthenticatedAccessor(new Claim(GymClaims.GymId, "gym-a"));
        var context = CreateContext(accessor);

        Assert.Equal("gym-a", context.GymId);
    }

    [Fact]
    public void Authenticated_request_without_gym_id_is_rejected()
    {
        var context = CreateContext(AuthenticatedAccessor());

        Assert.Throws<InvalidOperationException>(() => context.GymId);
    }

    [Fact]
    public void Background_scope_uses_configured_pilot_gym()
    {
        var context = CreateContext(new HttpContextAccessor());

        Assert.Equal("pilot-gym", context.GymId);
    }

    private static GymContext CreateContext(IHttpContextAccessor accessor) =>
        new(accessor, Options.Create(new MultiTenancyOptions { PilotGymId = "pilot-gym" }));

    private static HttpContextAccessor AuthenticatedAccessor(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }
}
