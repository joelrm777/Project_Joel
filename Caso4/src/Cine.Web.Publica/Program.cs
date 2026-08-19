using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Cine.Nucleo.Servicios;
using Cine.Nucleo.Tiempo;
using Cine.Web.Publica;
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
builder.Services.AddScoped<IServicioVenta, ServicioVenta>();

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

// Elegir butacas, soltar una y pagar. El motivo del rechazo se devuelve tal como lo da el
// núcleo, junto con el mapa actualizado cuando la butaca ya no está disponible (RF-12).
app.MapPost("/api/funciones/{funcionId:int}/apartar",
    async (int funcionId, PeticionApartar peticion, IServicioVenta venta, HttpContext contexto) =>
    {
        var butacas = peticion.Butacas.Select(b => new Butaca(b.Fila, b.Numero)).ToList();
        var resultado = await venta.ApartarAsync(funcionId, butacas,
            SesionDelVisitante.Token(contexto), peticion.ApartadoId);

        return resultado.Aceptado ? Results.Ok(resultado) : Results.BadRequest(resultado);
    });

app.MapPost("/api/apartados/{apartadoId:int}/liberar",
    async (int apartadoId, ButacaPedida butaca, IServicioVenta venta) =>
    {
        var resultado = await venta.LiberarButacaAsync(apartadoId, new Butaca(butaca.Fila, butaca.Numero));
        return resultado.Exitoso ? Results.Ok(resultado) : Results.BadRequest(resultado);
    });

app.MapPost("/api/apartados/{apartadoId:int}/pagar",
    async (int apartadoId, PeticionPago peticion, IServicioVenta venta) =>
    {
        var lineas = peticion.Butacas
            .Select(b => new LineaTarifa(b.Fila, b.Numero, Tarifa.General))
            .ToList();

        var resultado = await venta.PagarAsync(apartadoId, lineas, Canal.EnLinea,
            peticion.Correo, null, peticion.EdadDeclarada, peticion.ClaveIdempotencia);

        return resultado.Exitoso ? Results.Ok(resultado) : Results.BadRequest(resultado);
    });

app.Run();

public record ButacaPedida(string Fila, int Numero);
public record PeticionApartar(IReadOnlyList<ButacaPedida> Butacas, int? ApartadoId);
public record PeticionPago(IReadOnlyList<ButacaPedida> Butacas, string? Correo, bool EdadDeclarada, string ClaveIdempotencia);

/// <summary>Punto de entrada expuesto para las pruebas de la aplicación.</summary>
public partial class Program;
