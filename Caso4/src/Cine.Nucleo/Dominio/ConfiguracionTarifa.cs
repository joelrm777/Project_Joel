namespace Cine.Nucleo.Dominio;

/// <summary>
/// Los montos que la administradora fijó, con la fecha desde la que rigen. Nunca se actualiza una
/// fila existente: cada cambio inserta una nueva, y así se puede explicar el monto de un boleto
/// viejo aunque RN-16 ya lo proteja al grabarlo (DISENO.md, otras decisiones).
/// </summary>
public class ConfiguracionTarifa
{
    public int Id { get; set; }

    /// <summary>Monto único del cine para la tarifa general (RN-11).</summary>
    public decimal MontoGeneral { get; set; }

    /// <summary>Monto de la tarifa estudiante, menor que la general (RN-14).</summary>
    public decimal MontoEstudiante { get; set; }

    public DateTime VigenteDesde { get; set; }

    /// <summary>Cuenta de la administradora que la fijó. La usa la pieza 6.</summary>
    public int? CuentaId { get; set; }
}
