# Boleta de Kilometraje Digital — Diseño

## Panorama de la arquitectura

Un monolito modular en ASP.NET Core (C#), con ocho módulos internos de límites claros
(Boletas, Tarifas, Distancias, Aprobación, Integraciones y Autenticación, Notificaciones,
Temporizador, Auditoría y Reportes), cada uno dueño de sus propias tablas en SQL Server —
ningún módulo lee o escribe directamente sobre las tablas de otro, solo a través de las
interfaces que expone.

El frontend es una SPA en Node.js/TypeScript (React) que consume el backend vía una API
REST; no hay renderizado de vistas del lado del servidor .NET. La autenticación es SSO
contra el AD simulado: el módulo de Integraciones y Autenticación valida al usuario contra
los datos fake del directorio y el backend emite un JWT que la SPA usa en cada llamada a la
API.

El Temporizador es el único componente que no responde a una acción del usuario: es un
`BackgroundService` de .NET, dentro del mismo binario, que cada cierto intervalo le
pregunta a Aprobación por boletas vencidas de recordatorio y a Boletas por rechazadas
vencidas de descarte — la lógica de qué es "vencido" vive en esos módulos, no en el
Temporizador, que solo dispara el ciclo periódico.

## Componentes

### Boletas
**Propósito**: dueño del ciclo de vida de la boleta (`MileageClaim`) y sus viajes.

**Responsabilidades**:
- Crear/editar/retirar la boleta mientras esté pendiente (RN-8); agregar viajes validando
  ventana de tiempo (RN-6) y duplicados (RN-7); declarar vehículo único por boleta (RN-4).
- Calcular el monto total sumando el costo de cada viaje, pidiéndole el costo por km a
  Tarifas y la distancia a Distancias.
- Mantener el estado (Pending/Approved/Rejected/Discarded) y descartar automáticamente una
  rechazada no corregida en 7 días (RN-13), disparado por Temporizador.
- Conservar el detalle completo 7 días desde que llega a finanzas y entregarle el resumen a
  Auditoría y Reportes antes de purgarlo (REG-2).

**Límite con el resto**: no sabe calcular una tarifa ni una distancia — se las pide a
Tarifas/Distancias. No decide si la jefatura aprueba — eso es de Aprobación. No manda
correos — dispara eventos que Notificaciones consume.

### Tarifas
**Propósito**: dueño de la tabla vigente de tarifas y del cálculo de costo por km.

**Responsabilidades**:
- Mantener la tabla (`RateTable`: tipo de transporte × antigüedad × cilindraje ×
  combustible), editable por el administrador.
- Calcular el costo por km de un vehículo declarado (RN-1, RN-2, RN-3), congelando la
  tarifa usada para que un cambio posterior no afecte boletas ya calculadas (RN-16).

**Límite con el resto**: no conoce boletas ni viajes; recibe datos de un vehículo y devuelve
un costo por km. Boletas decide cuándo pedir ese cálculo (una sola vez, al declarar el
vehículo).

### Distancias
**Propósito**: dueño de la tabla de distancias entre tiendas.

**Responsabilidades**:
- Mantener la distancia de cada par de tiendas (`StoreDistance`), editable por el
  administrador.
- Dada una secuencia de tiendas, devolver la distancia total o señalar qué tramo falta
  (RN-5).

**Límite con el resto**: no conoce boletas ni colaboradores. Cuando falta un tramo, es
Boletas quien bloquea el envío y dispara el aviso a administrador/finanzas.

### Aprobación
**Propósito**: dueño del flujo de decisión de la jefatura sobre una boleta.

**Responsabilidades**:
- Asignar la boleta enviada a la jefatura correspondiente; registrar aprobación o rechazo
  todo-o-nada (RN-9), exigiendo motivo en el rechazo (RN-11).
- Marcar boletas vencidas de recordatorio (plazo configurable, por defecto 2 días hábiles)
  para que el Temporizador dispare el aviso repetido (RN-14).
- Avisarle a Boletas el resultado: aprobada pasa a Auditoría y Finanzas; rechazada vuelve a
  pendiente de corrección.

**Límite con el resto**: no sabe cómo se armó el monto ni los viajes — solo ve boleta +
jefatura + estado. No manda los correos, dispara eventos que Notificaciones consume.

### Integraciones y Autenticación
**Propósito**: única puerta hacia los datos "externos" — ERP de RH y AD corporativo —,
simulados en esta versión.

**Responsabilidades**:
- Exponer `IErpRH.BuscarPorCedula(cedula)`, con implementación fake seedeada.
- Exponer `IDirectorioCorporativo` para correo institucional y validación de credenciales
  (SSO simulado); emitir el JWT que usa el resto del sistema para saber quién actúa y con
  qué rol.

**Límite con el resto**: nadie fuera de este módulo sabe que los datos son fake — el resto
del sistema solo ve las interfaces. Migrar a la integración real es cambiar la
implementación sin tocar a nadie más (RN-17, RF-16).

### Notificaciones
**Propósito**: dueño del envío simulado de todos los correos del sistema.

**Responsabilidades**:
- Recibir eventos de los demás módulos (boleta enviada, rechazada con motivo, bloqueada por
  distancia, recordatorio de aprobación) y producir el correo correspondiente.
- Registrar cada envío simulado en `NotificationLog`, para poder mostrar en la demo que "se
  mandó" sin tocar un servidor de correo real.

**Límite con el resto**: no decide cuándo se dispara un correo — solo formatea y "envía"
(simulado) lo que otro módulo le pide.

### Temporizador
**Propósito**: único disparador periódico del sistema.

**Responsabilidades**:
- Despertar cada intervalo configurado (`TimerIntervalMinutes`) y preguntarle a Aprobación
  por vencidos de recordatorio, y a Boletas por rechazadas vencidas de descarte.

**Límite con el resto**: no contiene ninguna regla de negocio — "qué es vencido" vive en
Aprobación/Boletas.

**Limitaciones**: si el proceso se reinicia justo antes de despertar, un recordatorio se
atrasa hasta el próximo ciclo — aceptado por RNF-2.

### Auditoría y Reportes
**Propósito**: dueño del resumen de boletas de largo plazo.

**Responsabilidades**:
- Recibir de Boletas, antes de la purga a los 7 días, el resumen por boleta
  (`MileageClaimSummary`: colaborador, jefatura, monto, tarifa aplicada, distancia total,
  fechas, estado).
- Conservarlo en ventana móvil de 12 meses y responder consultas de monto/volumen/tiempo
  promedio de trámite por jefatura y periodo (RF-15).

**Límite con el resto**: recibe el resumen, no lo calcula.

**Limitaciones**: no puede reconstruir el detalle de tramos de una boleta ya purgada — solo
lo que Boletas le entregó al momento de purgar.

## Modelo de datos

**MileageClaim** — la boleta, mientras existe en su forma completa (0–7 días desde llegada
a finanzas):

| Campo | Qué guarda |
|---|---|
| Id | identificador |
| EmployeeNationalId, EmployeeName, EmployeeEmail | copia tomada del ERP al crear la boleta (RN-17) — única copia existente de estos datos |
| ApproverId, ApproverEmail | jefatura, tomada del ERP junto con el colaborador |
| VehicleType, PlateNumber, DriveType, ModelYear, EngineDisplacement, FuelType | vehículo declarado, uno solo por boleta (RN-4) |
| Status | Draft / Pending / Approved / Rejected / Discarded — Draft es mientras el colaborador todavía la está armando (agregando/quitando viajes) y no la ha enviado; pasa a Pending recién al enviarla |
| SubmittedAt | primer envío |
| DecidedAt | cuándo la jefatura decidió |
| FinanceReceivedAt | se llena al aprobar; dispara el conteo de 7 días (REG-2) |
| RejectionReason | obligatorio cuando Status = Rejected (RN-11) |
| TotalAmount | suma de los viajes |

**Trip** — pertenece a un MileageClaim:

| Campo | Qué guarda |
|---|---|
| Id, MileageClaimId | — |
| Date | validada contra la ventana mes en curso + 2 meses (RN-6) |
| AppliedRatePerKm | congelada al calcular (RN-1, RN-16) |
| TotalDistance | suma de sus tramos |
| TotalAmount | AppliedRatePerKm × TotalDistance |

**Leg** — pertenece a un Trip, uno por cada par de tiendas consecutivas:

| Campo | Qué guarda |
|---|---|
| Id, TripId, SequenceNumber | posición dentro del viaje |
| OriginStoreId, DestinationStoreId | — |
| AppliedDistanceKm | congelada al calcular, copiada de StoreDistance vigente en ese momento; null si ese tramo específico no tiene distancia registrada (RN-5) — Distancias intenta resolver todos los tramos del viaje, no se detiene en el primero que falte |

**Store** — catálogo de las 40 tiendas: Id, Name.

**StoreDistance** — catálogo vigente, mantenido por administrador: OriginStoreId,
DestinationStoreId, DistanceKm.

**RateTable** — catálogo vigente, mantenido por administrador: VehicleType, FuelType,
EngineDisplacementMin, EngineDisplacementMax, VehicleAgeYears (con tope, RN-2), RatePerKm.

**MileageClaimSummary** — lo que Boletas entrega a Auditoría al purgar el detalle a los 7
días, retenido 12 meses móviles:

| Campo | Qué guarda |
|---|---|
| MileageClaimId, EmployeeName, EmployeeNationalId, ApproverId | identifica de quién es cada boleta, incluso purgado el detalle |
| TotalAmount, TotalDistance | totales |
| AppliedRateSummary | descripción de la tarifa usada, sin las filas completas de RateTable |
| SubmittedAt, DecidedAt, Status | tiempo de trámite y conteo por jefatura/periodo |

**NotificationLog** — cada correo simulado: Id, Type, Recipient, SentAt.

**SystemConfiguration** — clave/valor: ReminderIntervalBusinessDays (por defecto 2),
TimerIntervalMinutes.

Con esto, cualquier pregunta de REG-1 se contesta mientras la boleta tiene menos de 7 días
desde que llegó a finanzas (MileageClaim + Trip + Leg completos), y cualquier pregunta de
REG-2 se contesta hasta un año después, vía MileageClaimSummary.

## Manejo de errores

- **Cédula no reconocida** (RN-17): Integraciones y Autenticación devuelve "no encontrado";
  Boletas no crea nada y la SPA muestra mensaje claro. No queda registro de intento.
- **Tramo faltante** (RN-5): Distancias devuelve qué tramo falta; Boletas bloquea el envío
  (la boleta queda en borrador, no en Pending) y dispara notificación a administrador y
  finanzas vía Notificaciones. El colaborador ve qué tramo falta, no un error genérico.
- **Duplicado** (RN-7): Boletas valida cada Trip nuevo contra los ya existentes del mismo
  colaborador (fecha + secuencia de tiendas) antes de guardarlo; si hay choque, rechaza el
  Trip con el detalle del duplicado encontrado.
- **Rechazo de jefatura** (RN-11, RN-12): Aprobación exige RejectionReason antes de aceptar
  el rechazo; sin motivo, la operación no se completa. Boletas vuelve el Status a editable y
  Notificaciones avisa al colaborador con el motivo.
- **Vencimiento sin corregir** (RN-13): Temporizador dispara sobre Boletas; una rechazada
  sin reenvío en el plazo pasa a Discarded. No hay reintento — si el colaborador la
  necesita, crea una nueva.
- **Jefatura sin responder** (RN-14): Temporizador dispara sobre Aprobación; se reenvía el
  mismo correo de aviso, sin cambiar estado. Se repite indefinidamente mientras la boleta
  siga Pending.
- **Cambio de tarifa a mitad de trámite** (RN-16): no es un error del sistema sino una regla
  de congelamiento — ya cubierta en el modelo de datos (AppliedRatePerKm se copia una sola
  vez).
- **Reinicio del proceso**: el Temporizador no tiene estado propio en memoria; al reiniciar,
  vuelve a evaluar qué está vencido contra la base de datos. Ningún estado se pierde, el
  peor caso es un ciclo de demora (RNF-2).

## Decisiones mayores

### Forma arquitectónica general

**Por qué es una decisión mayor:** define cómo se organizan los módulos y qué tan rápido se
puede construir y desplegar con la entrega de mañana.

| | Opción A: Monolito modular | Opción B: Microservicios | Opción C: Monolito simple sin módulos |
|---|---|---|---|
| **Experiencia de uso** | sin diferencia | sin diferencia | sin diferencia |
| **Rendimiento** | sobra al volumen esperado | overhead de red sin beneficio a este volumen | igual que A |
| **Recursos** | una base de datos, un servicio | múltiples servicios y bases de datos, orquestación | igual que A |
| **Complejidad** | la más baja de las tres | alta: varios despliegues, fallos de red entre servicios | baja al inicio, pero sin límites que defender |
| **Riesgo** | ninguno relevante a esta escala | alto de no llegar a un prototipo funcionando para mañana | mezcla de responsabilidades desde el día uno |

**Elección:** Opción A — monolito modular. Cumple el requisito de arquitectura decidida sin
la sobrecarga operativa de microservicios, y es lo más rápido de construir con la entrega
de mañana.

### Motor de base de datos

**Por qué es una decisión mayor:** cambia qué infraestructura hay que levantar y cuánta
fricción hay para correr el proyecto localmente y en CI.

| | Opción A: PostgreSQL | Opción B: SQL Server | Opción C: SQLite |
|---|---|---|---|
| **Experiencia de uso** | sin diferencia | sin diferencia | sin diferencia |
| **Rendimiento** | sobra a esta escala | sobra a esta escala | sobra a esta escala |
| **Recursos** | contenedor/instalación gratuita | SQL Server Express gratis, imagen más pesada | ninguno, es un archivo |
| **Complejidad** | baja, buen soporte en .NET vía Npgsql | integración nativa con EF Core, setup más lento | la más baja para arrancar |
| **Riesgo** | ninguno relevante | fricción de setup en el tiempo disponible | menos "de producción" a ojos de la rúbrica |

**Elección:** Opción B — SQL Server.

### Cómo se simulan el ERP de RH y el AD (SSO)

**Por qué es una decisión mayor:** define si hay un servicio adicional que levantar y
mantener, o si la simulación vive dentro del mismo monolito.

| | Opción A: Servicio HTTP separado | Opción B: Fake en proceso |
|---|---|---|
| **Experiencia de uso** | sin diferencia | sin diferencia |
| **Rendimiento** | una llamada HTTP extra, despreciable | mejor, sin red |
| **Recursos** | un segundo proceso para levantar y desplegar | ninguno adicional |
| **Complejidad** | dos proyectos que mantener; migración futura solo cambia la URL | la más baja; migración futura es una nueva implementación de la misma interfaz |
| **Riesgo** | se puede volver un proyecto paralelo mal mantenido | si la interfaz no está bien pensada, migrar después cuesta más |

**Elección:** Opción B — fake en proceso, detrás de `IErpRH` e `IDirectorioCorporativo`.

### Mecanismo de recordatorios y descartes automáticos

**Por qué es una decisión mayor:** define si hay infraestructura extra para tareas
programadas o si vive dentro del mismo proceso.

| | Opción A: `BackgroundService` interno | Opción B: Scheduler (Hangfire) | Opción C: Cron de SO |
|---|---|---|---|
| **Experiencia de uso** | sin diferencia | agrega un dashboard interno | sin diferencia |
| **Rendimiento** | sobra a este volumen | sobra igual | sobra igual |
| **Recursos** | ninguno adicional | tablas y paquete adicionales | depende de que el hosting soporte cron |
| **Complejidad** | la más baja | configurar dashboard y modelo de reintentos | endpoint expuesto solo para esto, config fuera del repo |
| **Riesgo** | atraso hasta el próximo ciclo si el proceso se reinicia (aceptado por RNF-2) | tiempo de setup sin valor a esta escala | configuración se puede perder sin que nadie lo note |

**Elección:** Opción A — `BackgroundService` interno de .NET.

## Otras decisiones

| Decisión | Opciones consideradas | Elección | Razón |
|---|---|---|---|
| Framework de la SPA | React, Angular, Vue | React | Ecosistema más grande, curva corta para los formularios de boleta y aprobación con la entrega de mañana. |
| ORM del backend | Entity Framework Core, Dapper | Entity Framework Core | Integración directa con SQL Server y migraciones versionadas, sin SQL a mano para este volumen. |
| Mecanismo de sesión SSO simulado | Cookie de sesión, JWT | JWT | SPA y backend son procesos separados; JWT evita cookies cross-origin en el tiempo disponible. |
| Registro de correos simulados | Log de texto plano, tabla `NotificationLog` | Tabla `NotificationLog` | Se puede consultar y mostrar en la demo sin depender de archivos de log. |
| Formato de `AppliedRateSummary` | Snapshot completo de `RateTable`, texto descriptivo | Texto descriptivo | Alcanza para auditar "qué tarifa se usó" sin duplicar toda la fila de `RateTable` en cada boleta. |

## Decisiones dejadas abiertas

| Qué no se decidió | Quién lo decide y cuándo |
|---|---|
| Intervalo exacto del `BackgroundService` (`TimerIntervalMinutes`) | Se deja configurable en `SystemConfiguration`; el valor de arranque lo fija quien construya la pieza del Temporizador, en `PLAN.md`. |
| Estructura de roles y permisos dentro de Integraciones y Autenticación (tablas exactas, cómo se seedean colaborador/jefatura/administrador/finanzas fake) | Quien construya la pieza de autenticación en `PLAN.md`, siguiendo el contrato `IDirectorioCorporativo` ya definido acá. |
