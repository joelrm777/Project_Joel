using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Servicios;

/// <summary>
/// Qué tarifas se pueden aplicar a una función y cuánto vale cada butaca con cada una.
/// La fecha de inicio de la función es lo único que lo decide (RN-12, RN-13, RN-14).
/// </summary>
public interface IServicioTarifas
{
    Task<IReadOnlyList<OpcionTarifa>> TarifasDisponiblesAsync(int funcionId);

    /// <summary>Las tarifas de una función ya cargada, con la configuración vigente al momento.</summary>
    Task<IReadOnlyList<OpcionTarifa>> TarifasDeAsync(Funcion funcion, DateTime momento);
}

public class ServicioTarifas(CineDbContext datos) : IServicioTarifas
{
    public async Task<IReadOnlyList<OpcionTarifa>> TarifasDisponiblesAsync(int funcionId)
    {
        var funcion = await datos.Funciones.FirstOrDefaultAsync(f => f.Id == funcionId);
        if (funcion is null)
        {
            return [];
        }

        var ahora = await RelojDelMotor.AhoraAsync(datos);
        return await TarifasDeAsync(funcion, ahora);
    }

    public async Task<IReadOnlyList<OpcionTarifa>> TarifasDeAsync(Funcion funcion, DateTime momento)
    {
        var configuracion = await VigenteAsync(momento);

        // En las funciones que inician miércoles toda butaca se vende a la mitad de la general,
        // sin excepción, y la tarifa estudiante no se ofrece (RN-12, RN-13).
        if (funcion.InicioLocal.DayOfWeek == DayOfWeek.Wednesday)
        {
            return [new OpcionTarifa(Tarifa.Miercoles, MontoMiercoles(configuracion.MontoGeneral))];
        }

        return
        [
            new OpcionTarifa(Tarifa.General, configuracion.MontoGeneral),
            new OpcionTarifa(Tarifa.Estudiante, configuracion.MontoEstudiante)
        ];
    }

    /// <summary>La mitad de la tarifa general, redondeada al colón (RN-12).</summary>
    public static decimal MontoMiercoles(decimal montoGeneral) =>
        Math.Round(montoGeneral / 2m, 2, MidpointRounding.AwayFromZero);

    private async Task<ConfiguracionTarifa> VigenteAsync(DateTime momento)
    {
        var configuracion = await datos.ConfiguracionesTarifa
            .Where(c => c.VigenteDesde <= momento)
            .OrderByDescending(c => c.VigenteDesde)
            .ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        return configuracion
            ?? throw new InvalidOperationException("No hay ninguna configuración de tarifas vigente.");
    }
}
