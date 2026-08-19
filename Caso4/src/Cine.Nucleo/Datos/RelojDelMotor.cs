using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Datos;

/// <summary>
/// La hora de los vencimientos sale del motor de base de datos, no del servidor de aplicación:
/// dos aplicaciones separadas pueden tener relojes distintos y los vencimientos deben ser
/// comparables entre ellas (DISENO.md, otras decisiones).
/// </summary>
public static class RelojDelMotor
{
    public static async Task<DateTime> AhoraAsync(CineDbContext datos)
    {
        var horas = await datos.Database
            .SqlQuery<DateTime>($"SELECT SYSDATETIME() AS Value")
            .ToListAsync();

        return horas[0];
    }
}
