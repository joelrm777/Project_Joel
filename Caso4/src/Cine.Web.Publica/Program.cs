using Cine.Nucleo.Datos;
using Cine.Nucleo.Servicios;
using Cine.Nucleo.Tiempo;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Los estados de butaca viajan como texto: el mapa del teléfono los usa tal cual.
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<CineDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("CineDb")));

builder.Services.AddSingleton<IRelojCine, RelojCine>();
builder.Services.AddScoped<IServicioCartelera, ServicioCartelera>();

var app = builder.Build();

// La cartelera de prueba de la semana en curso. No repite lo que ya sembró.
using (var alcance = app.Services.CreateScope())
{
    var datos = alcance.ServiceProvider.GetRequiredService<CineDbContext>();
    var reloj = alcance.ServiceProvider.GetRequiredService<IRelojCine>();
    var creadas = await SemillaCartelera.AplicarAsync(datos, reloj);
    app.Logger.LogInformation("Cartelera de prueba: {Creadas} funciones creadas.", creadas);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// Lo que consulta el mapa cada 5 segundos para redibujarse sin recargar la página.
app.MapGet("/api/funciones/{funcionId:int}/mapa",
    async (int funcionId, IServicioCartelera cartelera) =>
    {
        var mapa = await cartelera.ObtenerMapaAsync(funcionId);
        return mapa is null ? Results.NotFound() : Results.Ok(mapa);
    });

app.Run();

/// <summary>Punto de entrada expuesto para las pruebas de la aplicación.</summary>
public partial class Program;
