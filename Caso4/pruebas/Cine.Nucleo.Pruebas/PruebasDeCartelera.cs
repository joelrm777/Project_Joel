using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Cine.Nucleo.Servicios;
using Cine.Nucleo.Tiempo;

namespace Cine.Nucleo.Pruebas;

/// <summary>Comprobación de la pieza 1 del plan: la cartelera de la semana y el mapa de butacas.</summary>
public class PruebasDeCartelera : IClassFixture<BaseDePruebas>
{
    private readonly BaseDePruebas _base;

    /// <summary>Un jueves cualquiera: el primer día de una semana de cartelera (RN-7).</summary>
    private static readonly DateOnly Jueves = new(2026, 8, 20);

    public PruebasDeCartelera(BaseDePruebas baseDePruebas)
    {
        _base = baseDePruebas;
        _base.SembrarCarteleraAsync(Jueves).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task El_mapa_de_la_Sala_1_tiene_120_butacas_con_las_no_vendibles_marcadas()
    {
        using var datos = _base.Abrir();
        var cartelera = new ServicioCartelera(datos);
        var funcion = await _base.PrimeraFuncionDeAsync(SemillaCatalogo.SalaUnoId);

        var mapa = await cartelera.ObtenerMapaAsync(funcion.Id);

        Assert.NotNull(mapa);
        Assert.Equal(120, mapa.Butacas.Count);
        Assert.Equal(3, mapa.Butacas.Count(b => b.Estado == EstadoButaca.NoVendible));
        Assert.Equal(117, mapa.Butacas.Count(b => b.Estado == EstadoButaca.Libre));
        Assert.Contains(mapa.Butacas, b => b.Fila == "A" && b.Numero == 1 && b.Estado == EstadoButaca.NoVendible);
        Assert.Contains(mapa.Butacas, b => b.Fila == "J" && b.Numero == 6 && b.Estado == EstadoButaca.NoVendible);
    }

    [Fact]
    public async Task El_aforo_vendible_queda_congelado_al_crear_la_funcion()
    {
        var funcionSalaUno = await _base.PrimeraFuncionDeAsync(SemillaCatalogo.SalaUnoId);
        var funcionSalaDos = await _base.PrimeraFuncionDeAsync(SemillaCatalogo.SalaDosId);

        Assert.Equal(117, funcionSalaUno.AforoVendible);
        Assert.Equal(60, funcionSalaDos.AforoVendible);
    }

    [Fact]
    public async Task Una_butaca_ocupada_se_ve_vendida_en_el_mapa()
    {
        using var datos = _base.Abrir();
        var cartelera = new ServicioCartelera(datos);
        // La Sala 2 para no tocar el mapa que comprueba la otra prueba de esta clase.
        var funcion = await _base.PrimeraFuncionDeAsync(SemillaCatalogo.SalaDosId);

        datos.OcupacionesButaca.Add(new OcupacionButaca
        {
            FuncionId = funcion.Id,
            Fila = "C",
            Numero = 4,
            Estado = EstadoButaca.Vendida
        });
        await datos.SaveChangesAsync();

        var mapa = await cartelera.ObtenerMapaAsync(funcion.Id);

        Assert.NotNull(mapa);
        Assert.Equal(60, mapa.Butacas.Count);
        var c4 = mapa.Butacas.Single(b => b.Fila == "C" && b.Numero == 4);
        Assert.Equal(EstadoButaca.Vendida, c4.Estado);
        Assert.Equal(59, mapa.Butacas.Count(b => b.Estado == EstadoButaca.Libre));
    }

    [Fact]
    public async Task La_cartelera_devuelve_las_funciones_de_la_semana_de_jueves_a_miercoles()
    {
        using var datos = _base.Abrir();
        var cartelera = new ServicioCartelera(datos);
        var (desde, hasta) = SemanaDeCartelera.Contiene(Jueves);

        var funciones = await cartelera.ObtenerCarteleraAsync(desde, hasta);

        Assert.Equal(14, funciones.Count);
        Assert.All(funciones, f => Assert.InRange(DateOnly.FromDateTime(f.InicioLocal), desde, hasta));
        Assert.Equal(DayOfWeek.Thursday, desde.DayOfWeek);
        Assert.Equal(DayOfWeek.Wednesday, hasta.DayOfWeek);
        Assert.Contains(funciones, f => f.InicioLocal.DayOfWeek == DayOfWeek.Wednesday);
        Assert.Equal(funciones.OrderBy(f => f.InicioLocal).Select(f => f.FuncionId), funciones.Select(f => f.FuncionId));
    }

    [Fact]
    public async Task Una_funcion_inexistente_no_tiene_mapa()
    {
        using var datos = _base.Abrir();
        var cartelera = new ServicioCartelera(datos);

        Assert.Null(await cartelera.ObtenerMapaAsync(9999));
    }

    [Fact]
    public void La_semana_de_cartelera_va_del_jueves_al_miercoles()
    {
        var (desdeDomingo, hastaDomingo) = SemanaDeCartelera.Contiene(new DateOnly(2026, 8, 23));
        var (desdeMiercoles, hastaMiercoles) = SemanaDeCartelera.Contiene(new DateOnly(2026, 8, 26));

        Assert.Equal(new DateOnly(2026, 8, 20), desdeDomingo);
        Assert.Equal(new DateOnly(2026, 8, 26), hastaDomingo);
        Assert.Equal(new DateOnly(2026, 8, 20), desdeMiercoles);
        Assert.Equal(new DateOnly(2026, 8, 26), hastaMiercoles);
    }

    [Fact]
    public async Task La_semilla_no_repite_la_cartelera_que_ya_sembro()
    {
        var creadas = await _base.SembrarCarteleraAsync(Jueves);

        Assert.Equal(0, creadas);
    }
}
