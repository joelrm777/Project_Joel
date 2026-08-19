using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cine.Nucleo.Datos;

/// <summary>
/// Solo para las herramientas de migración de Entity Framework Core, que necesitan construir el
/// contexto fuera de una aplicación. En desarrollo la base corre sobre SQL Server LocalDB
/// (DISENO.md, decisión 6 y su nota de entorno).
/// </summary>
public class CineDbContextFactory : IDesignTimeDbContextFactory<CineDbContext>
{
    public const string CadenaDesarrollo =
        @"Server=(localdb)\MSSQLLocalDB;Database=CineVariedades;Trusted_Connection=True;TrustServerCertificate=True";

    public CineDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<CineDbContext>()
            .UseSqlServer(Environment.GetEnvironmentVariable("CINE_CADENA_CONEXION") ?? CadenaDesarrollo)
            .Options;

        return new CineDbContext(opciones);
    }
}
