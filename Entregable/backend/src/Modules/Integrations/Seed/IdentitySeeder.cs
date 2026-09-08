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
        // Colaborador inactivo: demuestra RN-17 (cédula que el ERP no reconoce porque la
        // persona ya no trabaja en la compañía). FakeErpRH.BuscarPorCedula filtra por
        // IsActive, así que para el ERP esto es indistinguible de "no existe".
        var inactiveEmployee = new FakeEmployeeRecord
        {
            NationalId = "333333333",
            Name = "Ex Colaborador",
            Email = "ex.colaborador@automercado.test",
            ApproverNationalId = "222222222",
            ApproverEmail = "carlos.jimenez@automercado.test",
            Department = "Ventas",
            JobTitle = "Ejecutivo de Ventas",
            IsActive = false
        };
        db.Set<FakeEmployeeRecord>().AddRange(employee, approverEmployee, inactiveEmployee);

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
            MakeAccount("finance-1", "finanzas@automercado.test", UserRole.Finance, null),
            // Su cuenta de AD sigue activa (todavía puede loguearse), pero el ERP ya no la
            // reconoce — es justo el desfase que RN-17 tiene que atajar.
            MakeAccount("employee-inactive", inactiveEmployee.Email, UserRole.Employee, inactiveEmployee.NationalId));

        await db.SaveChangesAsync(ct);
    }
}
