namespace Cine.Nucleo.Tiempo;

/// <summary>
/// La hora del cine. Toda hora se guarda y se muestra en hora local de Costa Rica, que es la
/// única zona del cine, y así «funciones cuya fecha de inicio cae en miércoles» (RN-12) se
/// evalúa sin conversiones.
/// Los vencimientos del apartado no salen de aquí: los toma del motor de base de datos (pieza 2).
/// </summary>
public interface IRelojCine
{
    DateTime AhoraLocal { get; }
    DateOnly HoyLocal { get; }
}

public class RelojCine : IRelojCine
{
    private static readonly TimeZoneInfo ZonaDelCine = ObtenerZona();

    public DateTime AhoraLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaDelCine);

    public DateOnly HoyLocal => DateOnly.FromDateTime(AhoraLocal);

    private static TimeZoneInfo ObtenerZona()
    {
        foreach (var id in new[] { "Central America Standard Time", "America/Costa_Rica" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Se prueba el identificador de la otra plataforma.
            }
        }

        throw new InvalidOperationException("No se encontró la zona horaria de Costa Rica en este sistema.");
    }
}

/// <summary>Una semana de cartelera empieza el jueves y termina el miércoles siguiente (RN-7).</summary>
public static class SemanaDeCartelera
{
    /// <summary>La semana de cartelera —de jueves a miércoles— que contiene el día dado.</summary>
    public static (DateOnly Desde, DateOnly Hasta) Contiene(DateOnly dia)
    {
        var diasDesdeJueves = ((int)dia.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        var jueves = dia.AddDays(-diasDesdeJueves);
        return (jueves, jueves.AddDays(6));
    }
}
