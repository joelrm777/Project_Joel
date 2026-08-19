using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Cine.Nucleo.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Pruebas;

/// <summary>
/// Comprobación de la pieza 3 del plan: qué tarifa admite cada función, cuánto cobra, la
/// declaración de edad y hasta cuándo vende el canal en línea.
/// </summary>
public class PruebasDeTarifas
{
    private static readonly DateOnly Jueves = new(2026, 8, 20);

    private const decimal MontoGeneral = 3500m;
    private const decimal MontoEstudiante = 2500m;
    private const decimal MontoMiercoles = 1750m;

    /// <summary>Sin restricción de edad. «El Último Autobús», clasificación 0.</summary>
    private const int PeliculaSinEdadMinima = 1;

    /// <summary>«Noche de Tormenta», clasificación 12.</summary>
    private const int PeliculaDeDoceAnios = 2;

    private static ServicioVenta Venta(CineDbContext datos) =>
        new(datos, new ServicioCartelera(datos), new ServicioTarifas(datos));

    private static IReadOnlyList<Butaca> Butacas(params string[] butacas) =>
        [.. butacas.Select(b => new Butaca(b[..1], int.Parse(b[1..])))];

    private static IReadOnlyList<LineaTarifa> Lineas(Tarifa tarifa, params string[] butacas) =>
        [.. butacas.Select(b => new LineaTarifa(b[..1], int.Parse(b[1..]), tarifa))];

    /// <summary>
    /// Una hora futura que no caiga en miércoles: ahí la tarifa general no existe (RN-12), y una
    /// prueba que hable de tarifa general fallaría según la hora a la que se corra.
    /// </summary>
    private static DateTime HorarioFuturoQueNoEsMiercoles(DateTime ahora)
    {
        var inicio = ahora.AddHours(3);
        return inicio.DayOfWeek == DayOfWeek.Wednesday ? inicio.AddDays(1) : inicio;
    }

    private static async Task<BaseDePruebas> PrepararAsync()
    {
        var baseDePruebas = new BaseDePruebas();
        await baseDePruebas.SembrarCarteleraAsync(Jueves);
        return baseDePruebas;
    }

    [Fact]
    public async Task Una_funcion_de_miercoles_cobra_la_mitad_y_no_ofrece_estudiante()
    {
        using var baseDePruebas = await PrepararAsync();
        var funcion = await baseDePruebas.PrimeraFuncionDelDiaAsync(DayOfWeek.Wednesday);

        using var datos = baseDePruebas.Abrir();
        var tarifas = await new ServicioTarifas(datos).TarifasDisponiblesAsync(funcion.Id);

        // Toda butaca a la mitad de la general, y ninguna tarifa estudiante (RN-12, RN-13, CA-3).
        Assert.Single(tarifas);
        Assert.Equal(Tarifa.Miercoles, tarifas[0].Tarifa);
        Assert.Equal(MontoMiercoles, tarifas[0].Monto);

        var venta = Venta(datos);
        var apartado = await venta.ApartarAsync(funcion.Id, Butacas("C7"), "token-uno", null);

        var conEstudiante = await venta.PagarAsync(apartado.ApartadoId!.Value,
            Lineas(Tarifa.Estudiante, "C7"), Canal.EnLinea, null, null, true, "clave-estudiante-miercoles");

        Assert.False(conEstudiante.Exitoso);
        Assert.Equal(MotivoRechazo.TarifaNoDisponible, conEstudiante.Motivo);

        var conMiercoles = await venta.PagarAsync(apartado.ApartadoId!.Value,
            Lineas(Tarifa.Miercoles, "C7"), Canal.EnLinea, null, null, true, "clave-miercoles");

        Assert.True(conMiercoles.Exitoso);
        Assert.Equal(MontoMiercoles, conMiercoles.Total);

        var boleto = await datos.Boletos.SingleAsync();
        Assert.Equal(Tarifa.Miercoles, boleto.Tarifa);
        Assert.Equal(MontoMiercoles, boleto.Monto);
    }

    [Fact]
    public async Task Una_funcion_de_otro_dia_ofrece_general_y_estudiante()
    {
        using var baseDePruebas = await PrepararAsync();
        var funcion = await baseDePruebas.PrimeraFuncionDelDiaAsync(DayOfWeek.Thursday);

        using var datos = baseDePruebas.Abrir();
        var tarifas = await new ServicioTarifas(datos).TarifasDisponiblesAsync(funcion.Id);

        Assert.Equal(2, tarifas.Count);
        Assert.Equal(MontoGeneral, tarifas.Single(t => t.Tarifa == Tarifa.General).Monto);
        Assert.Equal(MontoEstudiante, tarifas.Single(t => t.Tarifa == Tarifa.Estudiante).Monto);
        Assert.DoesNotContain(tarifas, t => t.Tarifa == Tarifa.Miercoles);
    }

    [Fact]
    public async Task Un_boleto_estudiante_cobra_el_monto_de_estudiante()
    {
        using var baseDePruebas = await PrepararAsync();
        var funcion = await baseDePruebas.PrimeraFuncionDelDiaAsync(DayOfWeek.Thursday);

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcion.Id, Butacas("D1", "D2"), "token-uno", null);

        // Una butaca general y la otra estudiante: nunca dos tarifas en la misma butaca (RN-15).
        var lineas = new List<LineaTarifa>
        {
            new("D", 1, Tarifa.General),
            new("D", 2, Tarifa.Estudiante)
        };

        var compra = await venta.PagarAsync(apartado.ApartadoId!.Value, lineas,
            Canal.EnLinea, null, null, true, "clave-mixta");

        Assert.True(compra.Exitoso);
        Assert.Equal(MontoGeneral + MontoEstudiante, compra.Total);

        var boletos = await datos.Boletos.OrderBy(b => b.Numero).ToListAsync();
        Assert.Equal(MontoGeneral, boletos[0].Monto);
        Assert.Equal(MontoEstudiante, boletos[1].Monto);
    }

    [Fact]
    public async Task Un_boleto_vendido_conserva_su_monto_cuando_cambian_las_tarifas()
    {
        using var baseDePruebas = await PrepararAsync();
        var funcion = await baseDePruebas.PrimeraFuncionDelDiaAsync(DayOfWeek.Thursday);

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcion.Id, Butacas("E1"), "token-uno", null);
        var compra = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas(Tarifa.General, "E1"),
            Canal.EnLinea, null, null, true, "clave-antes");

        Assert.True(compra.Exitoso);

        // La administradora sube la tarifa general: se inserta una fila nueva, no se actualiza la
        // anterior (RN-16, CA-4).
        await datos.Database.ExecuteSqlAsync(
            $"INSERT INTO ConfiguracionTarifa (MontoGeneral, MontoEstudiante, VigenteDesde) VALUES (5000, 3000, DATEADD(second, -1, SYSDATETIME()))");

        var boletoViejo = await datos.Boletos.AsNoTracking().SingleAsync();
        Assert.Equal(MontoGeneral, boletoViejo.Monto);

        // Y una compra nueva ya cobra la tarifa nueva.
        var otro = await venta.ApartarAsync(funcion.Id, Butacas("E2"), "token-dos", null);
        var compraNueva = await venta.PagarAsync(otro.ApartadoId!.Value, Lineas(Tarifa.General, "E2"),
            Canal.EnLinea, null, null, true, "clave-despues");

        Assert.True(compraNueva.Exitoso);
        Assert.Equal(5000m, compraNueva.Total);
    }

    [Fact]
    public async Task Sin_declarar_la_edad_no_se_completa_la_compra_en_linea()
    {
        using var baseDePruebas = await PrepararAsync();
        var ahora = await baseDePruebas.AhoraDelMotorAsync();
        var funcionId = await baseDePruebas.CrearFuncionAsync(
            PeliculaDeDoceAnios, SemillaCatalogo.SalaDosId, HorarioFuturoQueNoEsMiercoles(ahora));

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcionId, Butacas("A5"), "token-uno", null);

        var sinDeclarar = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas(Tarifa.General, "A5"),
            Canal.EnLinea, null, null, false, "clave-sin-edad");

        Assert.False(sinDeclarar.Exitoso);
        Assert.Equal(MotivoRechazo.EdadNoDeclarada, sinDeclarar.Motivo);
        Assert.Equal(0, await datos.Compras.CountAsync());

        // Declarándola, la misma compra pasa (RN-33, RF-17).
        var declarando = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas(Tarifa.General, "A5"),
            Canal.EnLinea, null, null, true, "clave-con-edad");

        Assert.True(declarando.Exitoso, $"rechazo: {declarando.Motivo}");
    }

    [Fact]
    public async Task Una_pelicula_sin_edad_minima_no_pide_declaracion()
    {
        using var baseDePruebas = await PrepararAsync();
        var ahora = await baseDePruebas.AhoraDelMotorAsync();
        var funcionId = await baseDePruebas.CrearFuncionAsync(
            PeliculaSinEdadMinima, SemillaCatalogo.SalaDosId, HorarioFuturoQueNoEsMiercoles(ahora));

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcionId, Butacas("A6"), "token-uno", null);
        var compra = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas(Tarifa.General, "A6"),
            Canal.EnLinea, null, null, false, "clave-sin-restriccion");

        Assert.True(compra.Exitoso, $"rechazo: {compra.Motivo}");
    }

    [Fact]
    public async Task La_venta_en_linea_se_cierra_cuando_la_funcion_inicia()
    {
        using var baseDePruebas = await PrepararAsync();
        var ahora = await baseDePruebas.AhoraDelMotorAsync();

        // La función empezó hace un minuto: en línea ya cerró, la taquilla todavía vende (RN-28,
        // RN-29, CA-7). El comprador se demoró y paga tarde (R-6).
        var funcionId = await baseDePruebas.CrearFuncionAsync(
            PeliculaSinEdadMinima, SemillaCatalogo.SalaDosId, ahora.AddMinutes(-1));

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcionId, Butacas("B1"), "token-uno", null);
        Assert.True(apartado.Aceptado);

        var enLinea = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas(Tarifa.General, "B1"),
            Canal.EnLinea, null, null, true, "clave-tarde");

        Assert.False(enLinea.Exitoso);
        Assert.Equal(MotivoRechazo.FuncionCerrada, enLinea.Motivo);

        // El rechazo libera las butacas del intento: vuelven a estar libres (R-6).
        Assert.Equal(0, await datos.OcupacionesButaca.CountAsync(o => o.FuncionId == funcionId));
        Assert.Equal(0, await datos.Apartados.CountAsync(a => a.FuncionId == funcionId));
        Assert.Equal(0, await datos.Compras.CountAsync());
    }

    [Fact]
    public async Task Pasados_veinte_minutos_del_inicio_ya_no_se_puede_ni_apartar()
    {
        using var baseDePruebas = await PrepararAsync();
        var ahora = await baseDePruebas.AhoraDelMotorAsync();

        // Veintiún minutos después del inicio la función queda cerrada para todos (RN-30, CA-7).
        var funcionId = await baseDePruebas.CrearFuncionAsync(
            PeliculaSinEdadMinima, SemillaCatalogo.SalaDosId, ahora.AddMinutes(-21));

        using var datos = baseDePruebas.Abrir();

        var apartado = await Venta(datos).ApartarAsync(funcionId, Butacas("B2"), "token-uno", null);

        Assert.False(apartado.Aceptado);
        Assert.Equal(MotivoRechazo.FuncionCerrada, apartado.Motivo);
    }
}
