using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Cine.Nucleo.Tiempo;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Pruebas;

/// <summary>
/// Una base de pruebas real sobre SQL Server LocalDB —el mismo motor de desarrollo— porque las
/// reglas que estas pruebas comprueban las hace cumplir el motor, no el código (DISENO.md,
/// decisión 1). Cada prueba trabaja sobre su propia base y la borra al terminar.
/// </summary>
public sealed class BaseDePruebas : IDisposable
{
    private readonly string _nombre = "CinePruebas_" + Guid.NewGuid().ToString("N");

    public BaseDePruebas()
    {
        using var datos = Abrir();
        datos.Database.EnsureCreated();
    }

    public CineDbContext Abrir()
    {
        var opciones = new DbContextOptionsBuilder<CineDbContext>()
            .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={_nombre};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new CineDbContext(opciones);
    }

    /// <summary>Deja la cartelera de la semana que contiene el día dado y devuelve sus funciones.</summary>
    public async Task<int> SembrarCarteleraAsync(DateOnly dia)
    {
        using var datos = Abrir();
        return await SemillaCartelera.AplicarAsync(datos, new RelojFijo(dia));
    }

    public async Task<Funcion> PrimeraFuncionDeAsync(int salaId)
    {
        using var datos = Abrir();
        return await datos.Funciones
            .Where(f => f.SalaId == salaId)
            .OrderBy(f => f.InicioLocal)
            .FirstAsync();
    }

    public void Dispose()
    {
        using var datos = Abrir();
        datos.Database.EnsureDeleted();
    }
}

/// <summary>Un reloj que siempre marca el mismo día, para que las pruebas no dependan de hoy.</summary>
public sealed class RelojFijo(DateOnly dia) : IRelojCine
{
    public DateTime AhoraLocal { get; } = dia.ToDateTime(new TimeOnly(10, 0));
    public DateOnly HoyLocal { get; } = dia;
}
