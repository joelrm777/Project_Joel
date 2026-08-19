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
        });

        SemillaCatalogo.Aplicar(modelo);
    }
}
