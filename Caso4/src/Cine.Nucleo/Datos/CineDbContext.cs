using Cine.Nucleo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Datos;

public class CineDbContext(DbContextOptions<CineDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Pelicula> Peliculas => Set<Pelicula>();
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<ButacaSala> ButacasSala => Set<ButacaSala>();
    public DbSet<Funcion> Funciones => Set<Funcion>();
    public DbSet<ButacaNoVendibleFuncion> ButacasNoVendiblesFuncion => Set<ButacaNoVendibleFuncion>();
    public DbSet<OcupacionButaca> OcupacionesButaca => Set<OcupacionButaca>();
    public DbSet<Apartado> Apartados => Set<Apartado>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<ConfiguracionTarifa> ConfiguracionesTarifa => Set<ConfiguracionTarifa>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Pelicula>(e =>
        {
            e.ToTable("Pelicula");
            e.Property(p => p.Titulo).HasMaxLength(120).IsRequired();
        });

        modelo.Entity<Sala>(e =>
        {
            e.ToTable("Sala");
            e.Property(s => s.Nombre).HasMaxLength(40).IsRequired();
        });

        modelo.Entity<ButacaSala>(e =>
        {
            e.ToTable("ButacaSala");
            e.HasKey(b => new { b.SalaId, b.Fila, b.Numero });
            e.Property(b => b.Fila).HasMaxLength(2).IsRequired();
            e.HasOne(b => b.Sala).WithMany(s => s.Butacas).HasForeignKey(b => b.SalaId);
        });

        modelo.Entity<Funcion>(e =>
        {
            e.ToTable("Funcion");
            e.Property(f => f.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(f => f.Pelicula).WithMany().HasForeignKey(f => f.PeliculaId);
            e.HasOne(f => f.Sala).WithMany().HasForeignKey(f => f.SalaId);
            e.HasIndex(f => new { f.SalaId, f.InicioLocal });
        });

        modelo.Entity<ButacaNoVendibleFuncion>(e =>
        {
            e.ToTable("ButacaNoVendibleFuncion");
            e.HasKey(b => new { b.FuncionId, b.Fila, b.Numero });
            e.Property(b => b.Fila).HasMaxLength(2).IsRequired();
            e.HasOne(b => b.Funcion).WithMany(f => f.ButacasNoVendibles).HasForeignKey(b => b.FuncionId);
        });

        modelo.Entity<OcupacionButaca>(e =>
        {
            e.ToTable("OcupacionButaca");
            e.Property(o => o.Fila).HasMaxLength(2).IsRequired();
            e.Property(o => o.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(o => o.Funcion).WithMany().HasForeignKey(o => o.FuncionId);

            // La regla que ningún código puede garantizar solo: una butaca de una función
            // pertenece a lo sumo a uno (decisión 1 de DISENO.md, CA-1, RNF-1).
            e.HasIndex(o => new { o.FuncionId, o.Fila, o.Numero }).IsUnique();

            e.HasOne(o => o.Apartado).WithMany(a => a.Ocupaciones)
                .HasForeignKey(o => o.ApartadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Apartado>(e =>
        {
            e.ToTable("Apartado");
            e.Property(a => a.TokenSesion).HasMaxLength(64).IsRequired();
            // Sin cascada desde la función: la ocupación ya cae en cascada por su función y por
            // su apartado, y dos caminos de borrado a la misma tabla no los acepta el motor.
            // Borrar el apartado y sus butacas es siempre un acto explícito del núcleo.
            e.HasOne(a => a.Funcion).WithMany().HasForeignKey(a => a.FuncionId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(a => a.VenceEn);
        });

        modelo.Entity<Compra>(e =>
        {
            e.ToTable("Compra");
            e.Property(c => c.Codigo).HasMaxLength(12).IsRequired();
            e.Property(c => c.Correo).HasMaxLength(200);
            e.Property(c => c.ClaveIdempotencia).HasMaxLength(64).IsRequired();
            e.Property(c => c.Canal).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.EstadoPago).HasConversion<string>().HasMaxLength(20);
            e.HasOne(c => c.Funcion).WithMany().HasForeignKey(c => c.FuncionId);

            // Un solo código por compra y una sola compra por clave de idempotencia (RN-23, RN-25).
            e.HasIndex(c => c.Codigo).IsUnique();
            e.HasIndex(c => c.ClaveIdempotencia).IsUnique();
        });

        modelo.Entity<Boleto>(e =>
        {
            e.ToTable("Boleto");
            e.Property(b => b.Fila).HasMaxLength(2).IsRequired();
            e.Property(b => b.Tarifa).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.Monto).HasPrecision(10, 2);
            e.HasOne(b => b.Compra).WithMany(c => c.Boletos).HasForeignKey(b => b.CompraId);
        });

        modelo.Entity<ConfiguracionTarifa>(e =>
        {
            e.ToTable("ConfiguracionTarifa");
            e.Property(c => c.MontoGeneral).HasPrecision(10, 2);
            e.Property(c => c.MontoEstudiante).HasPrecision(10, 2);
            e.HasIndex(c => c.VigenteDesde);
        });

        SemillaCatalogo.Aplicar(modelo);
    }
}
