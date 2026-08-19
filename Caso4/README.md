# Cine Variedades — prototipo de venta de butacas

Prototipo del sistema descrito en [`../Caso3/ESPECIFICACION.md`](../Caso3/ESPECIFICACION.md) y
[`../Caso3/DISENO.md`](../Caso3/DISENO.md). Se construye por piezas según [`PLAN.md`](PLAN.md);
cada pieza cerrada tiene su evidencia anotada ahí.

## Qué corre hoy

Pieza 1 cerrada: la cartelera de la semana y el mapa de butacas de una función, en la aplicación
pública, sin identificarse.

## Qué hace falta tener instalado

| Requisito | Cómo se verifica |
|---|---|
| .NET SDK 10 | `dotnet --version` |
| SQL Server LocalDB (instancia `MSSQLLocalDB`) | `sqllocaldb info` |

LocalDB viene con SQL Server Express y con las cargas de trabajo de datos de Visual Studio. El
destino de despliegue es SQL Server Express; entre uno y otro solo cambia la cadena de conexión.

## Cómo poner a correr la aplicación

Desde esta carpeta (`Caso4`):

```bash
dotnet tool restore                                  # instala dotnet-ef del manifiesto
dotnet dotnet-ef database update --project src/Cine.Nucleo   # crea la base y siembra el catálogo
dotnet run --project src/Cine.Web.Publica
```

La aplicación queda en la dirección que imprime la consola. Para fijar el puerto:

```bash
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Cine.Web.Publica --no-launch-profile
```

- `/` — cartelera de la semana en curso, de jueves a miércoles.
- `/funcion/{id}` — mapa de butacas de una función; se actualiza solo cada 5 segundos.
- `/api/funciones/{id}/mapa` — el mismo mapa en JSON, que es lo que consulta la pantalla.

## Cómo se recrean los datos de prueba

Los datos vienen de dos lugares distintos:

- **Catálogo** (las dos salas con sus 180 butacas, las tres no vendibles y las tres películas):
  va en la migración, porque es un dato fijo del cine. Se recrea aplicando la migración.
- **Cartelera** (las 14 funciones de la semana en curso): la siembra la aplicación al arrancar, y
  no repite lo que ya sembró. Sus fechas dependen del día en que se corre, por eso no va en la
  migración.

Para volver a empezar de cero:

```bash
dotnet dotnet-ef database drop --force --project src/Cine.Nucleo
dotnet dotnet-ef database update --project src/Cine.Nucleo
dotnet run --project src/Cine.Web.Publica
```

La cadena de conexión está en `src/Cine.Web.Publica/appsettings.json`, con el nombre `CineDb`.
Las herramientas de migración usan la variable de entorno `CINE_CADENA_CONEXION` si está puesta, y
si no, la misma base de desarrollo.

## Cómo se corren las pruebas

```bash
dotnet test
```

Las pruebas crean y borran su propia base en LocalDB, con nombre `CinePruebas_<identificador>`.

## Estructura

```
Caso4/
  src/Cine.Nucleo/          biblioteca de dominio: entidades, reglas y acceso a datos
  src/Cine.Web.Publica/     aplicación del comprador: cartelera y mapa de butacas
  pruebas/Cine.Nucleo.Pruebas/
  PLAN.md                   el plan de construcción, con la evidencia de cada pieza cerrada
```

`Cine.Web.Operacion` —taquilla, puerta, cartelera y reportes— llega en la pieza 4 del plan.

## Dependencias adoptadas

| Dependencia | Para qué | Repositorio oficial |
|---|---|---|
| .NET SDK 10 | Plataforma y herramientas de compilación | https://github.com/dotnet/sdk |
| ASP.NET Core (Razor Pages, Kestrel) | La aplicación web pública | https://github.com/dotnet/aspnetcore |
| Microsoft.EntityFrameworkCore.SqlServer 10.0.11 | Persistencia y proveedor de SQL Server | https://github.com/dotnet/efcore |
| Microsoft.EntityFrameworkCore.Design 10.0.11 | Generación de migraciones | https://github.com/dotnet/efcore |
| dotnet-ef 10.0.11 | Herramienta de línea de comandos de migraciones | https://github.com/dotnet/efcore |
| xunit 2.9.3 | Marco de pruebas | https://github.com/xunit/xunit |
| xunit.runner.visualstudio 3.1.4 | Ejecución de las pruebas desde `dotnet test` | https://github.com/xunit/visualstudio.xunit |
| Microsoft.NET.Test.Sdk 17.14.1 | Infraestructura de ejecución de pruebas | https://github.com/microsoft/vstest |
| coverlet.collector 6.0.4 | Cobertura de pruebas | https://github.com/coverlet-coverage/coverlet |

Motor de base de datos: **SQL Server LocalDB** en desarrollo y **SQL Server Express** en
despliegue. Es producto de Microsoft, sin repositorio público de código; su documentación oficial
está en https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb

Qué se rompe si alguna desaparece: EF Core y su proveedor de SQL Server son los únicos que están
atados al código de persistencia —cambiarlos significa reescribir `Cine.Nucleo/Datos`—; el resto
son herramientas de compilación y de pruebas, reemplazables sin tocar el dominio.
