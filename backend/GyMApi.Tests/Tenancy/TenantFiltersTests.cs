using GymManager.API.Models;
using GymManager.API.Repositories;
using MongoDB.Bson.Serialization;

namespace GyMApi.Tests.Tenancy;

public class TenantFiltersTests
{
    [Fact]
    public void Every_tenant_model_filter_contains_the_current_gym()
    {
        AssertGymFilter<Usuario>();
        AssertGymFilter<Alumno>();
        AssertGymFilter<Sucursal>();
        AssertGymFilter<CategoriaPago>();
        AssertGymFilter<Pago>();
        AssertGymFilter<NotificacionWhatsApp>();
    }

    [Fact]
    public void Write_guard_rejects_an_entity_from_another_gym()
    {
        var alumno = new Alumno { GymId = "gym-b" };

        Assert.Throws<InvalidOperationException>(() => TenantFilters.EnsureOwned(alumno, "gym-a"));
    }

    [Fact]
    public void Insert_stamp_cannot_keep_a_caller_supplied_gym_id()
    {
        var pago = new Pago { GymId = "gym-b" };

        TenantFilters.Stamp(pago, "gym-a");

        Assert.Equal("gym-a", pago.GymId);
    }

    private static void AssertGymFilter<T>() where T : IGymOwned
    {
        var rendered = TenantFilters.ForGym<T>("gym-a").Render(
            BsonSerializer.LookupSerializer<T>(), BsonSerializer.SerializerRegistry);

        Assert.Equal("gym-a", rendered["GymId"].AsString);
    }
}
