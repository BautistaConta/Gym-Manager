using GymManager.API.Options;
using GymManager.API.Services;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Cuotas;

public class CuotaCalculatorTests
{
    private static readonly DateTime Today = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, EstadoCuota.PROXIMO_A_VENCER)]
    [InlineData(5, EstadoCuota.PROXIMO_A_VENCER)]
    [InlineData(6, EstadoCuota.AL_DIA)]
    [InlineData(-1, EstadoCuota.VENCIDA)]
    public void Estado_respects_inclusive_boundaries(int offsetDays, EstadoCuota expected)
    {
        var calculator = CreateCalculator();

        Assert.Equal(expected, calculator.Evaluar(Today.AddDays(offsetDays)).Estado);
    }

    [Fact]
    public void Without_payment_is_sin_pagos()
    {
        Assert.Equal(EstadoCuota.SIN_PAGOS, CreateCalculator().Evaluar(null).Estado);
    }

    [Fact]
    public void Configured_window_changes_the_boundary()
    {
        var calculator = CreateCalculator(window: 2);

        Assert.Equal(EstadoCuota.PROXIMO_A_VENCER, calculator.Evaluar(Today.AddDays(2)).Estado);
        Assert.Equal(EstadoCuota.AL_DIA, calculator.Evaluar(Today.AddDays(3)).Estado);
    }

    [Fact]
    public void Business_today_uses_configured_time_zone_not_utc_date()
    {
        var calculator = CreateCalculator(new DateTimeOffset(2026, 9, 14, 2, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 9, 13), calculator.Hoy);
    }

    [Fact]
    public void Active_renewal_starts_after_inclusive_end()
    {
        var calculator = CreateCalculator();

        var periodo = calculator.NuevoPeriodo(Today.AddDays(5), 1, null);

        Assert.Equal(Today.AddDays(6), periodo.Desde);
        Assert.Equal(Today.AddDays(6).AddMonths(1), periodo.Hasta);
    }

    [Fact]
    public void Renewal_on_expiration_day_starts_tomorrow()
    {
        var periodo = CreateCalculator().NuevoPeriodo(Today, 1, null);

        Assert.Equal(Today.AddDays(1), periodo.Desde);
    }

    [Fact]
    public void Late_renewal_starts_today_and_does_not_start_expired()
    {
        var calculator = CreateCalculator();

        var periodo = calculator.NuevoPeriodo(Today.AddDays(-10), 1, null);

        Assert.Equal(Today, periodo.Desde);
        Assert.Equal(Today.AddMonths(1), periodo.Hasta);
    }

    [Fact]
    public void Monthly_payment_expires_on_same_calendar_day_next_month()
    {
        var periodo = CreateCalculator().NuevoPeriodo(null, 1, null);

        Assert.Equal(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), periodo.Desde);
        Assert.Equal(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), periodo.Hasta);
    }

    [Theory]
    [InlineData(8, EstadoCuota.AL_DIA)]
    [InlineData(9, EstadoCuota.PROXIMO_A_VENCER)]
    [InlineData(14, EstadoCuota.PROXIMO_A_VENCER)]
    [InlineData(15, EstadoCuota.VENCIDA)]
    public void October_14_expiration_changes_state_on_expected_day(int octoberDay, EstadoCuota expected)
    {
        var now = new DateTimeOffset(2026, 10, octoberDay, 15, 0, 0, TimeSpan.Zero);
        var calculator = CreateCalculator(now);

        Assert.Equal(expected, calculator.Evaluar(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc)).Estado);
    }

    [Fact]
    public void Manual_end_cannot_precede_new_start()
    {
        var calculator = CreateCalculator();

        Assert.Throws<DomainException>(() => calculator.NuevoPeriodo(Today.AddDays(5), 1, Today.AddDays(5)));
    }

    private static CuotaCalculator CreateCalculator(DateTimeOffset? utcNow = null, int window = 5) =>
        new(Options.Create(new CuotasOptions
        {
            TimeZoneId = "America/Argentina/Buenos_Aires",
            DiasProximoAVencer = window
        }), new FixedTimeProvider(utcNow ?? new DateTimeOffset(2026, 9, 14, 15, 0, 0, TimeSpan.Zero)));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
