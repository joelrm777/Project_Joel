# Plan de construcción: Boleta de Kilometraje Digital

**Objetivo:** al terminar el plan, el recorrido completo de la boleta funciona de extremo a
extremo (cédula → vehículo → viajes → cálculo → aprobación → finanzas), con las 17 reglas de
negocio, la retención de datos y los reportes por jefatura, persistiendo en SQL Server real.

**Arquitectura:** monolito modular en ASP.NET Core (ocho módulos: Boletas, Tarifas,
Distancias, Aprobación, Integraciones y Autenticación, Notificaciones, Temporizador,
Auditoría y Reportes), cada uno dueño de sus tablas en SQL Server. Frontend SPA en
React/TypeScript consumiendo una API REST. ERP de RH y AD corporativo simulados en proceso,
detrás de `IErpRH` e `IDirectorioCorporativo`.

**Stack:** ASP.NET Core (C#) + Entity Framework Core + SQL Server; React + TypeScript;
autenticación JWT contra el AD simulado.

**Restricciones globales:**
- No se imprime ni se firma ningún documento en papel.
- El sistema no ejecuta el pago (transferencia o desembolso) en sí.
- No hay una segunda aprobación por parte de finanzas.
- No se integra con el ERP de RH ni el AD reales de la compañía — quedan simulados.
- No se diseña para alta disponibilidad ni para picos de concurrencia (RNF-1, RNF-2).

## Cómo usar este plan
- Una pieza por conversación. Al cerrar la pieza, cerrar también la conversación: el
  contexto arranca limpio y barato en la siguiente.
- El encargo de cada pieza referencia `ESPECIFICACION.md` y `DISENO.md`; no los repite.
- Una pieza queda cerrada cuando su comprobación se corrió y el resultado quedó anotado en
  su Evidencia.
- Lo que la construcción revele que falta en la especificación o el diseño se corrige
  primero en ese documento, y después en el código.

## Piezas

| # | Pieza | Depende de | Estado |
|---|---|---|---|
| 1 | Recorrido principal completo (camino feliz) | — | **verificado en vivo** |
| 2 | Bloqueo por falta de distancia entre tiendas | 1 | código completo, sin verificar en vivo |
| 3 | Administrador mantiene tarifas y distancias | 1 | código completo, sin verificar en vivo |
| 4 | Boleta con varios viajes, ventana de tiempo y duplicados | 1 | código completo, sin verificar en vivo |
| 5 | Cédula no reconocida por el ERP | 1 | código completo, sin verificar en vivo |
| 6 | Vigencia de tarifa: un cambio no afecta boletas ya calculadas | 1, 3 | verificado por revisión de código |
| 7 | Edición y retiro de boleta mientras está pendiente | 1 | código completo, sin verificar en vivo |
| 8 | Rechazo de jefatura, corrección y reenvío | 1 | código completo (bug de reinicio de recordatorios corregido), sin verificar en vivo |
| 9 | Temporizador: recordatorio sin respuesta y descarte automático | 1, 8 | código completo, sin verificar en vivo |
| 10 | Retención de 7 días, resumen de auditoría y reportes | 1 | código completo, sin verificar en vivo |

## Detalle

### Pieza 1: Recorrido principal completo (camino feliz)
**Qué tiene que ser cierto**
- Repo con backend ASP.NET Core (monolito modular: Boletas, Tarifas, Distancias, Aprobación,
  Integraciones y Autenticación, Notificaciones, Temporizador, Auditoría y Reportes) y
  frontend React/TypeScript, ambos corren localmente contra SQL Server.
- Datos fake sembrados: al menos 2 colaboradores (uno jefatura del otro), 1 jefatura, 1
  usuario finanzas, en el ERP/AD fake.
- Al menos 1 fila de RateTable y suficientes filas de StoreDistance para conectar al menos 3
  tiendas en secuencia, sembradas directo en la base (sin UI de administrador — eso es
  pieza 3).
- Un colaborador se loguea (SSO simulado); el sistema toma la cédula asociada a su sesión y
  trae sus datos del ERP fake — no hay campo de cédula de texto libre (ver RF-1 actualizado).
- Declara un vehículo (tipo de transporte, placa, tracción, modelo/año, cilindraje,
  combustible) para la boleta.
- Agrega un viaje con fecha dentro del mes en curso y al menos 2 tiendas con distancia
  sembrada.
- El sistema calcula costo por km (RN-1, RN-2, RN-3) y monto, y lo muestra antes de enviar.
- Envía la boleta → queda Pending, se registra un NotificationLog tipo "pendiente de
  aprobación".
- La jefatura ve la boleta pendiente y la aprueba → pasa a Approved, se llena
  FinanceReceivedAt, se registra NotificationLog tipo "aprobada".
- Un usuario finanzas ve la boleta aprobada con su monto.
- Reintentar aprobar/rechazar una boleta ya Approved no tiene efecto (RN-15).

**Con qué se comprueba**
- `POST /api/mileage-claims` con cédula válida, vehículo y viaje con tiendas sembradas → 201,
  MontoTotal = tarifa × distancia calculado a mano.
- `POST /api/mileage-claims/{id}/submit` → Status=Pending; NotificationLog tiene entrada
  "pendiente de aprobación" al correo de la jefatura.
- Logueado como jefatura, `POST /api/approvals/{id}/approve` → Status=Approved,
  FinanceReceivedAt seteado, NotificationLog "aprobada" al colaborador.
- Logueado como finanzas, `GET /api/finance/mileage-claims?status=Approved` → aparece la
  boleta con su monto.
- Reintentar `POST /api/approvals/{id}/approve` (o `/reject`) sobre la misma boleta Approved
  → error, sin cambio de estado.
- En la SPA: recorrer el flujo completo con clicks (login colaborador → crear → enviar →
  login jefatura → aprobar → login finanzas → ver boleta).

**Toca**: Boletas, Tarifas, Distancias, Aprobación, Integraciones y Autenticación,
Notificaciones (parcial).

**Interfaces**
- Consume: —
- Produce: endpoints `POST /api/mileage-claims`, `POST /api/mileage-claims/{id}/submit`,
  `POST /api/approvals/{id}/approve`, `POST /api/approvals/{id}/reject`,
  `GET /api/finance/mileage-claims`; contratos `IErpRH.BuscarPorCedula(cedula)`,
  `IDirectorioCorporativo.ValidarCredenciales(...)`,
  `IDirectorioCorporativo.ObtenerCorreoPorId(...)`; entidades MileageClaim, Trip, Leg, Store,
  StoreDistance, RateTable, NotificationLog; estados Pending, Approved.

**Evidencia**
- 2026-09-09: **verificado en vivo, recorrido completo funcionando** (login de los 4 roles,
  crear boleta, agregar viaje con cálculo de tarifa/distancia correcto, enviar, aprobar,
  ver en finanzas). En el camino aparecieron y se corrigieron 5 bugs reales que el build no
  detecta:
  1. Middleware de errores exponía el mensaje crudo de cualquier `ArgumentException`/
     `InvalidOperationException` no reconocido (incluida una excepción interna de la
     librería de JWT) — se reemplazaron los usos de esas excepciones genéricas por tipos
     propios (`EmptyTripException`, `EmptyClaimException`) y se sacó el catch-all genérico
     de `ExceptionHandlingMiddleware`.
  2. `CreateMileageClaimRequest.EmployeeNationalId` era un `string` no-nulable → ASP.NET
     Core lo trataba como `[Required]` implícito y rechazaba el string vacío que mandaba el
     frontend antes de que el controller lo pisara con la cédula de la sesión — se sacó el
     campo del DTO (el servidor nunca lo necesitó).
  3. Los enums viajaban como número por defecto en `System.Text.Json`, pero la SPA manda y
     espera texto (`"Car"`, `"Approved"`) — se agregó `JsonStringEnumConverter` global en
     `Program.cs`.
  4. EF Core generaba `UPDATE` en vez de `INSERT` para `MileageClaim`/`Trip`/`Leg`/
     `RateTableEntry`/`MileageClaimSummary`/`NotificationLog` nuevos, porque sus Id (Guid)
     se asignan en código pero no estaban marcados `ValueGeneratedNever()` — EF no podía
     distinguir "entidad nueva con Id ya puesto" de "entidad existente", y el `UPDATE`
     afectaba 0 filas (`DbUpdateConcurrencyException`). Se agregó `ValueGeneratedNever()` a
     los seis Id afectados.
  5. La clave de firma JWT del usuario tenía 31 caracteres (248 bits); HS256 exige mínimo
     256 bits — corregida por el usuario en User Secrets.

---

### Pieza 2: Bloqueo por falta de distancia entre tiendas
**Qué tiene que ser cierto**
- Un viaje con dos tiendas consecutivas sin distancia en StoreDistance impide enviar la
  boleta.
- Se notifica a administrador y finanzas indicando qué tramo falta.
- El colaborador ve qué tramo falta, no un error genérico.
- Cargada la distancia faltante, la misma boleta se puede enviar sin más cambios.

**Con qué se comprueba**
- Crear boleta con viaje con un tramo sin distancia sembrada → `submit` responde error 422
  con OriginStoreId/DestinationStoreId del tramo faltante.
- NotificationLog tiene entrada "tramo faltante" a administrador y finanzas, con referencia
  a la boleta y el tramo.
- Insertar la distancia faltante en StoreDistance y reintentar `submit` sobre la misma
  boleta → 200/201, queda Pending.

**Toca**: Boletas, Distancias, Notificaciones, Integraciones y Autenticación (para saber a
qué correos avisar por rol).

**Interfaces**
- Consume: MileageClaim/Trip/Leg y StoreDistance (pieza 1), NotificationLog (pieza 1),
  `IDirectorioCorporativo.ObtenerCorreosPorRol(rol)` — nueva, agregada en esta pieza.
- Produce: `MissingDistanceError` (con OriginStoreId/DestinationStoreId) que la SPA
  interpreta.

**Evidencia**
- 2026-09-08: código completo (`AddTrip` guarda el viaje con tramos incompletos marcados;
  `Submit` reintenta el cálculo y bloquea con 422 + detalle del tramo si sigue faltando;
  se notifica a Administrator y Finance vía `IDirectorioCorporativo.ObtenerCorreosPorRol`).
  `dotnet build` sin errores. Pendiente verificación en vivo (mismo motivo que Pieza 1).

---

### Pieza 3: Administrador mantiene tarifas y distancias
**Qué tiene que ser cierto**
- Un usuario rol Administrador puede ver/crear/editar filas de RateTable, StoreDistance y
  Store.
- Un cambio se refleja en boletas nuevas, no en las ya calculadas (eso lo prueba pieza 6).
- Un usuario sin rol Administrador no puede llamar estos endpoints.

**Con qué se comprueba**
- `POST /api/admin/rate-table` con fila nueva → aparece en `GET /api/admin/rate-table`.
- `PUT /api/admin/store-distances/{originId}/{destinationId}` cambia el valor → un viaje
  nuevo creado después usa el valor actualizado (AppliedDistanceKm).
- `POST /api/admin/stores` agrega tienda nueva → disponible al armar un viaje en la SPA.
- Un colaborador que llama estos endpoints recibe 403.

**Toca**: Tarifas, Distancias, Integraciones y Autenticación (rol Administrador).

**Interfaces**
- Consume: RateTable, StoreDistance, Store, roles (pieza 1).
- Produce: endpoints `/api/admin/rate-table`, `/api/admin/store-distances`,
  `/api/admin/stores`.

**Evidencia**
- 2026-09-08: `AdminController` (backend, ya estaba desde Pieza 1) + página `/admin` en la
  SPA (tres secciones: tarifas, tiendas, distancias — crear/editar). `dotnet build` y
  `npm run build` sin errores. Pendiente verificación en vivo.

---

### Pieza 4: Boleta con varios viajes, ventana de tiempo y duplicados
**Qué tiene que ser cierto**
- Una boleta puede tener 2 o más viajes en fechas distintas, mismo vehículo.
- Un viaje con fecha anterior a mes en curso − 2 meses no se puede agregar (RN-6).
- Un viaje idéntico (misma fecha + misma secuencia de tiendas) del mismo colaborador, en
  otra boleta o en la misma, no se puede agregar (RN-7).

**Con qué se comprueba**
- Crear boleta con 2 viajes de fechas distintas dentro de la ventana → MontoTotal = suma de
  ambos TotalAmount.
- Agregar viaje con fecha de hace 3 meses → error de ventana de tiempo, no se agrega.
- Agregar un viaje y luego intentar el mismo viaje (misma fecha+tiendas) en otra boleta
  nueva del mismo colaborador → error de duplicado, con referencia al Trip original.

**Toca**: Boletas.

**Interfaces**
- Consume: MileageClaim/Trip (pieza 1).
- Produce: `TripDateOutOfWindowError`, `DuplicateTripError` (con referencia al Trip
  original).

**Evidencia**
- 2026-09-08: la lógica ya existía desde Pieza 1 (`ValidateDateWindow` y
  `EnsureNotDuplicate` en `MileageClaimService.AddTrip`) — al revisarla para esta pieza se
  encontró y limpió un parámetro sin usar (`currentClaimId`) en `EnsureNotDuplicate`; se dejó
  un comentario aclarando que RN-7 exige comparar contra todas las boletas del colaborador,
  incluida la actual, por diseño. `dotnet build` sin errores. Pendiente verificación en vivo.

---

### Pieza 5: Cédula no reconocida por el ERP
**Qué tiene que ser cierto**
- Cédula no existente (o marcada inactiva) en el ERP fake no crea ninguna boleta ni avanza
  el flujo; mensaje claro.

**Con qué se comprueba**
- `POST /api/mileage-claims` con cédula no sembrada → 404/422, mensaje claro, ningún
  MileageClaim creado.
- Repetir con cédula sembrada pero marcada inactiva (agregar ese caso a los datos fake) →
  mismo resultado.

**Toca**: Integraciones y Autenticación, Boletas.

**Interfaces**
- Consume: `IErpRH.BuscarPorCedula` (pieza 1).
- Produce: —

**Evidencia**
- 2026-09-08: la ruta de código ya existía desde Pieza 1 (`FakeErpRH.BuscarPorCedula` filtra
  `IsActive`; `MileageClaimService.Create` lanza `EmployeeNotFoundException` → 404 antes de
  tocar la base). Se agregó a `IdentitySeeder` un colaborador inactivo de demo
  (`ex.colaborador@retail.test`, cédula `333333333`, cuenta AD activa pero ERP
  inactivo) para poder disparar el caso real. De paso se corrigió una inconsistencia: el
  formulario de "Nueva boleta" dejaba escribir cualquier cédula, pero el backend siempre usa
  la del colaborador logueado — se sacó el campo (era un dato que el backend ignoraba) y se
  actualizó RF-1 en `ESPECIFICACION.md`. `dotnet build` y `npm run build` sin errores.
  Pendiente verificación en vivo.

---

### Pieza 6: Vigencia de tarifa — un cambio no afecta boletas ya calculadas
**Qué tiene que ser cierto**
- Cambiar el CostoPorKm de una fila de RateTable no afecta boletas Pending que ya calcularon
  su AppliedRatePerKm con el valor viejo.
- Una boleta nueva creada después del cambio usa el valor nuevo.

**Con qué se comprueba**
- Crear boleta Pending con viaje que usa fila X de RateTable, anotar AppliedRatePerKm.
- Como administrador, cambiar el CostoPorKm de la fila X (pieza 3).
- Releer la boleta Pending original → AppliedRatePerKm y TotalAmount sin cambio.
- Crear boleta nueva con el mismo tipo de vehículo → AppliedRatePerKm es el nuevo valor.

**Toca**: Tarifas, Boletas.

**Interfaces**
- Consume: RateTable y su edición (pieza 3); MileageClaim/Trip (pieza 1).
- Produce: —

**Evidencia**
- 2026-09-08: verificado por revisión de código, sin cambios necesarios. `CalculateRate`
  tiene un único llamador por acción de colaborador (`AddTrip`, `UpdateVehicle` en
  `MileageClaimService`); `RateCalculator.Update` (admin) solo muta la fila de `RateTable`
  en el lugar y nunca toca `Trip.AppliedRatePerKm`, que es un valor copiado y congelado. No
  hay ninguna ruta de código (incluido `Submit`) que vuelva a pedir la tarifa de un viaje ya
  agregado. Pendiente verificación en vivo (mismo motivo que piezas anteriores).

---

### Pieza 7: Edición y retiro de boleta mientras está pendiente
**Qué tiene que ser cierto**
- Mientras Pending, el colaborador dueño puede editarla (agregar/quitar viaje, cambiar
  vehículo) o retirarla.
- Una vez Approved, ninguna de estas operaciones está disponible.
- Un colaborador no puede editar/retirar la boleta de otro.

**Con qué se comprueba**
- `PUT /api/mileage-claims/{id}` sobre boleta Pending propia, cambiando el vehículo →
  cambio reflejado, monto recalculado.
- `DELETE /api/mileage-claims/{id}` sobre boleta Pending propia → deja de aparecer en
  consultas del colaborador y de la jefatura.
- `PUT`/`DELETE` sobre boleta Approved → error, sin cambios.
- `PUT`/`DELETE` sobre boleta Pending de otro colaborador → 403.

**Toca**: Boletas.

**Interfaces**
- Consume: MileageClaim (pieza 1).
- Produce: endpoints `PUT /api/mileage-claims/{id}/vehicle` (no `PUT /api/mileage-claims/{id}`
  a secas — se separó para no ambiguar "reemplazar toda la boleta" con "editar el vehículo"),
  `DELETE /api/mileage-claims/{id}`.

**Evidencia**
- 2026-09-08: backend ya cubría todo desde Pieza 1 (`LoadOwned` + `EnsureEditable` en
  `UpdateVehicle`/`AddTrip`/`RemoveTrip`/`Withdraw`: 403 si no es dueño, 409 si no es
  Draft/Pending/Rejected). Faltaba la UI: se agregó a `ClaimDetail` un panel de "Editar
  vehículo" y un botón "Quitar" por viaje en la SPA. `npm run build` sin errores. Pendiente
  verificación en vivo.

---

### Pieza 8: Rechazo de jefatura, corrección y reenvío
**Qué tiene que ser cierto**
- La jefatura rechaza una boleta Pending; sin motivo no se puede rechazar (RN-11).
- Al rechazar, pasa a Rejected, NotificationLog al colaborador con el motivo.
- El colaborador corrige (como en pieza 7) y reenvía → vuelve a Pending, requiere
  aprobación de nuevo.

**Con qué se comprueba**
- `POST /api/approvals/{id}/reject` sin motivo → error, sigue Pending.
- `POST /api/approvals/{id}/reject` con motivo → Rejected, NotificationLog con ese motivo
  al colaborador.
- Editar la boleta Rejected y `submit` de nuevo → Pending, SubmittedAt actualizado.
- La jefatura aprueba → llega a finanzas igual que en pieza 1.

**Toca**: Aprobación, Boletas, Notificaciones.

**Interfaces**
- Consume: MileageClaim (piezas 1, 7), endpoints de aprobación (pieza 1).
- Produce: Status=Rejected, RejectionReason en MileageClaim — insumo de pieza 9.

**Evidencia**
- 2026-09-08: el flujo ya existía desde Pieza 1 (`ApprovalService.Reject` exige motivo antes
  de tocar la boleta — `RejectionReasonRequiredException` → 400; `MarkRejected` solo actúa
  si Status=Pending, RN-15; notifica al colaborador con el motivo). Al revisarlo para esta
  pieza se encontró y corrigió un bug real en `MileageClaimService.Submit`: el reinicio de
  `RemindersSent`/`LastReminderAt` estaba condicionado a `!wasRejected` — es decir, se
  reiniciaba en el primer envío pero NO al reenviar tras un rechazo, dejando el contador de
  recordatorios (RN-14) con datos viejos justo en el caso que esta pieza ejercita. Ahora se
  reinicia siempre que hay un envío nuevo. `dotnet build` sin errores. Pendiente
  verificación en vivo.

---

### Pieza 9: Temporizador — recordatorio sin respuesta y descarte automático
**Qué tiene que ser cierto**
- Boleta Pending sin decisión durante ReminderIntervalBusinessDays (2 días hábiles por
  defecto) dispara recordatorio a la jefatura (RN-14); se repite cada ese plazo mientras
  siga Pending.
- Boleta Rejected no corregida/reenviada en 7 días pasa a Discarded (RN-13).
- El `BackgroundService` corre sin intervención manual y sobrevive un reinicio sin
  duplicar ni perder recordatorios.

**Con qué se comprueba**
- Boleta Pending con SubmittedAt de hace más de 2 días hábiles (fecha forzada en la base
  para el test) → forzar un ciclo del Temporizador → nuevo NotificationLog "recordatorio"
  a la jefatura.
- Repetir el ciclo sin que la jefatura decida → segundo NotificationLog "recordatorio".
- Boleta Rejected con DecidedAt de hace más de 7 días → forzar ciclo → pasa a Discarded.
- Reiniciar el proceso y correr un ciclo → sin recordatorios duplicados ni descartes
  indebidos.

**Toca**: Temporizador, Aprobación, Boletas, Notificaciones.

**Interfaces**
- Consume: MileageClaim.Status/SubmittedAt/DecidedAt (piezas 1, 8);
  SystemConfiguration.ReminderIntervalBusinessDays, TimerIntervalMinutes.
- Produce: `POST /api/admin/timer/run-once` (nuevo — dispara un ciclo a mano, solo
  Administrador, para probar/demostrar sin esperar el intervalo real).

**Evidencia**
- 2026-09-08: la lógica ya existía desde Pieza 1 (`ClaimLifecycleBackgroundService.RunOnce`,
  `ApprovalService.ProcessOverdueReminders` con `BusinessDayCalculator`,
  `IMileageClaimStatusUpdater.ProcessOverdueDiscards`). Se revisó `BusinessDaysBetween` a
  mano (Mon→Mié = 2, Vie→Lun = 1, fines de semana no cuentan) — correcto. Se separó el
  manejo de errores: `RunOnce` ya no traga excepciones (las deja subir), y es el bucle del
  `BackgroundService` el que las atrapa para no morir (RNF-2); esto permite que un disparo
  manual sepa si falló. Se agregó `POST /api/admin/timer/run-once` (backend) y un botón
  "Forzar ciclo ahora" en `/admin` (SPA) para poder demostrar RN-13/RN-14 en la presentación
  sin esperar horas. `dotnet build` y `npm run build` sin errores. Pendiente verificación en
  vivo.

---

### Pieza 10: Retención de 7 días, resumen de auditoría y reportes
**Qué tiene que ser cierto**
- Boleta Approved con FinanceReceivedAt de hace más de 7 días se purga: detalle completo
  (Trip, Leg) deja de estar disponible, queda un MileageClaimSummary.
- Se puede consultar monto total, cantidad de boletas y tiempo promedio de trámite
  (DecidedAt − SubmittedAt), filtrando por jefatura y periodo.

**Con qué se comprueba**
- Boleta Approved con FinanceReceivedAt de hace más de 7 días (fecha forzada) → forzar
  ciclo de purga → `GET /api/mileage-claims/{id}` responde 404/"purgado";
  `GET /api/reports/mileage-claims/{id}` sigue respondiendo con los campos de
  MileageClaimSummary.
- `GET /api/reports/summary?approverId=X&from=...&to=...` → monto total, cantidad y tiempo
  promedio, coincidiendo con datos sembrados de prueba.

**Toca**: Auditoría y Reportes, Boletas.

**Interfaces**
- Consume: MileageClaim con Status=Approved y FinanceReceivedAt (pieza 1).
- Produce: endpoints `GET /api/reports/summary` (filtra por `approverEmail`, no
  `approverId` — se identifica jefatura por correo en todo el sistema, no por un id
  separado), `GET /api/reports/mileage-claims/{id}`.

**Evidencia**
- 2026-09-08: la lógica ya existía desde Pieza 1 (`MileageClaimService.ProcessRetentionPurge`
  guarda el resumen y borra el detalle; `ReportingService.GetSummary`/`GetById`). Faltaba la
  UI: se agregó una sección "Reportes" a `/finanzas` en la SPA (filtro por jefatura/fecha,
  totales y tabla de boletas). `npm run build` sin errores. Pendiente verificación en vivo.

## Cobertura

| Requisito o recorrido | Pieza |
|---|---|
| RF-1, RN-17 (cédula, ERP) | 1, 5 |
| RF-2, RN-4 (vehículo único) | 1 |
| RF-3, RN-6, RN-7 (viajes, ventana, duplicados) | 1, 4 |
| RF-4, RN-1, RN-2, RN-3 (cálculo) | 1 |
| RF-5, RN-5 (bloqueo distancia) | 2 |
| RF-6, RN-8 (edición/retiro) | 7 |
| RF-7, RN-9, RN-10 (aprobación) | 1 |
| RF-8, RN-11, RN-12, RN-13 (rechazo, corrección, descarte) | 8, 9 |
| RF-9, RN-14 (recordatorio) | 9 |
| RF-10 (finanzas tiempo real) | 1 |
| RN-15 (no deshacer aprobada) | 1 |
| RF-12, RN-16 (vigencia tarifa) | 6 |
| RF-13 (admin mantiene tablas) | 3 |
| RF-14, RF-15, REG-1, REG-2 (retención, resumen, reportes) | 10 |
| RF-16 (SSO) | 1, 3 |

## Fuera del plan
- RNF-1, RNF-2 (no alta disponibilidad, no diseño para picos): no requieren ninguna pieza —
  son restricciones que el diseño ya satisface por omisión (sin trabajo adicional que
  construir).
