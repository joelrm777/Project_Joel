using System.Security.Cryptography;

namespace Cine.Nucleo.Servicios;

/// <summary>
/// «CV-» más 5 caracteres sin vocales ni caracteres ambiguos: el operador lo escucha y lo digita,
/// así que no puede confundir O con 0 ni I con 1 (DISENO.md, otras decisiones; RF-18).
/// </summary>
public static class CodigoDeConfirmacion
{
    private const string Alfabeto = "BCDFGHJKLMNPQRSTVWXYZ23456789";
    private const int Largo = 5;

    public static string Nuevo()
    {
        var caracteres = new char[Largo];

        for (var i = 0; i < Largo; i++)
        {
            caracteres[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return "CV-" + new string(caracteres);
    }
}
