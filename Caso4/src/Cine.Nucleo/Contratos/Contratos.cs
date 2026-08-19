using Cine.Nucleo.Dominio;

namespace Cine.Nucleo.Contratos;

/// <summary>Una función tal como se ve en la cartelera de la semana (RF-6).</summary>
public record FuncionEnCartelera(
    int FuncionId,
    string Titulo,
    int ClasificacionEdad,
    int DuracionMinutos,
    string SalaNombre,
    DateTime InicioLocal,
    int AforoVendible);

/// <summary>El estado de una butaca dentro del mapa de una función (RF-9).</summary>
public record ButacaEstado(string Fila, int Numero, EstadoButaca Estado);

/// <summary>El mapa de butacas de una función, con el estado de cada una.</summary>
public record MapaFuncion(
    int FuncionId,
    string Titulo,
    string SalaNombre,
    DateTime InicioLocal,
    int ClasificacionEdad,
    EstadoFuncion Estado,
    IReadOnlyList<ButacaEstado> Butacas);
