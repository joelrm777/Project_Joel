using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Cine.Nucleo.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Pruebas;

/// <summary>
/// Comprobación de la pieza 2 del plan: apartar, soltar y pagar en línea a tarifa general.
/// Cada prueba trabaja sobre su propia base, porque lo que se comprueba es lo que el motor hace
/// cuando dos intentos caen sobre la misma butaca.
/// </summary>
public class PruebasDeVenta
{
    private static readonly DateOnly Jueves = new(2026, 8, 20);

    private static async Task<(BaseDePruebas Base, int FuncionId)> PrepararAsync()
    {
        var baseDePruebas = new BaseDePruebas();
        await baseDePruebas.SembrarCarteleraAsync(Jueves);
        var funcion = await baseDePruebas.PrimeraFuncionDeAsync(SemillaCatalogo.SalaUnoId);
        return (baseDePruebas, funcion.Id);
    }

    private static ServicioVenta Venta(CineDbContext datos) => new(datos, new ServicioCartelera(datos));

    private static IReadOnlyList<Butaca> Butacas(params string[] butacas) =>
        [.. butacas.Select(b => new Butaca(b[..1], int.Parse(b[1..])))];

    private static IReadOnlyList<LineaTarifa> Lineas(params string[] butacas) =>
        [.. butacas.Select(b => new LineaTarifa(b[..1], int.Parse(b[1..]), Tarifa.General))];

    [Fact]
    public async Task Dos_intentos_simultaneos_sobre_la_misma_butaca_dejan_una_sola_ocupacion()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        // Los dos compradores tienen abierto el mapa de la misma función y tocan C4 a la vez (R-5).
        var primero = Task.Run(async () =>
        {
            using var datos = baseDePruebas.Abrir();
            return await Venta(datos).ApartarAsync(funcionId, Butacas("C4"), "token-uno", null);
        });

        var segundo = Task.Run(async () =>
        {
            using var datos = baseDePruebas.Abrir();
            return await Venta(datos).ApartarAsync(funcionId, Butacas("C4"), "token-dos", null);
        });

        var resultados = await Task.WhenAll(primero, segundo);

        Assert.Equal(1, resultados.Count(r => r.Aceptado));
        var rechazado = resultados.Single(r => !r.Aceptado);
        Assert.Equal(MotivoRechazo.ButacaTomada, rechazado.Motivo);

        // El rechazo vuelve con el mapa actualizado, para redibujarlo en el momento (RF-12).
        Assert.NotNull(rechazado.Mapa);
        Assert.Equal(EstadoButaca.Apartada,
            rechazado.Mapa!.Butacas.Single(b => b.Fila == "C" && b.Numero == 4).Estado);

        using var verificacion = baseDePruebas.Abrir();
        Assert.Equal(1, await verificacion.OcupacionesButaca
            .CountAsync(o => o.FuncionId == funcionId && o.Fila == "C" && o.Numero == 4));
    }

    [Fact]
    public async Task Un_apartado_vencido_deja_la_butaca_libre_para_otro_comprador()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using (var datos = baseDePruebas.Abrir())
        {
            var apartado = await Venta(datos).ApartarAsync(funcionId, Butacas("C4"), "token-uno", null);
            Assert.True(apartado.Aceptado);

            // Diez minutos después, sin pago (CA-2, RN-20).
            await datos.Database.ExecuteSqlAsync(
                $"UPDATE Apartado SET VenceEn = DATEADD(second, -1, SYSDATETIME()) WHERE Id = {apartado.ApartadoId}");
        }

        using (var datos = baseDePruebas.Abrir())
        {
            var mapa = await new ServicioCartelera(datos).ObtenerMapaAsync(funcionId);
            Assert.Equal(EstadoButaca.Libre,
                mapa!.Butacas.Single(b => b.Fila == "C" && b.Numero == 4).Estado);
        }

        using (var datos = baseDePruebas.Abrir())
        {
            var otro = await Venta(datos).ApartarAsync(funcionId, Butacas("C4"), "token-dos", null);
            Assert.True(otro.Aceptado);
        }
    }

    [Fact]
    public async Task Dos_confirmaciones_de_pago_dejan_una_sola_compra_y_un_solo_codigo()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        int apartadoId;
        using (var datos = baseDePruebas.Abrir())
        {
            var apartado = await Venta(datos).ApartarAsync(funcionId, Butacas("D5", "D6"), "token-uno", null);
            apartadoId = apartado.ApartadoId!.Value;
        }

        ResultadoCompra primera, segunda;
        using (var datos = baseDePruebas.Abrir())
        {
            primera = await Venta(datos).PagarAsync(apartadoId, Lineas("D5", "D6"),
                Canal.EnLinea, "cliente@ejemplo.cr", null, false, "clave-una");
        }

        using (var datos = baseDePruebas.Abrir())
        {
            // El comprador toca «pagar» otra vez al no ver respuesta (R-10).
            segunda = await Venta(datos).PagarAsync(apartadoId, Lineas("D5", "D6"),
                Canal.EnLinea, "cliente@ejemplo.cr", null, false, "clave-una");
        }

        Assert.True(primera.Exitoso);
        Assert.True(segunda.Exitoso);
        Assert.Equal(primera.Codigo, segunda.Codigo);
        Assert.StartsWith("CV-", primera.Codigo);
        Assert.Equal(2 * ServicioVenta.TarifaGeneralProvisional, primera.Total);

        using var verificacion = baseDePruebas.Abrir();
        Assert.Equal(1, await verificacion.Compras.CountAsync());
        Assert.Equal(2, await verificacion.Boletos.CountAsync());
        Assert.Equal(0, await verificacion.Apartados.CountAsync());
        Assert.Equal(2, await verificacion.OcupacionesButaca
            .CountAsync(o => o.Estado == EstadoButaca.Vendida));
    }

    [Fact]
    public async Task No_se_puede_pagar_un_apartado_vencido()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        int apartadoId;
        using (var datos = baseDePruebas.Abrir())
        {
            var apartado = await Venta(datos).ApartarAsync(funcionId, Butacas("E7"), "token-uno", null);
            apartadoId = apartado.ApartadoId!.Value;
            await datos.Database.ExecuteSqlAsync(
                $"UPDATE Apartado SET VenceEn = DATEADD(second, -1, SYSDATETIME()) WHERE Id = {apartadoId}");
        }

        using (var datos = baseDePruebas.Abrir())
        {
            var compra = await Venta(datos).PagarAsync(apartadoId, Lineas("E7"),
                Canal.EnLinea, null, null, false, "clave-vencida");

            Assert.False(compra.Exitoso);
            Assert.Equal(MotivoRechazo.ApartadoVencido, compra.Motivo);
        }

        using var verificacion = baseDePruebas.Abrir();
        Assert.Equal(0, await verificacion.Compras.CountAsync());
        Assert.Equal(0, await verificacion.OcupacionesButaca.CountAsync(o => o.Fila == "E" && o.Numero == 7));
    }

    [Fact]
    public async Task Un_intento_de_mas_de_diez_butacas_se_rechaza()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using var datos = baseDePruebas.Abrir();
        var once = Butacas("F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11");

        var resultado = await Venta(datos).ApartarAsync(funcionId, once, "token-uno", null);

        Assert.False(resultado.Aceptado);
        Assert.Equal(MotivoRechazo.LimiteButacas, resultado.Motivo);
        Assert.Equal(0, await datos.OcupacionesButaca.CountAsync(o => o.Fila == "F"));
    }

    [Fact]
    public async Task Agregar_una_butaca_renueva_el_plazo_de_todo_el_intento()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var primero = await venta.ApartarAsync(funcionId, Butacas("G1"), "token-uno", null);
        var segundo = await venta.ApartarAsync(funcionId, Butacas("G2"), "token-uno", primero.ApartadoId);

        Assert.True(segundo.Aceptado);
        Assert.Equal(primero.ApartadoId, segundo.ApartadoId);
        Assert.True(segundo.VenceEn >= primero.VenceEn);
        Assert.Equal(2, await datos.OcupacionesButaca.CountAsync(o => o.ApartadoId == primero.ApartadoId));
    }

    [Fact]
    public async Task Soltar_una_butaca_antes_de_pagar_la_devuelve_a_libre()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcionId, Butacas("H1", "H2", "H3"), "token-uno", null);
        var liberada = await venta.LiberarButacaAsync(apartado.ApartadoId!.Value, new Butaca("H", 2));

        Assert.True(liberada.Exitoso);

        var mapa = await new ServicioCartelera(datos).ObtenerMapaAsync(funcionId);
        Assert.Equal(EstadoButaca.Libre, mapa!.Butacas.Single(b => b.Fila == "H" && b.Numero == 2).Estado);
        Assert.Equal(EstadoButaca.Apartada, mapa.Butacas.Single(b => b.Fila == "H" && b.Numero == 1).Estado);
        Assert.Equal(2, await datos.OcupacionesButaca.CountAsync(o => o.ApartadoId == apartado.ApartadoId));
    }

    [Fact]
    public async Task Una_butaca_no_vendible_no_se_puede_apartar()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using var datos = baseDePruebas.Abrir();

        // A1 es una de las tres butacas que nunca se venden en la Sala 1 (RN-2).
        var resultado = await Venta(datos).ApartarAsync(funcionId, Butacas("A1"), "token-uno", null);

        Assert.False(resultado.Aceptado);
        Assert.Equal(MotivoRechazo.ButacaNoVendible, resultado.Motivo);
    }

    [Fact]
    public async Task El_boleto_graba_su_tarifa_y_su_monto_al_venderse()
    {
        var (baseDePruebas, funcionId) = await PrepararAsync();
        using var _ = baseDePruebas;

        using var datos = baseDePruebas.Abrir();
        var venta = Venta(datos);

        var apartado = await venta.ApartarAsync(funcionId, Butacas("B5"), "token-uno", null);
        var compra = await venta.PagarAsync(apartado.ApartadoId!.Value, Lineas("B5"),
            Canal.EnLinea, null, null, false, "clave-boleto");

        Assert.True(compra.Exitoso);

        var boleto = await datos.Boletos.SingleAsync();
        Assert.Equal(Tarifa.General, boleto.Tarifa);
        Assert.Equal(ServicioVenta.TarifaGeneralProvisional, boleto.Monto);
        Assert.Equal("B", boleto.Fila);
        Assert.Equal(5, boleto.Numero);

        var guardada = await datos.Compras.SingleAsync();
        Assert.Equal(Canal.EnLinea, guardada.Canal);
        Assert.Equal(EstadoPago.Pagada, guardada.EstadoPago);
        Assert.Null(guardada.Correo);
    }
}
