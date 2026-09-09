namespace MileageClaims.Modules.Claims;

/// <summary>RN-17: la cédula no existe en el ERP, o corresponde a alguien inactivo.</summary>
public sealed class EmployeeNotFoundException(string nationalId)
    : Exception($"No se encontró un colaborador activo con cédula {nationalId}.");

public sealed class MileageClaimNotFoundException(Guid id) : Exception($"La boleta {id} no existe.");

/// <summary>RN-6: el viaje cae fuera de la ventana mes en curso + 2 meses anteriores.</summary>
public sealed class TripDateOutOfWindowException(DateOnly date)
    : Exception($"La fecha {date:yyyy-MM-dd} está fuera de la ventana permitida (mes en curso y los dos meses anteriores).");

/// <summary>RN-7: ya existe un viaje idéntico (misma fecha + misma secuencia de tiendas) del mismo colaborador.</summary>
public sealed class DuplicateTripException(Guid existingTripId, Guid existingClaimId)
    : Exception($"Ya existe un viaje idéntico en la boleta {existingClaimId} (viaje {existingTripId}).")
{
    public Guid ExistingTripId { get; } = existingTripId;
    public Guid ExistingClaimId { get; } = existingClaimId;
}

/// <summary>RN-8: solo se puede editar/retirar mientras Draft o Pending.</summary>
public sealed class ClaimNotEditableException(Guid claimId, Domain.MileageClaimStatus status)
    : Exception($"La boleta {claimId} está en estado {status} y no se puede editar.");

/// <summary>Un colaborador no puede tocar la boleta de otro.</summary>
public sealed class ForbiddenClaimAccessException(Guid claimId)
    : Exception($"No tenés acceso a la boleta {claimId}.");

/// <summary>RN-5: falta la distancia de uno o más tramos.</summary>
public sealed class MissingDistanceException(IReadOnlyList<(int OriginStoreId, int DestinationStoreId)> missingLegs)
    : Exception("Faltan tramos con distancia registrada: " +
                string.Join(", ", missingLegs.Select(l => $"{l.OriginStoreId}->{l.DestinationStoreId}")))
{
    public IReadOnlyList<(int OriginStoreId, int DestinationStoreId)> MissingLegs { get; } = missingLegs;
}

public sealed class EmptyClaimException() : Exception("La boleta no tiene ningún viaje.");
