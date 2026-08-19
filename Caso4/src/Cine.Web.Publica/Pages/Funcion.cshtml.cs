using Cine.Nucleo.Contratos;
using Cine.Nucleo.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cine.Web.Publica.Pages;

/// <summary>
/// El mapa de butacas de una función, con el estado de cada una (RF-9), las tarifas que esa
/// función admite (RF-8) y la declaración de edad cuando la película la exige (RF-17).
/// </summary>
public class FuncionModel(IServicioCartelera cartelera, IServicioTarifas tarifas) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int FuncionId { get; set; }

    public MapaFuncion? Mapa { get; private set; }

    public IReadOnlyList<IGrouping<string, ButacaEstado>> Filas { get; private set; } = [];

    public IReadOnlyList<OpcionTarifa> Tarifas { get; private set; } = [];

    /// <summary>La película tiene edad mínima, así que hay que declararla antes de pagar.</summary>
    public bool ExigeDeclaracionDeEdad => Mapa is not null && Mapa.ClasificacionEdad > 0;

    public async Task<IActionResult> OnGetAsync()
    {
        Mapa = await cartelera.ObtenerMapaAsync(FuncionId);

        if (Mapa is null)
        {
            // La función no existe: se dice eso, no se muestra un error del servidor.
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Page();
        }

        Filas = [.. Mapa.Butacas.GroupBy(b => b.Fila).OrderBy(g => g.Key)];
        Tarifas = await tarifas.TarifasDisponiblesAsync(FuncionId);
        return Page();
    }

    public static string NombreDeTarifa(Cine.Nucleo.Dominio.Tarifa tarifa) => tarifa switch
    {
        Cine.Nucleo.Dominio.Tarifa.General => "general",
        Cine.Nucleo.Dominio.Tarifa.Miercoles => "miércoles",
        Cine.Nucleo.Dominio.Tarifa.Estudiante => "estudiante",
        _ => tarifa.ToString().ToLowerInvariant()
    };
}
