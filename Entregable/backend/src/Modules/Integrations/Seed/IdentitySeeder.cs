using MileageClaims.Infrastructure;
using MileageClaims.Modules.Integrations.Domain;
using MileageClaims.Modules.Integrations.Services;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Integrations.Seed;

/// <summary>
/// Colaboradores, jefaturas, administrador y finanzas sintéticos, para poder probar el
/// recorrido de punta a punta. Ningún dato real de la compañía ni de sus empleados.
/// Contraseña de demo para todas las cuentas: "Demo123!".
/// </summary>
public static class IdentitySeeder
{
    private const string DemoPassword = "Demo123!";

    public static async Task SeedIfEmpty(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Set<DirectoryAccount>().AnyAsync(ct)) return;

        // Jefatura de las jefaturas: aprueba las boletas de Carlos y del administrador,
        // que también son colaboradores además de su rol principal.
        var seniorApprover = new FakeEmployeeRecord
        {
            NationalId = "888888888",
            Name = "Roberto Solano",
            Email = "roberto.solano@retail.test",
            ApproverNationalId = "",
            ApproverEmail = "",
            Department = "Dirección",
            JobTitle = "Gerente Regional",
            IsActive = true
        };

        var employee = new FakeEmployeeRecord
        {
            NationalId = "111111111",
            Name = "Ana Rodríguez",
            Email = "ana.rodriguez@retail.test",
            ApproverNationalId = "222222222",
            ApproverEmail = "carlos.jimenez@retail.test",
            Department = "Ventas",
            JobTitle = "Ejecutiva de Ventas",
            IsActive = true
        };
        // Carlos aprueba boletas ajenas (jefatura) pero también cobra kilometraje propio —
        // sus boletas las aprueba Roberto.
        var approverEmployee = new FakeEmployeeRecord
        {
            NationalId = "222222222",
            Name = "Carlos Jiménez",
            Email = "carlos.jimenez@retail.test",
            ApproverNationalId = seniorApprover.NationalId,
            ApproverEmail = seniorApprover.Email,
            Department = "Ventas",
            JobTitle = "Jefe de Tienda",
            IsActive = true
        };
        // El administrador también cobra kilometraje propio — sus boletas también las
        // aprueba Roberto.
        var adminEmployee = new FakeEmployeeRecord
        {
            NationalId = "999999999",
            Name = "Marta Vindas",
            Email = "admin@retail.test",
            ApproverNationalId = seniorApprover.NationalId,
            ApproverEmail = seniorApprover.Email,
            Department = "Administración",
            JobTitle = "Administradora del sistema",
            IsActive = true
        };
        // Finanzas también cobra kilometraje propio — sus boletas también las aprueba Roberto.
        var financeEmployee = new FakeEmployeeRecord
        {
            NationalId = "444444440",
            Name = "Laura Méndez",
            Email = "finanzas@retail.test",
            ApproverNationalId = seniorApprover.NationalId,
            ApproverEmail = seniorApprover.Email,
            Department = "Finanzas",
            JobTitle = "Analista de Finanzas",
            IsActive = true
        };
        // Colaborador inactivo: demuestra RN-17 (cédula que el ERP no reconoce porque la
        // persona ya no trabaja en la compañía). FakeErpRH.BuscarPorCedula filtra por
        // IsActive, así que para el ERP esto es indistinguible de "no existe".
        var inactiveEmployee = new FakeEmployeeRecord
        {
            NationalId = "333333333",
            Name = "Ex Colaborador",
            Email = "ex.colaborador@retail.test",
            ApproverNationalId = "222222222",
            ApproverEmail = "carlos.jimenez@retail.test",
            Department = "Ventas",
            JobTitle = "Ejecutivo de Ventas",
            IsActive = false
        };
        db.Set<FakeEmployeeRecord>().AddRange(seniorApprover, employee, approverEmployee, adminEmployee, financeEmployee, inactiveEmployee);

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
            // Aprobador: también colaborador (LinkedEmployeeNationalId propio, no null) —
            // puede armar y cobrar sus propias boletas además de aprobar las de Ana.
            MakeAccount("approver-carlos", approverEmployee.Email, UserRole.Approver, approverEmployee.NationalId),
            // Administrador: también colaborador, mismo motivo.
            MakeAccount("admin-1", adminEmployee.Email, UserRole.Administrator, adminEmployee.NationalId),
            MakeAccount("approver-roberto", seniorApprover.Email, UserRole.Approver, seniorApprover.NationalId),
            // Finanzas: también colaborador, mismo motivo que jefatura y administrador.
            MakeAccount("finance-1", financeEmployee.Email, UserRole.Finance, financeEmployee.NationalId),
            // Su cuenta de AD sigue activa (todavía puede loguearse), pero el ERP ya no la
            // reconoce — es justo el desfase que RN-17 tiene que atajar.
            MakeAccount("employee-inactive", inactiveEmployee.Email, UserRole.Employee, inactiveEmployee.NationalId));

        await db.SaveChangesAsync(ct);
    }
}
