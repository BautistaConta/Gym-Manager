using Microsoft.Extensions.Options;

namespace GymManager.API.Tenancy;

public sealed class GymContext : IGymContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancyOptions _options;

    public GymContext(IHttpContextAccessor httpContextAccessor, IOptions<MultiTenancyOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public string GymId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(GymClaims.GymId)?.Value
                    ?? throw new InvalidOperationException("El JWT autenticado no contiene el claim gym_id.");
            }

            if (string.IsNullOrWhiteSpace(_options.PilotGymId))
                throw new InvalidOperationException("MultiTenancy:PilotGymId no está configurado.");

            return _options.PilotGymId;
        }
    }
}
