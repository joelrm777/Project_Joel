# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repositorio

`ESPECIFICACION.md`, `DISENO.md` y `PLAN.md` escritos, revisados y aprobados. Construcción en
curso siguiendo `PLAN.md` pieza por pieza. Cuando exista tooling real (solución .NET,
proyecto npm), actualizar esta sección con los comandos reales de build/lint/test/correr un
test individual — no inventarlos antes de que existan.

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

## Qué debe quedar registrado al entregar

Bitácora con entradas de gobernanza — dado que el sistema mueve dinero real, cada entrada
relevante debe poder mostrar que el monto calculado usó la tarifa y distancia correctas, y
que ninguna boleta llegó a finanzas sin aprobación real de jefatura. Integración continua
corriendo las pruebas de las 17 reglas de negocio (RN-1..RN-17 en `ESPECIFICACION.md`) en
cada push.
