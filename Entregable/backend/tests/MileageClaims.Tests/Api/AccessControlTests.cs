using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MileageClaims.Infrastructure;
using MileageClaims.Modules.Integrations.Domain;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Tests.Infrastructure;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;

namespace MileageClaims.Tests.Api;

/// <summary>
/// Una sola ApiFactoryFixture para toda la clase (WebApplicationFactory no se lleva bien con
/// instanciarse más de una vez por tipo de entry point). Para no depender de haber corrido
/// otra prueba antes, cada método siembra con un sufijo propio — nunca lee ni asume datos de
/// otro método, aunque compartan la misma base SQLite en memoria.
/// </summary>
public sealed class AccessControlTests : IClassFixture<ApiFactoryFixture>
{
    private readonly ApiFactoryFixture _factory;

    public AccessControlTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    private sealed record LoginResponse(string Token, string Email, string Role, string? NationalId);
    private sealed record MileageClaimJson(Guid Id);

    private async Task<string> LoginAsync(HttpClient client, string email, string password = TestData.DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static HttpClient AuthorizedClient(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static object VehiclePayload() => new
    {
        vehicleType = VehicleType.Car.ToString(),
        fuelType = FuelType.Gasoline.ToString(),
        plateNumber = "ABC123",
        driveType = DriveType.Single.ToString(),
        modelYear = 2026,
        engineDisplacement = 1500,
    };

    /// <summary>RF-16: que una contraseña incorrecta autenticara igual, o que el rol viniera mal, lo haría fallar.</summary>
    [Fact]
    public async Task Login_valido_da_token_con_el_rol_correcto_e_invalido_da_401()
    {
        using var scope = _factory.CreateSeedScope();
        TestData.SeedAccount(scope.ServiceProvider.GetRequiredService<AppDbContext>(), "login-employee-t1", "login.t1@automercado.test", UserRole.Employee);

        var client = _factory.CreateClient();

        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email = "login.t1@automercado.test", password = TestData.DemoPassword });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Employee", body!.Role);

        var bad = await client.PostAsJsonAsync("/api/auth/login", new { email = "login.t1@automercado.test", password = "contraseña-incorrecta" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    /// <summary>Que un colaborador pudiera leer o editar la boleta de otro lo haría fallar.</summary>
    [Fact]
    public async Task Un_colaborador_no_puede_ver_ni_tocar_la_boleta_de_otro()
    {
        using var scope = _factory.CreateSeedScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scenario = TestData.SeedStandardScenario(db, seed: "t2");
        TestData.SeedAccount(db, "employee-owner-t2", scenario.EmployeeEmail, UserRole.Employee, scenario.EmployeeNationalId);
        var otherEmployee = TestData.SeedEmployee(db, "777777772", "222222222t2", scenario.ApproverEmail);
        TestData.SeedAccount(db, "employee-other-t2", otherEmployee.Email, UserRole.Employee, otherEmployee.NationalId);

        var ownerClient = _factory.CreateClient();
        AuthorizedClient(ownerClient, await LoginAsync(ownerClient, scenario.EmployeeEmail));

        var createResponse = await ownerClient.PostAsJsonAsync("/api/mileage-claims", VehiclePayload());
        createResponse.EnsureSuccessStatusCode();
        var claim = await createResponse.Content.ReadFromJsonAsync<MileageClaimJson>();

        var otherClient = _factory.CreateClient();
        AuthorizedClient(otherClient, await LoginAsync(otherClient, otherEmployee.Email));

        var getResponse = await otherClient.GetAsync($"/api/mileage-claims/{claim!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);

        var deleteResponse = await otherClient.DeleteAsync($"/api/mileage-claims/{claim.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    /// <summary>Que una jefatura pudiera decidir sobre una boleta que no le corresponde lo haría fallar.</summary>
    [Fact]
    public async Task Una_jefatura_no_puede_aprobar_una_boleta_que_no_es_suya()
    {
        using var scope = _factory.CreateSeedScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scenario = TestData.SeedStandardScenario(db, seed: "t3");
        TestData.SeedAccount(db, "employee-owner-t3", scenario.EmployeeEmail, UserRole.Employee, scenario.EmployeeNationalId);
        TestData.SeedAccount(db, "approver-real-t3", scenario.ApproverEmail, UserRole.Approver, "222222222t3");
        TestData.SeedAccount(db, "approver-impostor-t3", "impostor.t3@automercado.test", UserRole.Approver, null);

        var employeeClient = _factory.CreateClient();
        AuthorizedClient(employeeClient, await LoginAsync(employeeClient, scenario.EmployeeEmail));

        var createResponse = await employeeClient.PostAsJsonAsync("/api/mileage-claims", VehiclePayload());
        var claim = await createResponse.Content.ReadFromJsonAsync<MileageClaimJson>();
        await employeeClient.PostAsJsonAsync($"/api/mileage-claims/{claim!.Id}/trips", new
        {
            date = "2026-06-01",
            storeIdsInOrder = new[] { scenario.StoreAId, scenario.StoreBId },
        });
        await employeeClient.PostAsync($"/api/mileage-claims/{claim.Id}/submit", null);

        var impostorClient = _factory.CreateClient();
        AuthorizedClient(impostorClient, await LoginAsync(impostorClient, "impostor.t3@automercado.test"));

        var approveResponse = await impostorClient.PostAsync($"/api/approvals/{claim.Id}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
    }

    /// <summary>Que un rol distinto de Administrator pudiera tocar estas rutas lo haría fallar.</summary>
    [Fact]
    public async Task Las_rutas_de_administrador_rechazan_a_quien_no_es_administrador()
    {
        using var scope = _factory.CreateSeedScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TestData.SeedAccount(db, "employee-plain-t4", "empleado.plano.t4@automercado.test", UserRole.Employee);

        var client = _factory.CreateClient();
        AuthorizedClient(client, await LoginAsync(client, "empleado.plano.t4@automercado.test"));

        var response = await client.GetAsync("/api/admin/rate-table");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
