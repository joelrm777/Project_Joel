using Cine.Nucleo.Dominio;

namespace Cine.Nucleo.Contratos;

/// <summary>
/// Motivo por el que el núcleo rechaza una operación de negocio. Los casos esperables se
/// devuelven como resultado con motivo, no como excepción (DISENO.md, componente 1).
/// </summary>
public enum MotivoRechazo
{
    ButacaTomada,
    ApartadoVencido,
    FuncionCerrada,
    FuncionCancelada,
    ButacaNoVendible,
    LimiteButacas,
    TarifaNoDisponible,
    EdadNoDeclarada,
    CompraYaIngresada,
    FuncionNoEncontrada,
    ApartadoNoEncontrado,
    ButacaNoApartada,
    SinButacas
}

/// <summary>Una butaca con la tarifa que el comprador eligió para ella (RN-15).</summary>
public record LineaTarifa(string Fila, int Numero, Tarifa Tarifa);

/// <summary>Una tarifa aplicable a una función, con lo que cuesta cada butaca con ella.</summary>
public record OpcionTarifa(Tarifa Tarifa, decimal Monto);

public record ResultadoOperacion(bool Exitoso, MotivoRechazo? Motivo = null)
{
    public static ResultadoOperacion Aceptado() => new(true);
    public static ResultadoOperacion Rechazado(MotivoRechazo motivo) => new(false, motivo);
}

/// <summary>
/// El resultado de apartar. Cuando el rechazo es por una butaca que ya no está disponible,
/// vuelve con el mapa actualizado para que la pantalla lo redibuje en el momento (RF-12, R-5).
/// </summary>
public record ResultadoApartado(
    bool Aceptado,
    int? ApartadoId = null,
    DateTime? VenceEn = null,
    MotivoRechazo? Motivo = null,
    MapaFuncion? Mapa = null);

public record ResultadoCompra(
    bool Exitoso,
    int? CompraId = null,
    string? Codigo = null,
    decimal Total = 0m,
    MotivoRechazo? Motivo = null);
