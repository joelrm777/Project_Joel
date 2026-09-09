using MileageClaims.Infrastructure;
using MileageClaims.Modules.Distances.Domain;
using MileageClaims.Modules.Integrations.Domain;
using MileageClaims.Modules.Integrations.Services;
using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Tests.Infrastructure;

/// <summary>Datos sintéticos mínimos para armar cada prueba — nunca datos reales de la compañía.</summary>
public static class TestData
{
    public const string DemoPassword = "Demo123!";

    public static FakeEmployeeRecord SeedEmployee(
        AppDbContext db,
        string nationalId,
        string approverNationalId,
        string approverEmail,
        bool isActive = true)
    {
        var employee = new FakeEmployeeRecord
        {
            NationalId = nationalId,
            Name = $"Empleado {nationalId}",
            Email = $"{nationalId}@automercado.test",
            ApproverNationalId = approverNationalId,
            ApproverEmail = approverEmail,
            Department = "Ventas",
            JobTitle = "Ejecutivo",
            IsActive = isActive,
        };
        db.Set<FakeEmployeeRecord>().Add(employee);
        db.SaveChanges();
        return employee;
    }

    public static DirectoryAccount SeedAccount(AppDbContext db, string id, string email, UserRole role, string? linkedNationalId = null)
    {
        var (hash, salt) = PasswordHasher.Hash(DemoPassword);
        var account = new DirectoryAccount
        {
            Id = id,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            LinkedEmployeeNationalId = linkedNationalId,
        };
        db.Set<DirectoryAccount>().Add(account);
        db.SaveChanges();
        return account;
    }

    public static RateTableEntry SeedRate(
        AppDbContext db,
        VehicleType vehicleType,
        FuelType fuelType,
        int minCc,
        int maxCc,
        int ageYears,
        decimal ratePerKm)
    {
        var entry = new RateTableEntry
        {
            Id = Guid.NewGuid(),
            VehicleType = vehicleType,
            FuelType = fuelType,
            EngineDisplacementMin = minCc,
            EngineDisplacementMax = maxCc,
            VehicleAgeYears = ageYears,
            RatePerKm = ratePerKm,
        };
        db.Set<RateTableEntry>().Add(entry);
        db.SaveChanges();
        return entry;
    }

    public static Store SeedStore(AppDbContext db, string name)
    {
        var store = new Store { Name = name };
        db.Set<Store>().Add(store);
        db.SaveChanges();
        return store;
    }

    public static void SeedDistance(AppDbContext db, int originStoreId, int destinationStoreId, decimal km)
    {
        db.Set<StoreDistance>().Add(new StoreDistance
        {
            OriginStoreId = originStoreId,
            DestinationStoreId = destinationStoreId,
            DistanceKm = km,
        });
        db.SaveChanges();
    }

    public sealed record StandardScenario(
        string EmployeeNationalId,
        string EmployeeEmail,
        string ApproverEmail,
        string AdminEmail,
        string FinanceEmail,
        int StoreAId,
        int StoreBId,
        int StoreCId);

    /// <summary>
    /// Un colaborador con su jefatura, una tarifa simple (₡100/km, Car/Gasoline/0 años,
    /// 1000-3000cc) y tres tiendas en línea (A-B 10km, B-C 15km). Suficiente para armar
    /// boletas de uno o dos viajes en la mayoría de las pruebas de integración.
    /// </summary>
    /// <param name="seed">
    /// Sufijo para que cédulas/correos/ids no choquen cuando varias pruebas siembran contra
    /// la misma base (ej. las de Api, que comparten una ApiFactoryFixture por clase).
    /// </param>
    public static StandardScenario SeedStandardScenario(AppDbContext db, string seed = "")
    {
        var approverNationalId = $"222222222{seed}";
        var approverEmail = $"{approverNationalId}@automercado.test";
        var employee = SeedEmployee(db, $"111111111{seed}", approverNationalId, approverEmail);

        SeedRate(db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 0, ratePerKm: 100m);

        var storeA = SeedStore(db, $"A{seed}");
        var storeB = SeedStore(db, $"B{seed}");
        var storeC = SeedStore(db, $"C{seed}");
        SeedDistance(db, storeA.Id, storeB.Id, 10m);
        SeedDistance(db, storeB.Id, storeC.Id, 15m);

        var adminEmail = $"admin{seed}@automercado.test";
        var financeEmail = $"finanzas{seed}@automercado.test";
        SeedAccount(db, $"admin-1{seed}", adminEmail, UserRole.Administrator);
        SeedAccount(db, $"finance-1{seed}", financeEmail, UserRole.Finance);

        return new StandardScenario(employee.NationalId, employee.Email, approverEmail, adminEmail, financeEmail, storeA.Id, storeB.Id, storeC.Id);
    }
}
