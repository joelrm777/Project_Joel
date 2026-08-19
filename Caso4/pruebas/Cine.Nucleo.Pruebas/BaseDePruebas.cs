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

    /// <summary>La primera función de la semana que cae en el día pedido.</summary>
    public async Task<Funcion> PrimeraFuncionDelDiaAsync(DayOfWeek dia)
    {
        using var datos = Abrir();
        var funciones = await datos.Funciones.OrderBy(f => f.InicioLocal).ToListAsync();
        return funciones.First(f => f.InicioLocal.DayOfWeek == dia);
    }

    /// <summary>Una función puesta a una hora concreta, para comprobar la ventana de venta.</summary>
    public async Task<int> CrearFuncionAsync(int peliculaId, int salaId, DateTime inicioLocal)
    {
        using var datos = Abrir();

        var plantilla = await datos.ButacasSala
            .Where(b => b.SalaId == salaId)
            .Select(b => new { b.Fila, b.Numero, b.EsVendible })
            .ToListAsync();

        var funcion = new Funcion
        {
            PeliculaId = peliculaId,
            SalaId = salaId,
            InicioLocal = inicioLocal,
            AforoVendible = plantilla.Count(b => b.EsVendible),
            ButacasNoVendibles = [.. plantilla.Where(b => !b.EsVendible)
                .Select(b => new ButacaNoVendibleFuncion { Fila = b.Fila, Numero = b.Numero })]
        };

        datos.Funciones.Add(funcion);
        await datos.SaveChangesAsync();
        return funcion.Id;
    }

    /// <summary>La hora del motor, que es la que manda en los vencimientos y las ventanas.</summary>
    public async Task<DateTime> AhoraDelMotorAsync()
    {
        using var datos = Abrir();
        return await RelojDelMotor.AhoraAsync(datos);
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
