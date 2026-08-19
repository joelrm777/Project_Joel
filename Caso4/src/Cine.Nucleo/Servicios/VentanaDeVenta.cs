using Cine.Nucleo.Dominio;

namespace Cine.Nucleo.Servicios;

/// <summary>
/// Hasta cuándo se puede vender una función, según el canal (RN-28 a RN-31).
/// En línea, hasta el instante en que la función inicia. En taquilla, hasta 20 minutos después.
/// Pasados esos 20 minutos la función queda cerrada para todos.
/// </summary>
public static class VentanaDeVenta
{
    public static readonly TimeSpan MargenDeTaquilla = TimeSpan.FromMinutes(20);

    public static bool Permite(Funcion funcion, Canal canal, DateTime ahora)
    {
        if (funcion.Estado == EstadoFuncion.Cancelada)
        {
            return false;
        }

        return ahora < Cierre(funcion, canal);
    }

    /// <summary>Cuándo deja de vender este canal.</summary>
    public static DateTime Cierre(Funcion funcion, Canal canal) => canal switch
    {
        Canal.EnLinea => funcion.InicioLocal,
        Canal.Taquilla => funcion.InicioLocal.Add(MargenDeTaquilla),
        _ => funcion.InicioLocal
    };

    /// <summary>
    /// Cuándo la función queda cerrada para cualquier canal: apartar después de eso no tiene
    /// para qué (RN-30).
    /// </summary>
    public static bool AdmiteApartar(Funcion funcion, DateTime ahora) =>
        funcion.Estado != EstadoFuncion.Cancelada
        && ahora < funcion.InicioLocal.Add(MargenDeTaquilla);
}
