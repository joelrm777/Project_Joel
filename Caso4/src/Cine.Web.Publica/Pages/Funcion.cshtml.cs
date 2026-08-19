using Cine.Nucleo.Contratos;
using Cine.Nucleo.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cine.Web.Publica.Pages;

/// <summary>El mapa de butacas de una función, con el estado de cada una (RF-9).</summary>
public class FuncionModel(IServicioCartelera cartelera) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int FuncionId { get; set; }

    public MapaFuncion? Mapa { get; private set; }

    public IReadOnlyList<IGrouping<string, ButacaEstado>> Filas { get; private set; } = [];

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
        return Page();
    }
}
