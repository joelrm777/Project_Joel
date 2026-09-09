using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Modules.Claims.Dtos;

// Sin EmployeeNationalId a propósito: el servidor siempre usa la cédula de la sesión ya
// autenticada (RF-1) — nunca una que mande el cliente en el cuerpo de la petición.
public sealed record CreateMileageClaimRequest(
    VehicleType VehicleType,
    FuelType FuelType,
    string PlateNumber,
    Domain.DriveType DriveType,
    int ModelYear,
    int EngineDisplacement);

public sealed record UpdateVehicleRequest(
    VehicleType VehicleType,
    FuelType FuelType,
    string PlateNumber,
    Domain.DriveType DriveType,
    int ModelYear,
    int EngineDisplacement);

public sealed record AddTripRequest(DateOnly Date, IReadOnlyList<int> StoreIdsInOrder);
