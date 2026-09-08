using MileageClaims.Infrastructure;
using MileageClaims.Modules.Integrations.Abstractions;
using MileageClaims.Modules.Integrations.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Integrations.Services;

public sealed class FakeDirectorioCorporativo : IDirectorioCorporativo
{
    private readonly AppDbContext _db;

    public FakeDirectorioCorporativo(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthResult?> ValidarCredenciales(string email, string password, CancellationToken ct = default)
    {
        var account = await _db.Set<DirectoryAccount>().FirstOrDefaultAsync(a => a.Email == email, ct);
        if (account is null) return null;
        if (!PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt)) return null;

        return new AuthResult(account.Id, account.Email, account.Role, account.LinkedEmployeeNationalId);
    }

    public async Task<string?> ObtenerCorreoPorId(string userId, CancellationToken ct = default) =>
        await _db.Set<DirectoryAccount>().Where(a => a.Id == userId).Select(a => a.Email).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> ObtenerCorreosPorRol(UserRole role, CancellationToken ct = default) =>
        await _db.Set<DirectoryAccount>().Where(a => a.Role == role).Select(a => a.Email).ToListAsync(ct);
}
