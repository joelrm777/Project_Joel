namespace Cine.Nucleo.Dominio;

/// <summary>Origen de la compra (glosario: canal).</summary>
public enum Canal
{
    EnLinea,
    Taquilla
}

/// <summary>Regla de precio aplicable a una butaca (RN-11 a RN-14).</summary>
public enum Tarifa
{
    General,
    Miercoles,
    Estudiante
}

/// <summary>Estado de pago de una compra (RN-27).</summary>
public enum EstadoPago
{
    Pagada,
    Reembolsada
}

/// <summary>
/// Butacas de una función retenidas para un comprador que aún no pagó (RN-18 a RN-20).
/// Un apartado por intento de compra, con sus 1 a 10 butacas y un solo vencimiento.
/// </summary>
public class Apartado
{
    public int Id { get; set; }
    public int FuncionId { get; set; }
    public Funcion? Funcion { get; set; }

    /// <summary>Cookie anónima del comprador o identificador de sesión del operador (RN-19).</summary>
    public string TokenSesion { get; set; } = string.Empty;

    public DateTime CreadoEn { get; set; }

    /// <summary>Se recalcula a «ahora más 10 minutos» cada vez que se le agrega una butaca (RN-18).</summary>
    public DateTime VenceEn { get; set; }

    public List<OcupacionButaca> Ocupaciones { get; set; } = [];
}

/// <summary>Operación por la que uno o más boletos de una misma función quedan pagados.</summary>
public class Compra
{
    public int Id { get; set; }

    /// <summary>Identificador corto que se le entrega al comprador, único e irrepetible (RN-25).</summary>
    public string Codigo { get; set; } = string.Empty;

    public int FuncionId { get; set; }
    public Funcion? Funcion { get; set; }
    public Canal Canal { get; set; }
    public DateTime PagadaEn { get; set; }

    /// <summary>Único dato personal, y solo cuando la compra fue en línea.</summary>
    public string? Correo { get; set; }

    /// <summary>Cuenta que la registró cuando fue en taquilla (REG-2). La usa la pieza 4.</summary>
    public int? CuentaId { get; set; }

    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pagada;

    /// <summary>Se cobró alguna diferencia en puerta (RN-27). La usa la pieza 5.</summary>
    public bool Ajustada { get; set; }

    /// <summary>Cuándo se admitió a la sala (RN-27). La usa la pieza 5.</summary>
    public DateTime? IngresadaEn { get; set; }

    /// <summary>Lo que hace que dos confirmaciones de pago dejen una sola compra (RN-23).</summary>
    public string ClaveIdempotencia { get; set; } = string.Empty;

    public List<Boleto> Boletos { get; set; } = [];
}

/// <summary>Una butaca de una función vendida dentro de una compra (RN-16).</summary>
public class Boleto
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }
    public int FuncionId { get; set; }
    public string Fila { get; set; } = string.Empty;
    public int Numero { get; set; }

    /// <summary>Tarifa aplicada, grabada en el momento de la compra.</summary>
    public Tarifa Tarifa { get; set; }

    /// <summary>Monto cobrado, grabado en el momento de la compra. No lo altera un cambio posterior.</summary>
    public decimal Monto { get; set; }
}
