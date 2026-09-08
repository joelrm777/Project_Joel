using MileageClaims.Modules.Integrations.Domain;

namespace MileageClaims.Modules.Integrations.Abstractions;

/// <summary>
/// Única puerta hacia el ERP de RH. Hoy la implementación es un fake sembrado; el día que
/// se conecte el ERP real, se reemplaza la implementación sin tocar a nadie más (RN-17, RF-1).
/// </summary>
public interface IErpRH
{
    /// <summary>Null si la cédula no existe o corresponde a alguien inactivo (RN-17).</summary>
    Task<EmployeeInfo?> BuscarPorCedula(string nationalId, CancellationToken ct = default);
}
