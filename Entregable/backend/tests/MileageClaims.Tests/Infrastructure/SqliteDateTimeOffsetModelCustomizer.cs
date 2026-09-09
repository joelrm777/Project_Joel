using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MileageClaims.Tests.Infrastructure;

/// <summary>
/// SQLite no puede traducir ORDER BY sobre columnas DateTimeOffset (limitación conocida del
/// proveedor, no de nuestro código: SQL Server lo hace sin problema). Esto convierte esas
/// columnas a un long ordenable, solo para las pruebas — ninguna configuración de producción
/// se toca, esto se registra aparte vía ReplaceService en TestHost.
/// </summary>
public sealed class SqliteDateTimeOffsetModelCustomizer : RelationalModelCustomizer
{
    public SqliteDateTimeOffsetModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
