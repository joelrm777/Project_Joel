using System.Reflection;
using System.Text;
using MileageClaims.Api.Auth;
using MileageClaims.Api.Middleware;
using MileageClaims.Infrastructure;
using MileageClaims.Modules.Approvals;
using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Distances;
using MileageClaims.Modules.Distances.Seed;
using MileageClaims.Modules.Integrations;
using MileageClaims.Modules.Integrations.Seed;
using MileageClaims.Modules.Notifications;
using MileageClaims.Modules.Rates;
using MileageClaims.Modules.Rates.Seed;
using MileageClaims.Modules.Reporting;
using MileageClaims.Modules.Scheduling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Módulos (monolito modular: cada uno registra su propio DI) ----
builder.Services.AddRatesModule();
builder.Services.AddDistancesModule();
builder.Services.AddIntegrationsModule();
builder.Services.AddNotificationsModule();
builder.Services.AddClaimsModule();
builder.Services.AddApprovalsModule();
builder.Services.AddSchedulingModule();
builder.Services.AddReportingModule();
builder.Services.AddSystemConfiguration();

// ---- Base de datos: connection string viene de User Secrets / variables de entorno ----
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:Default. Configuralo con: dotnet user-secrets set \"ConnectionStrings:Default\" \"<tu connection string de Azure SQL>\" --project src/Api");
}

var moduleAssemblies = new[]
{
    typeof(MileageClaims.Modules.Rates.Domain.RateTableEntry).Assembly,
    typeof(MileageClaims.Modules.Distances.Domain.Store).Assembly,
    typeof(MileageClaims.Modules.Integrations.Domain.DirectoryAccount).Assembly,
    typeof(MileageClaims.Modules.Notifications.Domain.NotificationLog).Assembly,
    typeof(MileageClaims.Modules.Claims.Domain.MileageClaim).Assembly,
    typeof(MileageClaims.Modules.Reporting.Domain.MileageClaimSummary).Assembly,
};
builder.Services.AddMileageClaimsDatabase(connectionString, moduleAssemblies);

// ---- Autenticación JWT contra el AD simulado (RF-16) ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException(
        "Falta Jwt:SigningKey. Configuralo con: dotnet user-secrets set \"Jwt:SigningKey\" \"<una clave larga y aleatoria>\" --project src/Api");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// ---- CORS para la SPA en desarrollo ----
const string FrontendCorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // Los enums viajan como texto ("Car", "Approved") en toda la API, no como número —
        // así lo espera la SPA (ver lib/types.ts) y así queda legible en NotificationLog.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Aplica migraciones y siembra datos sintéticos para poder probar el recorrido
    // de punta a punta sin la UI de administrador (Pieza 3).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await RateTableSeeder.SeedIfEmpty(db);
    await StoreSeeder.SeedIfEmpty(db);
    await IdentitySeeder.SeedIfEmpty(db);
}

app.UseMileageClaimsExceptionHandling();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
