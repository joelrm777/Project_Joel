using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Servicios;

/// <summary>
/// Lo que cualquiera puede consultar sin identificarse: la cartelera de la semana (RF-6) y el
/// mapa de butacas de una función con el estado de cada una (RF-9).
/// </summary>
public interface IServicioCartelera
{
    Task<IReadOnlyList<FuncionEnCartelera>> ObtenerCarteleraAsync(DateOnly desde, DateOnly hasta);
    Task<MapaFuncion?> ObtenerMapaAsync(int funcionId);
}

public class ServicioCartelera(CineDbContext datos) : IServicioCartelera
{
    public async Task<IReadOnlyList<FuncionEnCartelera>> ObtenerCarteleraAsync(DateOnly desde, DateOnly hasta)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await datos.Funciones
            .Where(f => f.InicioLocal >= inicio && f.InicioLocal < fin)
            .OrderBy(f => f.InicioLocal)
            .ThenBy(f => f.SalaId)
            .Select(f => new FuncionEnCartelera(
                f.Id,
                f.Pelicula!.Titulo,
                f.Pelicula.ClasificacionEdad,
                f.Pelicula.DuracionMinutos,
                f.Sala!.Nombre,
                f.InicioLocal,
                f.AforoVendible))
            .ToListAsync();
    }

    public async Task<MapaFuncion?> ObtenerMapaAsync(int funcionId)
    {
        var funcion = await datos.Funciones
            .Include(f => f.Pelicula)
            .Include(f => f.Sala)
            .FirstOrDefaultAsync(f => f.Id == funcionId);

        if (funcion is null)
        {
            return null;
        }

        // La disposición de butacas es la de la sala y no cambia por función (RN-1). Las no
        // vendibles son las que quedaron congeladas al crear la función, no las de hoy (RN-4).
        var disposicion = await datos.ButacasSala
            .Where(b => b.SalaId == funcion.SalaId)
            .OrderBy(b => b.Fila).ThenBy(b => b.Numero)
            .Select(b => new { b.Fila, b.Numero })
            .ToListAsync();

        var noVendibles = await datos.ButacasNoVendiblesFuncion
            .Where(b => b.FuncionId == funcionId)
            .Select(b => new { b.Fila, b.Numero })
            .ToListAsync();

        var ocupadas = await datos.OcupacionesButaca
            .Where(o => o.FuncionId == funcionId)
            .Select(o => new { o.Fila, o.Numero, o.Estado })
            .ToListAsync();

        var noVendiblesPorButaca = noVendibles
            .Select(b => (b.Fila, b.Numero))
            .ToHashSet();

        var estadoOcupada = ocupadas
            .ToDictionary(o => (o.Fila, o.Numero), o => o.Estado);

        var butacas = disposicion
            .Select(b => new ButacaEstado(b.Fila, b.Numero, EstadoDe(b.Fila, b.Numero)))
            .ToList();

        return new MapaFuncion(
            funcion.Id,
            funcion.Pelicula!.Titulo,
            funcion.Sala!.Nombre,
            funcion.InicioLocal,
            funcion.Pelicula.ClasificacionEdad,
            funcion.Estado,
            butacas);

        EstadoButaca EstadoDe(string fila, int numero)
        {
            if (noVendiblesPorButaca.Contains((fila, numero)))
            {
                return EstadoButaca.NoVendible;
            }

            return estadoOcupada.TryGetValue((fila, numero), out var estado)
                ? estado
                : EstadoButaca.Libre;
        }
    }
}
