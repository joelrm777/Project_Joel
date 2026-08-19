using Cine.Nucleo.Contratos;
using Cine.Nucleo.Servicios;
using Cine.Nucleo.Tiempo;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cine.Web.Publica.Pages;

/// <summary>La cartelera de la semana, que cualquiera consulta sin identificarse (RF-6).</summary>
public class IndexModel(IServicioCartelera cartelera, IRelojCine reloj) : PageModel
{
    public DateOnly Desde { get; private set; }
    public DateOnly Hasta { get; private set; }
    public IReadOnlyList<IGrouping<DateOnly, FuncionEnCartelera>> PorDia { get; private set; } = [];

    public async Task OnGetAsync()
    {
        (Desde, Hasta) = SemanaDeCartelera.Contiene(reloj.HoyLocal);

        var funciones = await cartelera.ObtenerCarteleraAsync(Desde, Hasta);

        PorDia = [.. funciones.GroupBy(f => DateOnly.FromDateTime(f.InicioLocal)).OrderBy(g => g.Key)];
    }
}
