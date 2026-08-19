using Cine.Nucleo.Dominio;
using Cine.Nucleo.Tiempo;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Datos;

/// <summary>
/// La cartelera de prueba: las funciones de la semana en curso y de la siguiente, de jueves a
/// miércoles (RN-7), que son las dos que la especificación permite programar.
/// No va en la migración porque sus fechas dependen del día en que se recrean los datos; se
/// aplica al arrancar y no repite lo que ya sembró. La programación de verdad la hace la
/// administradora en la pieza 6.
/// </summary>
public static class SemillaCartelera
{
    /// <summary>Función de la semana: día contado desde el jueves, hora, sala y película.</summary>
    private record FuncionSembrada(int DiaDesdeJueves, int Hora, int Minuto, int SalaId, int PeliculaId);

    private static readonly FuncionSembrada[] Programacion =
    [
        new FuncionSembrada(0, 19, 0, SemillaCatalogo.SalaUnoId, 1),
        new FuncionSembrada(0, 20, 0, SemillaCatalogo.SalaDosId, 2),
        new FuncionSembrada(1, 17, 0, SemillaCatalogo.SalaUnoId, 1),
        new FuncionSembrada(1, 19, 0, SemillaCatalogo.SalaUnoId, 2),
        new FuncionSembrada(1, 21, 30, SemillaCatalogo.SalaUnoId, 3),
        new FuncionSembrada(1, 18, 0, SemillaCatalogo.SalaDosId, 3),
        new FuncionSembrada(1, 20, 0, SemillaCatalogo.SalaDosId, 1),
        new FuncionSembrada(2, 15, 0, SemillaCatalogo.SalaUnoId, 1),
        new FuncionSembrada(2, 17, 0, SemillaCatalogo.SalaUnoId, 2),
        new FuncionSembrada(2, 19, 30, SemillaCatalogo.SalaUnoId, 3),
        new FuncionSembrada(2, 18, 0, SemillaCatalogo.SalaDosId, 2),
        new FuncionSembrada(3, 16, 0, SemillaCatalogo.SalaUnoId, 1),
        // Miércoles: la función que la tarifa de miércoles necesita para verse (RN-12, pieza 3).
        new FuncionSembrada(6, 19, 0, SemillaCatalogo.SalaUnoId, 2),
        new FuncionSembrada(6, 20, 0, SemillaCatalogo.SalaDosId, 3)
    ];

    public static async Task<int> AplicarAsync(CineDbContext datos, IRelojCine reloj)
    {
        var (desde, hasta) = SemanaDeCartelera.Contiene(reloj.HoyLocal);
        var inicioSemana = desde.ToDateTime(TimeOnly.MinValue);

        // Se siembran dos semanas: la que corre y la siguiente. Sin la segunda, un martes casi
        // toda la cartelera ya pasó y no quedan funciones que se puedan comprar.
        var finSegundaSemana = hasta.AddDays(8).ToDateTime(TimeOnly.MinValue);

        var yaHayCartelera = await datos.Funciones
            .AnyAsync(f => f.InicioLocal >= inicioSemana && f.InicioLocal < finSegundaSemana);

        if (yaHayCartelera)
        {
            return 0;
        }

        // El aforo vendible y las butacas no vendibles se congelan al crear la función
        // (RN-3, RN-4): se copian de la plantilla vigente de la sala en este momento.
        var plantilla = await datos.ButacasSala
            .Select(b => new { b.SalaId, b.Fila, b.Numero, b.EsVendible })
            .ToListAsync();

        var creadas = 0;

        foreach (var (dia, hora, minuto, salaId, peliculaId) in Programacion
                     .SelectMany(f => new[] { f, f with { DiaDesdeJueves = f.DiaDesdeJueves + 7 } }))
        {
            var noVendibles = plantilla
                .Where(b => b.SalaId == salaId && !b.EsVendible)
                .ToList();

            var funcion = new Funcion
            {
                PeliculaId = peliculaId,
                SalaId = salaId,
                InicioLocal = desde.AddDays(dia).ToDateTime(new TimeOnly(hora, minuto)),
                AforoVendible = plantilla.Count(b => b.SalaId == salaId && b.EsVendible),
                Estado = EstadoFuncion.Activa,
                ButacasNoVendibles = [.. noVendibles.Select(b => new ButacaNoVendibleFuncion
                {
                    Fila = b.Fila,
                    Numero = b.Numero
                })]
            };

            datos.Funciones.Add(funcion);
            creadas++;
        }

        await datos.SaveChangesAsync();
        return creadas;
    }
}
