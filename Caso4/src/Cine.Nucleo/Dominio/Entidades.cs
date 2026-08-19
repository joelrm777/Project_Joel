namespace Cine.Nucleo.Dominio;

/// <summary>Estado de una butaca dentro del mapa de una función (RN-17, RN-2).</summary>
public enum EstadoButaca
{
    Libre,
    Apartada,
    Vendida,
    NoVendible
}

/// <summary>Estado de una función (RN-31, RN-43).</summary>
public enum EstadoFuncion
{
    Activa,
    Cancelada
}

/// <summary>Puesto individual identificado por fila y número dentro de una sala.</summary>
public readonly record struct Butaca(string Fila, int Numero)
{
    public override string ToString() => $"{Fila}{Numero}";
}

/// <summary>Título exhibido, con su duración y su clasificación por edad (RN-32).</summary>
public class Pelicula
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public int ClasificacionEdad { get; set; }
}

/// <summary>Auditorio físico. Existen dos: Sala 1 con 120 butacas y Sala 2 con 60 (RN-1).</summary>
public class Sala
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int TotalButacas { get; set; }
    public List<ButacaSala> Butacas { get; set; } = [];
}

/// <summary>Plantilla actual de butacas de una sala. Marcarla no vendible solo afecta funciones futuras (RN-4).</summary>
public class ButacaSala
{
    public int SalaId { get; set; }
    public Sala? Sala { get; set; }
    public string Fila { get; set; } = string.Empty;
    public int Numero { get; set; }
    public bool EsVendible { get; set; } = true;
}

/// <summary>Exhibición de una película en una sala a una fecha y hora concretas (RN-5).</summary>
public class Funcion
{
    public int Id { get; set; }
    public int PeliculaId { get; set; }
    public Pelicula? Pelicula { get; set; }
    public int SalaId { get; set; }
    public Sala? Sala { get; set; }

    /// <summary>Hora local de Costa Rica, que es la única zona del cine.</summary>
    public DateTime InicioLocal { get; set; }

    /// <summary>Total de butacas de la sala menos las no vendibles, congelado al crear la función (RN-3).</summary>
    public int AforoVendible { get; set; }

    public EstadoFuncion Estado { get; set; } = EstadoFuncion.Activa;

    public List<ButacaNoVendibleFuncion> ButacasNoVendibles { get; set; } = [];
}

/// <summary>Copia de las butacas no vendibles al momento de crear la función (RN-2, RN-4).</summary>
public class ButacaNoVendibleFuncion
{
    public int FuncionId { get; set; }
    public Funcion? Funcion { get; set; }
    public string Fila { get; set; } = string.Empty;
    public int Numero { get; set; }
}

/// <summary>
/// Butaca ocupada de una función. Butaca libre significa que no hay fila.
/// La restricción UNIQUE (FuncionId, Fila, Numero) es lo que impide venderla dos veces.
/// </summary>
public class OcupacionButaca
{
    public int Id { get; set; }
    public int FuncionId { get; set; }
    public Funcion? Funcion { get; set; }
    public string Fila { get; set; } = string.Empty;
    public int Numero { get; set; }

    /// <summary>Apartada o Vendida. Los otros dos estados no se guardan.</summary>
    public EstadoButaca Estado { get; set; }

    /// <summary>Apartado dueño de la ocupación mientras no esté vendida. Lo usa la pieza 2.</summary>
    public int? ApartadoId { get; set; }

    /// <summary>Boleto que la vendió. Lo usa la pieza 2.</summary>
    public int? BoletoId { get; set; }
}
