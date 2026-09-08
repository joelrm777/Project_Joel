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
| 1 | Recorrido principal completo (camino feliz) | — | pendiente |
| 2 | Bloqueo por falta de distancia entre tiendas | 1 | pendiente |
| 3 | Administrador mantiene tarifas y distancias | 1 | pendiente |
| 4 | Boleta con varios viajes, ventana de tiempo y duplicados | 1 | pendiente |
| 5 | Cédula no reconocida por el ERP | 1 | pendiente |
| 6 | Vigencia de tarifa: un cambio no afecta boletas ya calculadas | 1, 3 | pendiente |
| 7 | Edición y retiro de boleta mientras está pendiente | 1 | pendiente |
| 8 | Rechazo de jefatura, corrección y reenvío | 1 | pendiente |
| 9 | Temporizador: recordatorio sin respuesta y descarte automático | 1, 8 | pendiente |
| 10 | Retención de 7 días, resumen de auditoría y reportes | 1 | pendiente |

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
- Un colaborador se loguea (SSO simulado), ingresa su cédula, ve sus datos traídos del ERP
  fake.
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

**Toca**: Boletas, Distancias, Notificaciones.

**Interfaces**
- Consume: MileageClaim/Trip/Leg y StoreDistance (pieza 1), NotificationLog (pieza 1).
- Produce: `MissingDistanceError` (con OriginStoreId/DestinationStoreId) que la SPA
  interpreta.

**Evidencia**

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
- Produce: endpoints `PUT /api/mileage-claims/{id}`, `DELETE /api/mileage-claims/{id}`.

**Evidencia**

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
- Produce: —

**Evidencia**

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
- Produce: endpoints `GET /api/reports/summary`, `GET /api/reports/mileage-claims/{id}`.

**Evidencia**

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
