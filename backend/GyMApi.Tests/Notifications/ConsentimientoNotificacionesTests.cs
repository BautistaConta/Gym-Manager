using GymManager.API.DTOs;
using GymManager.API.Services;

namespace GyMApi.Tests.Notifications;

public class ConsentimientoNotificacionesTests
{
    [Fact]
    public void New_student_does_not_enable_notifications_silently()
    {
        var request = new CrearAlumnoRequest();
        Assert.False(request.NotificacionesHabilitadas);
        Assert.False(request.ConsentimientoConfirmado);
        ConsentimientoNotificaciones.Validate(
            request.NotificacionesHabilitadas, request.MedioConsentimiento, request.ConsentimientoConfirmado);
    }

    [Fact]
    public void Explicit_confirmation_and_medium_are_required_only_when_enabling()
    {
        ConsentimientoNotificaciones.Validate(false, null, false);
        Assert.Throws<DomainException>(() => ConsentimientoNotificaciones.Validate(true, null, true));
        ConsentimientoNotificaciones.Validate(true, "formulario firmado", true);
    }
}
