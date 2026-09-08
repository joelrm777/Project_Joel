using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Modules.Claims.Dtos;

public sealed record CreateMileageClaimRequest(
    string EmployeeNationalId,
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
