using Cine.Nucleo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Datos;

/// <summary>
/// Las dos salas del cine con sus butacas y el catálogo de películas, sembrados por migración.
/// RN-1 es un dato fijo del cine, no algo que alguien deba digitar (DISENO.md, otras decisiones).
/// </summary>
public static class SemillaCatalogo
{
    public const int SalaUnoId = 1;
    public const int SalaDosId = 2;

    /// <summary>Butacas por fila en las dos salas: 12 caben en la pantalla de un teléfono (RNF-6).</summary>
    public const int ButacasPorFila = 12;

    public static readonly string[] FilasSalaUno = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];
    public static readonly string[] FilasSalaDos = ["A", "B", "C", "D", "E"];

    /// <summary>Butacas que nunca se venden: vista tapada por la columna y espacio de silla de ruedas (RN-2).</summary>
    public static readonly (int SalaId, string Fila, int Numero)[] NoVendibles =
    [
        (SalaUnoId, "A", 1),
        (SalaUnoId, "A", 12),
        (SalaUnoId, "J", 6)
    ];

    public static void Aplicar(ModelBuilder modelo)
    {
        modelo.Entity<Sala>().HasData(
            new Sala { Id = SalaUnoId, Nombre = "Sala 1", TotalButacas = 120 },
            new Sala { Id = SalaDosId, Nombre = "Sala 2", TotalButacas = 60 });

        modelo.Entity<Pelicula>().HasData(
            new Pelicula { Id = 1, Titulo = "El Último Autobús", DuracionMinutos = 100, ClasificacionEdad = 0 },
            new Pelicula { Id = 2, Titulo = "Noche de Tormenta", DuracionMinutos = 120, ClasificacionEdad = 12 },
            new Pelicula { Id = 3, Titulo = "Camino al Volcán", DuracionMinutos = 95, ClasificacionEdad = 16 });

        // Los montos con los que el cine arranca. La administradora los cambia en la pieza 6,
        // insertando una fila nueva; esta nunca se actualiza (RN-11, RN-14).
        modelo.Entity<ConfiguracionTarifa>().HasData(new ConfiguracionTarifa
        {
            Id = 1,
            MontoGeneral = 3500m,
            MontoEstudiante = 2500m,
            VigenteDesde = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            CuentaId = null
        });

        modelo.Entity<ButacaSala>().HasData(Butacas());
    }

    /// <summary>Las 180 butacas de las dos salas, con las tres no vendibles ya marcadas.</summary>
    public static IEnumerable<ButacaSala> Butacas()
    {
        foreach (var (salaId, filas) in new[] { (SalaUnoId, FilasSalaUno), (SalaDosId, FilasSalaDos) })
        {
            foreach (var fila in filas)
            {
                for (var numero = 1; numero <= ButacasPorFila; numero++)
                {
                    yield return new ButacaSala
                    {
                        SalaId = salaId,
                        Fila = fila,
                        Numero = numero,
                        EsVendible = !NoVendibles.Contains((salaId, fila, numero))
                    };
                }
            }
        }
    }
}
