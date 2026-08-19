using Cine.Nucleo.Contratos;
using Cine.Nucleo.Datos;
using Cine.Nucleo.Dominio;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cine.Nucleo.Servicios;

/// <summary>
/// Apartar butacas, soltarlas y registrar la compra pagada. Las tres operaciones son
/// transaccionales y ninguna deja el mapa en un estado imposible: quien impide la doble venta es
/// la restricción UNIQUE (FuncionId, Fila, Numero), no este código (DISENO.md, decisión 1).
/// </summary>
public interface IServicioVenta
{
    Task<ResultadoApartado> ApartarAsync(int funcionId, IReadOnlyList<Butaca> butacas,
                                         string tokenSesion, int? apartadoId);

    Task<ResultadoOperacion> LiberarButacaAsync(int apartadoId, Butaca butaca);

    Task<ResultadoCompra> PagarAsync(int apartadoId, IReadOnlyList<LineaTarifa> lineas,
                                     Canal canal, string? correo, int? cuentaId,
                                     bool edadDeclarada, string claveIdempotencia);
}

public class ServicioVenta(CineDbContext datos, IServicioCartelera cartelera, IServicioTarifas tarifas)
    : IServicioVenta
{
    /// <summary>Elegir una butaca libre la deja apartada durante 10 minutos (RN-18).</summary>
    public static readonly TimeSpan DuracionDelApartado = TimeSpan.FromMinutes(10);

    /// <summary>Una compra contiene entre 1 y 10 boletos (RN-21).</summary>
    public const int MaximoButacasPorCompra = 10;

    public async Task<ResultadoApartado> ApartarAsync(int funcionId, IReadOnlyList<Butaca> butacas,
                                                      string tokenSesion, int? apartadoId)
    {
        if (butacas.Count == 0)
        {
            return new ResultadoApartado(false, Motivo: MotivoRechazo.SinButacas);
        }

        await using var transaccion = await datos.Database.BeginTransactionAsync();

        var funcion = await datos.Funciones.FirstOrDefaultAsync(f => f.Id == funcionId);
        if (funcion is null)
        {
            return new ResultadoApartado(false, Motivo: MotivoRechazo.FuncionNoEncontrada);
        }

        if (funcion.Estado == EstadoFuncion.Cancelada)
        {
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.FuncionCancelada);
        }

        // Una butaca no vendible no puede ser apartada en ningún canal (RN-2).
        var noVendibles = await datos.ButacasNoVendiblesFuncion
            .Where(b => b.FuncionId == funcionId)
            .Select(b => new { b.Fila, b.Numero })
            .ToListAsync();

        if (butacas.Any(b => noVendibles.Any(n => n.Fila == b.Fila && n.Numero == b.Numero)))
        {
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.ButacaNoVendible);
        }

        var disposicion = await datos.ButacasSala
            .Where(b => b.SalaId == funcion.SalaId)
            .Select(b => new { b.Fila, b.Numero })
            .ToListAsync();

        if (butacas.Any(b => !disposicion.Any(d => d.Fila == b.Fila && d.Numero == b.Numero)))
        {
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.ButacaNoVendible);
        }

        var ahora = await RelojDelMotor.AhoraAsync(datos);

        // Pasados los 20 minutos del inicio, la función queda cerrada para todos los canales y
        // apartar ya no tiene para qué (RN-30).
        if (!VentanaDeVenta.AdmiteApartar(funcion, ahora))
        {
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.FuncionCerrada);
        }

        Apartado? apartado = null;
        if (apartadoId is not null)
        {
            apartado = await datos.Apartados
                .Include(a => a.Ocupaciones)
                .FirstOrDefaultAsync(a => a.Id == apartadoId);

            if (apartado is null || apartado.FuncionId != funcionId || apartado.TokenSesion != tokenSesion)
            {
                return new ResultadoApartado(false, Motivo: MotivoRechazo.ApartadoNoEncontrado);
            }

            if (apartado.VenceEn <= ahora)
            {
                await BorrarApartadoAsync(apartado);
                await datos.SaveChangesAsync();
                await transaccion.CommitAsync();
                return await RechazoConMapaAsync(funcionId, MotivoRechazo.ApartadoVencido);
            }
        }

        var yaApartadas = apartado?.Ocupaciones.Count ?? 0;
        if (yaApartadas + butacas.Count > MaximoButacasPorCompra)
        {
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.LimiteButacas);
        }

        // La ocupación de un apartado vencido se borra dentro de esta misma transacción, para que
        // la restricción de unicidad no tropiece con una fila que ya debería no existir.
        var filas = butacas.Select(b => b.Fila).Distinct().ToList();
        var numeros = butacas.Select(b => b.Numero).Distinct().ToList();

        var vencidas = await datos.OcupacionesButaca
            .Include(o => o.Apartado)
            .Where(o => o.FuncionId == funcionId
                        && filas.Contains(o.Fila) && numeros.Contains(o.Numero)
                        && o.Estado == EstadoButaca.Apartada
                        && o.Apartado != null && o.Apartado.VenceEn <= ahora)
            .ToListAsync();

        foreach (var vencida in vencidas.Where(v => butacas.Contains(new Butaca(v.Fila, v.Numero))))
        {
            datos.OcupacionesButaca.Remove(vencida);
        }

        if (vencidas.Count > 0)
        {
            await datos.SaveChangesAsync();
            await LimpiarApartadosSinButacasAsync(vencidas);
        }

        var vence = ahora.Add(DuracionDelApartado);

        if (apartado is null)
        {
            apartado = new Apartado
            {
                FuncionId = funcionId,
                TokenSesion = tokenSesion,
                CreadoEn = ahora,
                VenceEn = vence
            };
            datos.Apartados.Add(apartado);
        }
        else
        {
            // Agregar una butaca renueva el plazo para todas las del intento (RN-18).
            apartado.VenceEn = vence;
        }

        foreach (var butaca in butacas)
        {
            datos.OcupacionesButaca.Add(new OcupacionButaca
            {
                FuncionId = funcionId,
                Fila = butaca.Fila,
                Numero = butaca.Numero,
                Estado = EstadoButaca.Apartada,
                Apartado = apartado
            });
        }

        try
        {
            await datos.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            await transaccion.RollbackAsync();
            datos.ChangeTracker.Clear();
            return await RechazoConMapaAsync(funcionId, MotivoRechazo.ButacaTomada);
        }

        return new ResultadoApartado(true, apartado.Id, apartado.VenceEn);
    }

    public async Task<ResultadoOperacion> LiberarButacaAsync(int apartadoId, Butaca butaca)
    {
        var apartado = await datos.Apartados
            .Include(a => a.Ocupaciones)
            .FirstOrDefaultAsync(a => a.Id == apartadoId);

        if (apartado is null)
        {
            return ResultadoOperacion.Rechazado(MotivoRechazo.ApartadoNoEncontrado);
        }

        var ocupacion = apartado.Ocupaciones
            .FirstOrDefault(o => o.Fila == butaca.Fila && o.Numero == butaca.Numero);

        if (ocupacion is null)
        {
            return ResultadoOperacion.Rechazado(MotivoRechazo.ButacaNoApartada);
        }

        datos.OcupacionesButaca.Remove(ocupacion);
        apartado.Ocupaciones.Remove(ocupacion);

        // Un apartado sin butacas no existe: el comprador soltó todo lo que había elegido.
        if (apartado.Ocupaciones.Count == 0)
        {
            datos.Apartados.Remove(apartado);
        }

        await datos.SaveChangesAsync();
        return ResultadoOperacion.Aceptado();
    }

    public async Task<ResultadoCompra> PagarAsync(int apartadoId, IReadOnlyList<LineaTarifa> lineas,
                                                  Canal canal, string? correo, int? cuentaId,
                                                  bool edadDeclarada, string claveIdempotencia)
    {
        // Confirmar el pago dos veces produce una sola compra pagada, con un solo código y un
        // solo cobro (RN-23, CA-5).
        var yaPagada = await BuscarPorClaveAsync(claveIdempotencia);
        if (yaPagada is not null)
        {
            return yaPagada;
        }

        await using var transaccion = await datos.Database.BeginTransactionAsync();

        var apartado = await datos.Apartados
            .Include(a => a.Ocupaciones)
            .FirstOrDefaultAsync(a => a.Id == apartadoId);

        if (apartado is null || apartado.Ocupaciones.Count == 0)
        {
            return new ResultadoCompra(false, Motivo: MotivoRechazo.ApartadoVencido);
        }

        var ahora = await RelojDelMotor.AhoraAsync(datos);

        // Una compra solo puede quedar pagada si ninguno de sus apartados venció (RN-22).
        if (apartado.VenceEn <= ahora)
        {
            await BorrarApartadoAsync(apartado);
            await datos.SaveChangesAsync();
            await transaccion.CommitAsync();
            return new ResultadoCompra(false, Motivo: MotivoRechazo.ApartadoVencido);
        }

        var apartadas = apartado.Ocupaciones
            .Select(o => new Butaca(o.Fila, o.Numero))
            .ToHashSet();

        var pedidas = lineas.Select(l => new Butaca(l.Fila, l.Numero)).ToHashSet();

        if (!apartadas.SetEquals(pedidas))
        {
            return new ResultadoCompra(false, Motivo: MotivoRechazo.ButacaNoApartada);
        }

        var funcion = await datos.Funciones
            .Include(f => f.Pelicula)
            .FirstAsync(f => f.Id == apartado.FuncionId);

        if (funcion.Estado == EstadoFuncion.Cancelada)
        {
            await BorrarApartadoAsync(apartado);
            await datos.SaveChangesAsync();
            await transaccion.CommitAsync();
            return new ResultadoCompra(false, Motivo: MotivoRechazo.FuncionCancelada);
        }

        // En línea se vende hasta el instante en que la función inicia; en taquilla, 20 minutos
        // más (RN-28, RN-29). El rechazo libera las butacas del intento (R-6).
        if (!VentanaDeVenta.Permite(funcion, canal, ahora))
        {
            await BorrarApartadoAsync(apartado);
            await datos.SaveChangesAsync();
            await transaccion.CommitAsync();
            return new ResultadoCompra(false, Motivo: MotivoRechazo.FuncionCerrada);
        }

        // Antes de pagar una función con clasificación mayor que cero, el comprador declara que
        // cumple la edad mínima (RN-33, RF-17). En taquilla la edad la ve el operador en puerta.
        if (canal == Canal.EnLinea && funcion.Pelicula!.ClasificacionEdad > 0 && !edadDeclarada)
        {
            return new ResultadoCompra(false, Motivo: MotivoRechazo.EdadNoDeclarada);
        }

        // La tarifa elegida tiene que estar disponible para la fecha de la función, y el monto
        // sale de la configuración vigente en este momento (RN-12 a RN-16).
        var disponibles = await tarifas.TarifasDeAsync(funcion, ahora);
        var montoPorTarifa = disponibles.ToDictionary(o => o.Tarifa, o => o.Monto);

        if (lineas.Any(l => !montoPorTarifa.ContainsKey(l.Tarifa)))
        {
            return new ResultadoCompra(false, Motivo: MotivoRechazo.TarifaNoDisponible);
        }

        var compra = new Compra
        {
            Codigo = await CodigoLibreAsync(),
            FuncionId = apartado.FuncionId,
            Canal = canal,
            PagadaEn = ahora,
            Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim(),
            CuentaId = cuentaId,
            EstadoPago = EstadoPago.Pagada,
            ClaveIdempotencia = claveIdempotencia
        };

        foreach (var linea in lineas)
        {
            compra.Boletos.Add(new Boleto
            {
                FuncionId = apartado.FuncionId,
                Fila = linea.Fila,
                Numero = linea.Numero,
                Tarifa = linea.Tarifa,
                Monto = montoPorTarifa[linea.Tarifa]
            });
        }

        datos.Compras.Add(compra);

        try
        {
            await datos.SaveChangesAsync();

            foreach (var ocupacion in apartado.Ocupaciones)
            {
                var boleto = compra.Boletos
                    .First(b => b.Fila == ocupacion.Fila && b.Numero == ocupacion.Numero);

                ocupacion.Estado = EstadoButaca.Vendida;
                ocupacion.ApartadoId = null;
                ocupacion.Apartado = null;
                ocupacion.BoletoId = boleto.Id;
            }

            datos.Apartados.Remove(apartado);

            await datos.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            await transaccion.RollbackAsync();
            datos.ChangeTracker.Clear();

            // Dos confirmaciones a la vez: la que perdió la carrera devuelve la compra que ganó.
            var compraGanadora = await BuscarPorClaveAsync(claveIdempotencia);
            return compraGanadora ?? new ResultadoCompra(false, Motivo: MotivoRechazo.ButacaTomada);
        }

        return new ResultadoCompra(true, compra.Id, compra.Codigo, compra.Boletos.Sum(b => b.Monto));
    }

    /// <summary>
    /// Un código que no esté en uso. La restricción de unicidad sigue siendo la garantía; esto
    /// evita el reintento en el caso improbable de que dos códigos salgan iguales (RN-25).
    /// </summary>
    private async Task<string> CodigoLibreAsync()
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var codigo = CodigoDeConfirmacion.Nuevo();
            if (!await datos.Compras.AnyAsync(c => c.Codigo == codigo))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de confirmación libre.");
    }

    private async Task<ResultadoCompra?> BuscarPorClaveAsync(string claveIdempotencia)
    {
        var compra = await datos.Compras
            .Include(c => c.Boletos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaveIdempotencia == claveIdempotencia);

        return compra is null
            ? null
            : new ResultadoCompra(true, compra.Id, compra.Codigo, compra.Boletos.Sum(b => b.Monto));
    }

    private async Task<ResultadoApartado> RechazoConMapaAsync(int funcionId, MotivoRechazo motivo)
    {
        var mapa = await cartelera.ObtenerMapaAsync(funcionId);
        return new ResultadoApartado(false, Motivo: motivo, Mapa: mapa);
    }

    private async Task BorrarApartadoAsync(Apartado apartado)
    {
        var ocupaciones = await datos.OcupacionesButaca
            .Where(o => o.ApartadoId == apartado.Id)
            .ToListAsync();

        datos.OcupacionesButaca.RemoveRange(ocupaciones);
        datos.Apartados.Remove(apartado);
    }

    private async Task LimpiarApartadosSinButacasAsync(IEnumerable<OcupacionButaca> borradas)
    {
        var apartados = borradas
            .Select(o => o.ApartadoId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var huerfanos = await datos.Apartados
            .Where(a => apartados.Contains(a.Id) && !a.Ocupaciones.Any())
            .ToListAsync();

        if (huerfanos.Count > 0)
        {
            datos.Apartados.RemoveRange(huerfanos);
            await datos.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 2601 y 2627 son el índice único y la restricción única de SQL Server: la segunda
    /// inserción sobre la misma butaca, el mismo código o la misma clave de idempotencia.
    /// </summary>
    private static bool EsViolacionDeUnicidad(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;
    }
}
