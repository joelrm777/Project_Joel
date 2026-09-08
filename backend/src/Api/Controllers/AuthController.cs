using MileageClaims.Api.Auth;
using MileageClaims.Modules.Integrations.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, string Email, string Role, string? NationalId);

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IDirectorioCorporativo _directory;
    private readonly JwtTokenService _tokenService;

    public AuthController(IDirectorioCorporativo directory, JwtTokenService tokenService)
    {
        _directory = directory;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _directory.ValidarCredenciales(request.Email, request.Password, ct);
        if (result is null)
        {
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });
        }

        var token = _tokenService.CreateToken(result);
        return Ok(new LoginResponse(token, result.Email, result.Role.ToString(), result.LinkedEmployeeNationalId));
    }
}
