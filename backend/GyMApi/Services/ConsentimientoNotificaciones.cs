namespace GymManager.API.Services;

public static class ConsentimientoNotificaciones
{
    public static void Validate(bool habilitadas, string? medio, bool confirmado)
    {
        if (!habilitadas) return;
        if (!confirmado) throw new DomainException("Confirmá que el alumno aceptó recibir avisos por WhatsApp.");
        if (string.IsNullOrWhiteSpace(medio)) throw new DomainException("Indicá cómo se obtuvo el consentimiento del alumno.");
        if (medio.Trim().Length > 200) throw new DomainException("El medio de consentimiento no puede superar 200 caracteres.");
    }
}
