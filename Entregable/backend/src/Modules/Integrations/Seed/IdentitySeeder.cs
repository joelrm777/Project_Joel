using MileageClaims.Infrastructure;
using MileageClaims.Modules.Integrations.Domain;
using MileageClaims.Modules.Integrations.Services;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Integrations.Seed;

/// <summary>
/// Colaboradores, jefatura, administrador y finanzas sintéticos, para poder probar el
/// recorrido de punta a punta. Ningún dato real de la compañía ni de sus empleados.
/// Contraseña de demo para todas las cuentas: "Demo123!".
/// </summary>
public static class IdentitySeeder
{
    private const string DemoPassword = "Demo123!";

    public static async Task SeedIfEmpty(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Set<DirectoryAccount>().AnyAsync(ct)) return;

        var employee = new FakeEmployeeRecord
        {
            NationalId = "111111111",
            Name = "Ana Rodríguez",
            Email = "ana.rodriguez@automercado.test",
            ApproverNationalId = "222222222",
            ApproverEmail = "carlos.jimenez@automercado.test",
            Department = "Ventas",
            JobTitle = "Ejecutiva de Ventas",
            IsActive = true
        };
        var approverEmployee = new FakeEmployeeRecord
        {
            NationalId = "222222222",
            Name = "Carlos Jiménez",
            Email = "carlos.jimenez@automercado.test",
            ApproverNationalId = "",
            ApproverEmail = "",
            Department = "Ventas",
            JobTitle = "Jefe de Tienda",
            IsActive = true
        };
        db.Set<FakeEmployeeRecord>().AddRange(employee, approverEmployee);

        DirectoryAccount MakeAccount(string id, string email, UserRole role, string? linkedNationalId)
        {
            var (hash, salt) = PasswordHasher.Hash(DemoPassword);
            return new DirectoryAccount
            {
                Id = id,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                LinkedEmployeeNationalId = linkedNationalId
            };
        }

        db.Set<DirectoryAccount>().AddRange(
            MakeAccount("employee-ana", employee.Email, UserRole.Employee, employee.NationalId),
            MakeAccount("approver-carlos", approverEmployee.Email, UserRole.Approver, approverEmployee.NationalId),
            MakeAccount("admin-1", "admin@automercado.test", UserRole.Administrator, null),
            MakeAccount("finance-1", "finanzas@automercado.test", UserRole.Finance, null));

        await db.SaveChangesAsync(ct);
    }
}
