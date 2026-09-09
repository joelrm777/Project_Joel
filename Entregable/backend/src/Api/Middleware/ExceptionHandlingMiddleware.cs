using System.Text.Json;
using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Distances.Services;
using MileageClaims.Modules.Rates.Services;

namespace MileageClaims.Api.Middleware;

/// <summary>
/// Traduce las excepciones de dominio a códigos HTTP en un solo lugar, en vez de repetir
/// try/catch en cada acción de cada controlador (principio de responsabilidad única).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, body) = Map(ex);
            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Error no manejado.");
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }

    private static (int Status, object Body) Map(Exception ex) => ex switch
    {
        EmployeeNotFoundException => (StatusCodes.Status404NotFound, new { message = ex.Message }),
        MileageClaimNotFoundException => (StatusCodes.Status404NotFound, new { message = ex.Message }),
        KeyNotFoundException => (StatusCodes.Status404NotFound, new { message = ex.Message }),

        TripDateOutOfWindowException => (StatusCodes.Status422UnprocessableEntity, new { message = ex.Message, code = "TripDateOutOfWindow" }),
        DuplicateTripException dup => (StatusCodes.Status422UnprocessableEntity, new { message = ex.Message, code = "DuplicateTrip", existingTripId = dup.ExistingTripId, existingClaimId = dup.ExistingClaimId }),
        MissingDistanceException missing => (StatusCodes.Status422UnprocessableEntity, new { message = ex.Message, code = "MissingDistance", missingLegs = missing.MissingLegs.Select(l => new { l.OriginStoreId, l.DestinationStoreId }) }),
        RateNotFoundException => (StatusCodes.Status422UnprocessableEntity, new { message = ex.Message, code = "RateNotFound" }),
        RejectionReasonRequiredException => (StatusCodes.Status400BadRequest, new { message = ex.Message }),

        ClaimNotEditableException => (StatusCodes.Status409Conflict, new { message = ex.Message }),

        ForbiddenClaimAccessException => (StatusCodes.Status403Forbidden, new { message = ex.Message }),
        ApprovalNotOwnedException => (StatusCodes.Status403Forbidden, new { message = ex.Message }),

        EmptyTripException => (StatusCodes.Status400BadRequest, new { message = ex.Message }),
        EmptyClaimException => (StatusCodes.Status400BadRequest, new { message = ex.Message }),

        // Deliberadamente NO hay un catch-all para ArgumentException/InvalidOperationException
        // genéricos: esos tipos también los usan librerías internas (ej. el token JWT) para
        // errores que no son de negocio y no deberían llegarle crudos al usuario. Cada mensaje
        // que sí se expone tiene que venir de un tipo de excepción propio, declarado arriba.
        _ => (StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error inesperado." })
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseMileageClaimsExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
