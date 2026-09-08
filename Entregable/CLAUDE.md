# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repositorio

`ESPECIFICACION.md`, `DISENO.md` y `PLAN.md` escritos, revisados y aprobados. Construcción en
curso siguiendo `PLAN.md` pieza por pieza (Pieza 1 lista, ver su Evidencia en `PLAN.md`).

**Este proyecto vive en `Entregable/` dentro de un repo remoto compartido** (`origin` =
`https://github.com/joelrm777/Project_Joel.git`, rama `master`) que también tiene otro
proyecto del curso (`ferreteria-pos/`) con su propio historial — conviven como carpetas
hermanas en la raíz del repo. No tocar `ferreteria-pos/`.

### Backend (`backend/`, ASP.NET Core / C#)
- Build: `dotnet build` desde `backend/`.
- Correr la API: `dotnet run --project src/Api/MileageClaims.Api.csproj` (perfil `https` por
  defecto). En Development aplica migraciones y siembra datos sintéticos automáticamente.
- Requiere dos User Secrets antes de poder correr (ver "Secretos" más abajo).
- No hay suite de tests todavía (pendiente en piezas siguientes de `PLAN.md`).

### Frontend (`frontend/`, React + TypeScript + Vite)
- Instalar deps: `npm install` desde `frontend/`.
- Correr: `npm run dev` (puerto 5173, con proxy a la API en `https://localhost:7064`).
- Build de producción: `npm run build`. Type-check solo: `npx tsc --noEmit`.

### Base de datos (SQL Server, EF Core Code-First)
- Las tablas se crean solas al correr la API en Development (`db.Database.MigrateAsync()`
  en `Program.cs`), leyendo el connection string de User Secrets.
- Agregar una migración nueva tras cambiar el modelo de datos:
  `dotnet ef migrations add <Nombre> --project src/Infrastructure/MileageClaims.Infrastructure.csproj --startup-project src/Api/MileageClaims.Api.csproj -o Migrations`
  (desde `backend/`; requiere `dotnet tool install --global dotnet-ef` una sola vez).
- Generar el script SQL plano de las migraciones (para correrlo a mano en SSMS/Azure Data
  Studio/sqlcmd, o dárselo a alguien sin el proyecto .NET) — **no se versiona**, se regenera
  cuando haga falta:
  `dotnet ef migrations script --project src/Infrastructure/MileageClaims.Infrastructure.csproj --startup-project src/Api/MileageClaims.Api.csproj --idempotent -o scripts/InitialCreate.sql`
- La fábrica de diseño (`AppDbContextFactory` en `Infrastructure`) usa un connection string
  ficticio solo para poder generar migraciones/scripts sin una base real — no confundir con
  el connection string de verdad, que vive únicamente en User Secrets.

**Documentos, en orden de autoridad:** `PROYECTO.md` (enunciado original) → `ESPECIFICACION.md`
(requisitos, reglas de negocio RN-1..RN-17, qué se registra) → `DISENO.md` (arquitectura,
componentes, modelo de datos) → `PLAN.md` (piezas de construcción, con su evidencia de cierre).
Ante cualquier duda de requisito o regla de negocio, `ESPECIFICACION.md` manda sobre
`PROYECTO.md`. Ante cualquier duda de arquitectura, `DISENO.md` manda. Lo que la construcción
revele que falta en la especificación o el diseño se corrige primero en ese documento, y
después en el código.

**Decisiones de arquitectura ya tomadas (ver `DISENO.md` para el detalle y la justificación):**
- Monolito modular en ASP.NET Core (C#), ocho módulos internos con límites claros (Boletas,
  Tarifas, Distancias, Aprobación, Integraciones y Autenticación, Notificaciones,
  Temporizador, Auditoría y Reportes).
- Frontend: SPA en React + TypeScript, consume el backend por API REST.
- Base de datos: SQL Server.
- ERP de RH y AD corporativo: fakes en proceso, detrás de interfaces (`IErpRH`,
  `IDirectorioCorporativo`) — no un servicio HTTP aparte.
- Recordatorios y descartes automáticos: `BackgroundService` interno de .NET, sin scheduler
  externo (no Hangfire, no cron de sistema).

## Convenciones de código

- **Todo identificador de código en inglés**: clases, métodos, variables, tablas, campos,
  rutas de API. Ejemplo: la boleta es `MileageClaim`, un viaje es `Trip`, un tramo es `Leg`,
  una tienda es `Store` (ver `DISENO.md` para el resto del modelo de datos).
- **Los comentarios dentro del código van en español.** Es la única excepción a la regla
  anterior — identificadores en inglés, comentarios en español.
- La documentación de negocio (`ESPECIFICACION.md`, `DISENO.md`, `PLAN.md`, la bitácora) se
  escribe en español, con el glosario de `ESPECIFICACION.md` como fuente de verdad para el
  mapeo término de negocio → nombre en inglés en el código.
- **Backend: principios SOLID.** Cada módulo (Boletas, Tarifas, Distancias, etc.) expone su
  contrato como interfaz; el resto del sistema depende de esa interfaz, no de la
  implementación concreta — es lo que ya permite en `DISENO.md` cambiar el fake de
  Integraciones por la integración real sin tocar nada más. Aplica igual dentro de cada
  módulo: una clase, una razón para cambiar.
- **Frontend: Tailwind CSS**, cuidando que el diseño sea prolijo e intuitivo, no solo
  funcional. Paleta de marca:
  - Verde `#00a159` — color principal/de marca (acciones primarias, aprobaciones).
  - Rojo `#E00614` — alertas, rechazos, bloqueos.
  - Blanco y negro — fondo, texto, superficies neutras.

## Qué es el proyecto

Boleta de Kilometraje Digital: reemplaza el trámite en papel (Excel impreso, firmado a
mano, llevado físicamente a jefatura y luego a finanzas) con el que ~1.000 colaboradores
cobran el kilometraje recorrido entre tiendas de la compañía. Detalle completo del alcance,
recorrido principal, reglas de negocio y supuestos en [ESPECIFICACION.md](ESPECIFICACION.md);
enunciado original en [PROYECTO.md](PROYECTO.md); resumen ejecutivo en
[FICHA-APROBACION.md](FICHA-APROBACION.md).

## Secretos y connection strings

No leer el valor de connection strings, contraseñas, ni ningún secreto que se agregue al
proyecto (`appsettings.json` locales, User Secrets, `.env`, variables de entorno con
credenciales). Está bien saber que un archivo de configuración de ese tipo existe y dónde
vive, pero no abrirlo para ver el valor, ni repetirlo, ni guardarlo en ningún lado (memoria
incluida). Si hace falta tocar ese archivo (agregar una clave nueva, cambiar de motor de
base de datos), pedirle al usuario el valor o que lo complete él mismo.

El backend necesita dos User Secrets para poder correr, que el usuario configura en su propia
terminal (nunca pedírselos por chat ni correr el comando por él):
```
dotnet user-secrets set "ConnectionStrings:Default" "<connection string de Azure SQL>" --project src/Api
dotnet user-secrets set "Jwt:SigningKey" "<cadena aleatoria de al menos 32 caracteres>" --project src/Api
```

## Qué debe quedar registrado al entregar

Bitácora con entradas de gobernanza — dado que el sistema mueve dinero real, cada entrada
relevante debe poder mostrar que el monto calculado usó la tarifa y distancia correctas, y
que ninguna boleta llegó a finanzas sin aprobación real de jefatura. Integración continua
corriendo las pruebas de las 17 reglas de negocio (RN-1..RN-17 en `ESPECIFICACION.md`) en
cada push.
