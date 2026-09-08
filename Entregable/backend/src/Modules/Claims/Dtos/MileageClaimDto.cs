using MileageClaims.Modules.Rates.Domain;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;
using MileageClaimStatus = MileageClaims.Modules.Claims.Domain.MileageClaimStatus;

namespace MileageClaims.Modules.Claims.Dtos;

public sealed record LegDto(int SequenceNumber, int OriginStoreId, int DestinationStoreId, decimal? AppliedDistanceKm);

public sealed record TripDto(
    Guid Id,
    DateOnly Date,
    decimal AppliedRatePerKm,
    string RateSummary,
    decimal TotalDistance,
    decimal TotalAmount,
    bool IsDistanceComplete,
    IReadOnlyList<LegDto> Legs);

public sealed record MileageClaimDto(
    Guid Id,
    string EmployeeNationalId,
    string EmployeeName,
    string EmployeeEmail,
    string ApproverNationalId,
    string ApproverEmail,
    VehicleType VehicleType,
    FuelType FuelType,
    string PlateNumber,
    DriveType DriveType,
    int ModelYear,
    int EngineDisplacement,
    MileageClaimStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? DecidedAt,
    DateTimeOffset? FinanceReceivedAt,
    string? RejectionReason,
    decimal TotalAmount,
    IReadOnlyList<TripDto> Trips);
