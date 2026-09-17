using System.Text.RegularExpressions;
using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public sealed class ConfiguracionGymService(IConfiguracionGymRepository repository, TimeProvider clock)
{
    private static readonly Regex Invite = new(@"^https://chat\.whatsapp\.com/[A-Za-z0-9_-]{10,}$", RegexOptions.Compiled);

    public async Task<ConfiguracionGym> GetAsync() => await repository.GetAsync() ?? new ConfiguracionGym();

    public async Task<ConfiguracionGym> UpdateAsync(ConfiguracionWhatsAppRequest request)
    {
        var url = string.IsNullOrWhiteSpace(request.WhatsAppGroupInviteUrl) ? null : request.WhatsAppGroupInviteUrl.Trim();
        if (url is not null && !Invite.IsMatch(url))
            throw new DomainException("El enlace debe ser una invitación HTTPS válida de chat.whatsapp.com.");
        var value = await repository.GetAsync() ?? new ConfiguracionGym();
        value.NombreComercial = request.NombreComercial.Trim();
        value.WhatsAppGroupInviteUrl = url;
        value.TextoInicialChat = string.IsNullOrWhiteSpace(request.TextoInicialChat) ? null : request.TextoInicialChat.Trim();
        value.FechaActualizacionUtc = clock.GetUtcNow().UtcDateTime;
        await repository.UpsertAsync(value);
        return value;
    }
}
