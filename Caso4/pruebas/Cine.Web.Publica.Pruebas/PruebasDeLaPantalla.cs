using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cine.Web.Publica.Pruebas;

/// <summary>
/// Lo que un navegador de verdad recibe al abrir la aplicación. Nace de un defecto real: la hoja
/// de estilo y el JavaScript llegaban vacíos —200 con cuerpo de cero bytes— a cualquier cliente
/// que pidiera gzip, que son todos, y la pantalla se veía sin diseño y sin poder elegir butacas.
/// </summary>
public class PruebasDeLaPantalla(WebApplicationFactory<Program> fabrica)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient NavegadorAsync()
    {
        var cliente = fabrica.CreateClient();

        // Las cabeceras con las que pide cualquier navegador.
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/140.0");
        cliente.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        cliente.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        cliente.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        return cliente;
    }

    [Theory]
    [InlineData("/css/cine.css", "text/css")]
    [InlineData("/js/mapa.js", "text/javascript")]
    public async Task Los_archivos_de_la_pantalla_llegan_con_contenido(string ruta, string tipoEsperado)
    {
        using var navegador = NavegadorAsync();

        var respuesta = await navegador.GetAsync(ruta);
        var contenido = await respuesta.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(tipoEsperado, respuesta.Content.Headers.ContentType?.MediaType ?? string.Empty);
        Assert.True(contenido.Length > 1000,
            $"«{ruta}» llegó con {contenido.Length} bytes: el navegador la recibe vacía.");
    }

    [Fact]
    public async Task La_hoja_de_estilo_trae_el_diseno_y_el_mapa_cabe_en_un_telefono()
    {
        using var navegador = NavegadorAsync();

        var hoja = await navegador.GetStringAsync("/css/cine.css");

        // La paleta de la sala y las clases que el mapa enciende desde JavaScript.
        Assert.Contains("--color-sala-950", hoja);
        Assert.Contains("--color-marquesina-500", hoja);
        Assert.Contains(".butaca", hoja);
        Assert.Contains(".mia", hoja);

        // La regla que achica la butaca en pantallas angostas (RNF-6).
        Assert.Contains("max-width:420px", hoja.Replace(" ", string.Empty));
    }

    [Fact]
    public async Task La_cartelera_enlaza_su_hoja_de_estilo_y_la_pagina_de_funcion_su_script()
    {
        using var navegador = NavegadorAsync();

        var cartelera = await navegador.GetStringAsync("/");

        Assert.Contains("/css/cine.css", cartelera);
        Assert.Contains("tarjeta-funcion", cartelera);
    }
}
