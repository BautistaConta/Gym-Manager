using GymManager.API.DTOs;
using GymManager.API.Services;

namespace GyMApi.Tests.Notifications;

public class ConsentimientoNotificacionesTests
{
    [Fact]
    public void New_student_proposes_notifications_but_requires_explicit_confirmation()
    {
        var request = new CrearAlumnoRequest();
        Assert.True(request.NotificacionesHabilitadas);
        Assert.False(request.ConsentimientoConfirmado);
        Assert.Throws<DomainException>(() => ConsentimientoNotificaciones.Validate(
            request.NotificacionesHabilitadas, request.MedioConsentimiento, request.ConsentimientoConfirmado));
    }

    [Fact]
    public void Explicit_confirmation_and_medium_are_required_only_when_enabling()
    {
        ConsentimientoNotificaciones.Validate(false, null, false);
        Assert.Throws<DomainException>(() => ConsentimientoNotificaciones.Validate(true, null, true));
        ConsentimientoNotificaciones.Validate(true, "formulario firmado", true);
    }
}
