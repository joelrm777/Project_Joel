using MileageClaims.Infrastructure;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Domain;
using MileageClaims.Modules.Claims.Dtos;
using MileageClaims.Modules.Distances.Abstractions;
using MileageClaims.Modules.Integrations.Abstractions;
using MileageClaims.Modules.Notifications.Abstractions;
using MileageClaims.Modules.Notifications.Domain;
using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Modules.Reporting.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Claims.Services;

public sealed class MileageClaimService : IMileageClaimService, IMileageClaimStatusUpdater
{
    private readonly AppDbContext _db;
    private readonly IErpRH _erpRH;
    private readonly IRateCalculator _rateCalculator;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly INotificationSender _notifications;
    private readonly IMileageClaimSummaryStore _summaryStore;
    private readonly IDirectorioCorporativo _directory;
    private readonly TimeProvider _clock;

    public MileageClaimService(
        AppDbContext db,
        IErpRH erpRH,
        IRateCalculator rateCalculator,
        IDistanceCalculator distanceCalculator,
        INotificationSender notifications,
        IMileageClaimSummaryStore summaryStore,
        IDirectorioCorporativo directory,
        TimeProvider clock)
    {
        _db = db;
        _erpRH = erpRH;
        _rateCalculator = rateCalculator;
        _distanceCalculator = distanceCalculator;
        _notifications = notifications;
        _summaryStore = summaryStore;
        _directory = directory;
        _clock = clock;
    }

    private DateOnly Today => DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

    public async Task<MileageClaimDto> Create(string employeeNationalId, CreateMileageClaimRequest request, CancellationToken ct = default)
    {
        var employee = await _erpRH.BuscarPorCedula(employeeNationalId, ct)
                       ?? throw new EmployeeNotFoundException(employeeNationalId);

        var claim = new MileageClaim
        {
            Id = Guid.NewGuid(),
            EmployeeNationalId = employee.NationalId,
            EmployeeName = employee.Name,
            EmployeeEmail = employee.Email,
            ApproverNationalId = employee.ApproverNationalId,
            ApproverEmail = employee.ApproverEmail,
            VehicleType = request.VehicleType,
            FuelType = request.FuelType,
            PlateNumber = request.PlateNumber,
            DriveType = request.DriveType,
            ModelYear = request.ModelYear,
            EngineDisplacement = request.EngineDisplacement,
            Status = MileageClaimStatus.Draft,
            CreatedAt = _clock.GetUtcNow()
        };

        _db.Set<MileageClaim>().Add(claim);
        await _db.SaveChangesAsync(ct);
        return ToDto(claim);
    }

    public async Task<MileageClaimDto> UpdateVehicle(Guid claimId, string requestingEmployeeNationalId, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var claim = await LoadOwned(claimId, requestingEmployeeNationalId, ct);
        EnsureEditable(claim);

        claim.VehicleType = request.VehicleType;
        claim.FuelType = request.FuelType;
        claim.PlateNumber = request.PlateNumber;
        claim.DriveType = request.DriveType;
        claim.ModelYear = request.ModelYear;
        claim.EngineDisplacement = request.EngineDisplacement;

        // El vehículo es único por boleta (RN-4): cambiarlo recalcula la tarifa de todos los viajes ya agregados.
        var vehicle = new VehicleDeclaration(claim.VehicleType, claim.FuelType, claim.EngineDisplacement, claim.ModelYear);
        foreach (var trip in claim.Trips)
        {
            var quote = await _rateCalculator.CalculateRate(vehicle, Today, ct);
            trip.AppliedRatePerKm = quote.RatePerKm;
            trip.RateSummary = quote.RateSummary;
            trip.Recalculate();
        }
        claim.RecalculateTotal();

        await _db.SaveChangesAsync(ct);
        return ToDto(claim);
    }

    public async Task<MileageClaimDto> AddTrip(Guid claimId, string requestingEmployeeNationalId, AddTripRequest request, CancellationToken ct = default)
    {
        var claim = await LoadOwned(claimId, requestingEmployeeNationalId, ct);
        EnsureEditable(claim);

        ValidateDateWindow(request.Date);
        await EnsureNotDuplicate(claim.EmployeeNationalId, request.Date, request.StoreIdsInOrder, ct);

        var vehicle = new VehicleDeclaration(claim.VehicleType, claim.FuelType, claim.EngineDisplacement, claim.ModelYear);
        var quote = await _rateCalculator.CalculateRate(vehicle, Today, ct);
        var distance = await _distanceCalculator.CalculateTotalDistance(request.StoreIdsInOrder, ct);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            MileageClaimId = claim.Id,
            Date = request.Date,
            AppliedRatePerKm = quote.RatePerKm,
            RateSummary = quote.RateSummary,
            Legs = distance.Legs.Select(l => new Leg
            {
                Id = Guid.NewGuid(),
                SequenceNumber = l.SequenceNumber,
                OriginStoreId = l.OriginStoreId,
                DestinationStoreId = l.DestinationStoreId,
                AppliedDistanceKm = l.DistanceKm
            }).ToList()
        };
        trip.Recalculate();

        claim.Trips.Add(trip);
        claim.RecalculateTotal();

        await _db.SaveChangesAsync(ct);
        return ToDto(claim);
    }

    public async Task<MileageClaimDto> RemoveTrip(Guid claimId, string requestingEmployeeNationalId, Guid tripId, CancellationToken ct = default)
    {
        var claim = await LoadOwned(claimId, requestingEmployeeNationalId, ct);
        EnsureEditable(claim);

        var trip = claim.Trips.FirstOrDefault(t => t.Id == tripId);
        if (trip is not null)
        {
            claim.Trips.Remove(trip);
            _db.Set<Trip>().Remove(trip);
            claim.RecalculateTotal();
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(claim);
    }

    public async Task<MileageClaimDto> Submit(Guid claimId, string requestingEmployeeNationalId, CancellationToken ct = default)
    {
        var claim = await LoadOwned(claimId, requestingEmployeeNationalId, ct);
        EnsureEditable(claim);

        if (claim.Trips.Count == 0)
        {
            throw new EmptyClaimException();
        }

        // Reintenta la distancia de los viajes incompletos por si el administrador ya cargó
        // el tramo que faltaba (RN-5) — la tarifa no se vuelve a tocar (RN-16).
        var missing = new List<(int, int)>();
        foreach (var trip in claim.Trips.Where(t => !t.IsDistanceComplete))
        {
            var storeIds = BuildStoreSequence(trip);
            var distance = await _distanceCalculator.CalculateTotalDistance(storeIds, ct);

            trip.Legs.Clear();
            foreach (var leg in distance.Legs)
            {
                trip.Legs.Add(new Leg
                {
                    Id = Guid.NewGuid(),
                    SequenceNumber = leg.SequenceNumber,
                    OriginStoreId = leg.OriginStoreId,
                    DestinationStoreId = leg.DestinationStoreId,
                    AppliedDistanceKm = leg.DistanceKm
                });
            }
            trip.Recalculate();

            if (!trip.IsDistanceComplete)
            {
                missing.AddRange(distance.MissingLegs.Select(l => (l.OriginStoreId, l.DestinationStoreId)));
            }
        }

        if (missing.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            await NotifyMissingDistance(claim, missing, ct);
            throw new MissingDistanceException(missing);
        }

        claim.RecalculateTotal();
        claim.Status = MileageClaimStatus.Pending;
        claim.SubmittedAt = _clock.GetUtcNow();
        claim.DecidedAt = null;
        claim.RejectionReason = null;
        // SubmittedAt se acaba de reiniciar (primer envío o reenvío tras rechazo, RN-12) —
        // el conteo de recordatorios (RN-14) arranca de cero contra la nueva fecha.
        claim.RemindersSent = 0;
        claim.LastReminderAt = null;

        await _db.SaveChangesAsync(ct);

        await _notifications.Send(
            NotificationType.SubmittedForApproval,
            claim.ApproverEmail,
            claim.Id,
            $"Boleta de {claim.EmployeeName} pendiente de tu aprobación. Monto: {claim.TotalAmount:C}.",
            ct);

        return ToDto(claim);
    }

    public async Task Withdraw(Guid claimId, string requestingEmployeeNationalId, CancellationToken ct = default)
    {
        var claim = await LoadOwned(claimId, requestingEmployeeNationalId, ct);
        EnsureEditable(claim);

        _db.Set<MileageClaim>().Remove(claim);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MileageClaimDto> Get(Guid claimId, CancellationToken ct = default)
    {
        var claim = await LoadWithTrips(claimId, ct) ?? throw new MileageClaimNotFoundException(claimId);
        return ToDto(claim);
    }

    public async Task<IReadOnlyList<MileageClaimDto>> GetForEmployee(string employeeNationalId, CancellationToken ct = default)
    {
        var claims = await _db.Set<MileageClaim>()
            .Include(c => c.Trips).ThenInclude(t => t.Legs)
            .Where(c => c.EmployeeNationalId == employeeNationalId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
        return claims.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MileageClaimDto>> GetForApprover(string approverEmail, CancellationToken ct = default)
    {
        var claims = await _db.Set<MileageClaim>()
            .Include(c => c.Trips).ThenInclude(t => t.Legs)
            .Where(c => c.ApproverEmail == approverEmail && c.Status == MileageClaimStatus.Pending)
            .OrderBy(c => c.SubmittedAt)
            .ToListAsync(ct);
        return claims.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MileageClaimDto>> GetApprovedByApprover(string approverEmail, CancellationToken ct = default)
    {
        var claims = await _db.Set<MileageClaim>()
            .Include(c => c.Trips).ThenInclude(t => t.Legs)
            .Where(c => c.ApproverEmail == approverEmail && c.Status == MileageClaimStatus.Approved)
            .OrderByDescending(c => c.DecidedAt)
            .ToListAsync(ct);
        return claims.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MileageClaimDto>> GetApproved(CancellationToken ct = default)
    {
        var claims = await _db.Set<MileageClaim>()
            .Include(c => c.Trips).ThenInclude(t => t.Legs)
            .Where(c => c.Status == MileageClaimStatus.Approved)
            .OrderByDescending(c => c.FinanceReceivedAt)
            .ToListAsync(ct);
        return claims.Select(ToDto).ToList();
    }

    // ---- IMileageClaimStatusUpdater: consumido por Aprobación y el Temporizador ----

    public async Task MarkApproved(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.Set<MileageClaim>().FirstOrDefaultAsync(c => c.Id == claimId, ct)
                   ?? throw new MileageClaimNotFoundException(claimId);

        if (claim.Status != MileageClaimStatus.Pending) return; // RN-15: ya decidida, no hay nada que rehacer.

        claim.Status = MileageClaimStatus.Approved;
        claim.DecidedAt = _clock.GetUtcNow();
        claim.FinanceReceivedAt = claim.DecidedAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkRejected(Guid claimId, string reason, CancellationToken ct = default)
    {
        var claim = await _db.Set<MileageClaim>().FirstOrDefaultAsync(c => c.Id == claimId, ct)
                   ?? throw new MileageClaimNotFoundException(claimId);

        if (claim.Status != MileageClaimStatus.Pending) return; // RN-15.

        claim.Status = MileageClaimStatus.Rejected;
        claim.DecidedAt = _clock.GetUtcNow();
        claim.RejectionReason = reason;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PendingReminderCandidate>> FindPendingForReminderCheck(CancellationToken ct = default) =>
        await _db.Set<MileageClaim>()
            .Where(c => c.Status == MileageClaimStatus.Pending && c.SubmittedAt != null)
            .Select(c => new PendingReminderCandidate(c.Id, c.ApproverEmail, c.SubmittedAt!.Value, c.RemindersSent, c.LastReminderAt))
            .ToListAsync(ct);

    public async Task RecordReminderSent(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.Set<MileageClaim>().FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return;
        claim.RemindersSent += 1;
        claim.LastReminderAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ProcessOverdueDiscards(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = _clock.GetUtcNow().AddDays(-retentionDays);
        var overdue = await _db.Set<MileageClaim>()
            .Where(c => c.Status == MileageClaimStatus.Rejected && c.DecidedAt != null && c.DecidedAt <= cutoff)
            .ToListAsync(ct);

        foreach (var claim in overdue)
        {
            claim.Status = MileageClaimStatus.Discarded;
        }
        await _db.SaveChangesAsync(ct);
        return overdue.Count;
    }

    public async Task<int> ProcessRetentionPurge(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = _clock.GetUtcNow().AddDays(-retentionDays);
        var expired = await _db.Set<MileageClaim>()
            .Include(c => c.Trips).ThenInclude(t => t.Legs)
            .Where(c => c.Status == MileageClaimStatus.Approved && c.FinanceReceivedAt != null && c.FinanceReceivedAt <= cutoff)
            .ToListAsync(ct);

        foreach (var claim in expired)
        {
            await _summaryStore.Save(new Reporting.Domain.MileageClaimSummary
            {
                MileageClaimId = claim.Id,
                EmployeeName = claim.EmployeeName,
                EmployeeNationalId = claim.EmployeeNationalId,
                ApproverNationalId = claim.ApproverNationalId,
                ApproverEmail = claim.ApproverEmail,
                TotalAmount = claim.TotalAmount,
                TotalDistance = claim.Trips.Sum(t => t.TotalDistance),
                AppliedRateSummary = string.Join(" | ", claim.Trips.Select(t => t.RateSummary).Distinct()),
                SubmittedAt = claim.SubmittedAt ?? claim.CreatedAt,
                DecidedAt = claim.DecidedAt,
                Status = claim.Status.ToString(),
                RecordedAt = _clock.GetUtcNow()
            }, ct);

            _db.Set<MileageClaim>().Remove(claim);
        }

        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }

    // ---- helpers ----

    /// <summary>RN-5: avisa a administrador y finanzas qué tramo falta cargar, sin que el colaborador tenga que hacerlo a mano.</summary>
    private async Task NotifyMissingDistance(MileageClaim claim, IReadOnlyList<(int OriginStoreId, int DestinationStoreId)> missing, CancellationToken ct)
    {
        var recipients = (await _directory.ObtenerCorreosPorRol(Integrations.Domain.UserRole.Administrator, ct))
            .Concat(await _directory.ObtenerCorreosPorRol(Integrations.Domain.UserRole.Finance, ct));

        var detail = string.Join(", ", missing.Select(l => $"{l.OriginStoreId}->{l.DestinationStoreId}"));
        var message = $"La boleta de {claim.EmployeeName} está bloqueada: falta la distancia entre tiendas {detail}.";

        foreach (var recipient in recipients.Distinct())
        {
            await _notifications.Send(NotificationType.MissingDistance, recipient, claim.Id, message, ct);
        }
    }

    private static List<int> BuildStoreSequence(Trip trip) =>
        trip.Legs.OrderBy(l => l.SequenceNumber).Select(l => l.OriginStoreId)
            .Append(trip.Legs.OrderBy(l => l.SequenceNumber).Last().DestinationStoreId)
            .ToList();

    private async Task<MileageClaim?> LoadWithTrips(Guid claimId, CancellationToken ct) =>
        await _db.Set<MileageClaim>().Include(c => c.Trips).ThenInclude(t => t.Legs)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);

    private async Task<MileageClaim> LoadOwned(Guid claimId, string requestingEmployeeNationalId, CancellationToken ct)
    {
        var claim = await LoadWithTrips(claimId, ct) ?? throw new MileageClaimNotFoundException(claimId);
        if (claim.EmployeeNationalId != requestingEmployeeNationalId)
        {
            throw new ForbiddenClaimAccessException(claimId);
        }
        return claim;
    }

    private static void EnsureEditable(MileageClaim claim)
    {
        if (claim.Status is not (MileageClaimStatus.Draft or MileageClaimStatus.Pending or MileageClaimStatus.Rejected))
        {
            throw new ClaimNotEditableException(claim.Id, claim.Status);
        }
    }

    private void ValidateDateWindow(DateOnly date)
    {
        var currentMonthStart = new DateOnly(Today.Year, Today.Month, 1);
        var earliestAllowed = currentMonthStart.AddMonths(-2);
        var latestAllowed = currentMonthStart.AddMonths(1).AddDays(-1);
        if (date < earliestAllowed || date > latestAllowed)
        {
            throw new TripDateOutOfWindowException(date);
        }
    }

    private async Task EnsureNotDuplicate(string employeeNationalId, DateOnly date, IReadOnlyList<int> storeIdsInOrder, CancellationToken ct)
    {
        // RN-7: un viaje idéntico no se puede repetir ni en otra boleta ni en la misma —
        // por eso no se excluye la boleta actual de esta búsqueda.
        var sameEmployeeTrips = await _db.Set<Trip>()
            .Where(t => t.Date == date && t.MileageClaim!.EmployeeNationalId == employeeNationalId)
            .Select(t => new { t.Id, t.MileageClaimId, t.Legs })
            .ToListAsync(ct);

        foreach (var candidate in sameEmployeeTrips)
        {
            var candidateStoreIds = candidate.Legs.OrderBy(l => l.SequenceNumber).Select(l => l.OriginStoreId)
                .Append(candidate.Legs.OrderBy(l => l.SequenceNumber).Last().DestinationStoreId)
                .ToList();

            if (candidateStoreIds.SequenceEqual(storeIdsInOrder))
            {
                throw new DuplicateTripException(candidate.Id, candidate.MileageClaimId);
            }
        }
    }

    private static MileageClaimDto ToDto(MileageClaim claim) => new(
        claim.Id,
        claim.EmployeeNationalId,
        claim.EmployeeName,
        claim.EmployeeEmail,
        claim.ApproverNationalId,
        claim.ApproverEmail,
        claim.VehicleType,
        claim.FuelType,
        claim.PlateNumber,
        claim.DriveType,
        claim.ModelYear,
        claim.EngineDisplacement,
        claim.Status,
        claim.CreatedAt,
        claim.SubmittedAt,
        claim.DecidedAt,
        claim.FinanceReceivedAt,
        claim.RejectionReason,
        claim.TotalAmount,
        claim.Trips.OrderBy(t => t.Date).Select(t => new TripDto(
            t.Id, t.Date, t.AppliedRatePerKm, t.RateSummary, t.TotalDistance, t.TotalAmount, t.IsDistanceComplete,
            t.Legs.OrderBy(l => l.SequenceNumber).Select(l => new LegDto(l.SequenceNumber, l.OriginStoreId, l.DestinationStoreId, l.AppliedDistanceKm)).ToList()
        )).ToList());
}
