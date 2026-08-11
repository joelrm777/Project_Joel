# Venta de butacas del Cine Variedades — Diseño

Este documento describe la forma de la solución para la especificación de `ESPECIFICACION.md`.
Toda referencia RN, REG, RF, RNF, CA y R apunta a ese documento.

## Panorama de la arquitectura

La solución son **dos aplicaciones ASP.NET Core desplegadas por separado** que comparten una
**biblioteca de dominio** y una **base de datos SQL Server Express**. La aplicación pública
atiende al comprador que llega desde el teléfono: cartelera, mapa de butacas, apartado y pago
simulado. La aplicación de operación atiende al personal con cuenta: venta en taquilla,
verificación en puerta, programación de la cartelera, tarifas y reportes.

Las dos aplicaciones no se conocen entre sí. No hay llamadas de una a la otra, no comparten
sesión y ninguna sabe la dirección de la otra. Todo lo que tienen en común está en dos lugares:
la biblioteca `Cine.Nucleo`, que contiene las entidades, las reglas y el acceso a datos, y la base
de datos, que es la única verdad sobre qué butaca está ocupada. Eso es lo que impide que la
taquilla cobre distinto que internet: ninguna de las dos aplicaciones contiene una regla de
negocio propia, solo la interfaz y la sesión de su público.

La butaca ocupada se representa con una fila en `OcupacionButaca` protegida por una restricción de
unicidad sobre (función, fila, número). Apartar es insertar esa fila dentro de una transacción
corta. Si dos compradores tocan C4 al mismo tiempo, el motor deja pasar una inserción y rechaza la
otra, sin código de coordinación de nuestra parte y sin importar qué aplicación la originó. Esa es
la garantía de CA-1 y RNF-1, y es la razón de que las dos aplicaciones puedan escribir a la vez.

```
   Comprador (teléfono)                    Operador / Administradora
           |                                          |
           v                                          v
  +--------------------+                    +----------------------+
  |  Cine.Web.Publica  |                    | Cine.Web.Operacion   |
  |  cartelera, mapa,  |                    | taquilla, puerta,    |
  |  apartado, pago    |                    | cartelera, reportes  |
  |  (sin sesión)      |                    | (sesión + rol)       |
  +--------------------+                    +----------------------+
           |     referencia de proyecto           |
           +------------------+-------------------+
                              v
                  +-----------------------+
                  |     Cine.Nucleo       |  reglas, entidades,
                  |  (biblioteca .NET)    |  cálculo de tarifa,
                  +-----------------------+  acceso a datos
                              |
                              v
                  +-----------------------+
                  |  SQL Server Express   |  UNIQUE (FuncionId, Fila, Numero)
                  +-----------------------+
                              ^
                              |
                  +-----------------------+
                  | LimpiadorApartados    |  tarea de fondo,
                  | (dentro de Operacion) |  borra apartados vencidos
                  +-----------------------+
```

## Componentes

### Componente 1: Cine.Nucleo

**Propósito**: contener las reglas del negocio y el acceso a datos, de modo que exista una sola
implementación de cada regla para los dos canales de venta.

**Responsabilidades**:
- Definir las entidades del modelo de datos y su persistencia con Entity Framework Core.
- Calcular qué tarifas están disponibles para una función y qué monto corresponde a cada butaca
  (RN-11 a RN-16).
- Apartar butacas, liberar apartados vencidos y registrar compras pagadas de forma transaccional
  (RN-17 a RN-27).
- Verificar la ventana de venta según el canal (RN-28 a RN-31).
- Registrar ingresos, cobros de diferencia y cancelaciones con su reembolso masivo (RN-32 a RN-44).
- Validar solapes y limpieza entre funciones de la misma sala (RN-6).
- Producir los conjuntos de datos de los cuatro reportes (RF-26 a RF-29).
- Es una biblioteca de clases: no se despliega, no escucha en ningún puerto, no tiene interfaz.

**Superficie pública** — es el contrato que las dos aplicaciones consumen:

```csharp
// Cartelera y mapa
Task<IReadOnlyList<FuncionEnCartelera>> ObtenerCarteleraAsync(DateOnly desde, DateOnly hasta);
Task<MapaFuncion>                       ObtenerMapaAsync(int funcionId);

// Apartado y compra
// apartadoId null crea un apartado nuevo; con valor, agrega butacas al existente y renueva su plazo
Task<ResultadoApartado> ApartarAsync(int funcionId, IReadOnlyList<Butaca> butacas,
                                     string tokenSesion, int? apartadoId);
Task<ResultadoOperacion> LiberarButacaAsync(int apartadoId, Butaca butaca);
Task<ResultadoCompra>   PagarAsync(int apartadoId, IReadOnlyList<LineaTarifa> lineas,
                                   Canal canal, string? correo, int? cuentaId,
                                   bool edadDeclarada, string claveIdempotencia);

// Puerta
Task<Compra?>          BuscarCompraAsync(string codigoOCorreo);
Task<ResultadoIngreso> RegistrarIngresoAsync(string codigo, int cuentaId, ResultadoIngresoTipo tipo);
Task<ResultadoCompra>  CobrarDiferenciaAsync(int boletoId, int cuentaId);

// Cartelera (solo administradora)
Task<ResultadoProgramacion> ProgramarFuncionAsync(int peliculaId, int salaId, DateTime inicioLocal);
Task<ResultadoOperacion>    ModificarFuncionAsync(int funcionId, DateTime nuevoInicio, int nuevaSalaId);
Task<ResultadoCancelacion>  CancelarFuncionAsync(int funcionId, int cuentaId, string motivo);

// Reportes
Task<ReporteDistribuidor>  ReporteDistribuidorAsync(int anio, int mes);
Task<ReporteOcupacion>     ReporteOcupacionAsync(DateOnly desde, DateOnly hasta);
Task<ReporteCanal>         ReporteCanalAsync(DateOnly desde, DateOnly hasta);
Task<ReporteCancelaciones> ReporteCancelacionesAsync(DateOnly desde, DateOnly hasta);
```

**Límite con el resto**: no sabe que existen aplicaciones web. No conoce HTTP, ni sesiones, ni
cookies, ni roles de interfaz. Recibe el identificador de la cuenta ya autenticada y lo registra;
no autentica a nadie. Promete tres cosas a quien lo consume: que ninguna operación deja el mapa en
un estado imposible, que toda regla RN se cumple sin importar el canal, y que devuelve el motivo
del rechazo en un valor de retorno en vez de lanzar excepciones para los casos de negocio
esperables (butaca tomada, apartado vencido, función cerrada, compra ya ingresada). Lo que **no**
promete: no valida permisos por rol —eso es de cada aplicación— y no envía correos.

**Limitaciones**:
- Las dos aplicaciones deben desplegarse con la misma versión de la biblioteca. Una versión vieja
  en un despliegue significa reglas distintas por canal, que es justo lo que este componente
  existe para evitar. Cada aplicación expone la versión del núcleo que tiene cargada para que la
  diferencia se vea sin adivinar.
- Usa la hora del servidor de base de datos para todo vencimiento. Un reloj mal puesto corre los
  10 minutos del apartado y el margen de 20 minutos de taquilla.

### Componente 2: Cine.Web.Publica

**Propósito**: dejar que cualquiera vea la cartelera, elija su butaca desde el teléfono y pague, sin
identificarse.

**Responsabilidades**:
- Mostrar la cartelera de la semana y el mapa de butacas de una función (RF-6, RF-9).
- Volver a consultar el mapa cada 5 segundos mientras esté abierto y redibujarlo.
- Enviar los apartados y el pago al núcleo, y mostrar el motivo cuando lo rechaza.
- Pedir la declaración de edad mínima antes de pagar cuando la película lo exige (RF-17).
- Mostrar el código de confirmación en pantalla al terminar la compra, y pedir el envío del correo.
- Identificar al visitante con un token de sesión anónimo, sin cuenta ni registro, para saber de
  quién es cada apartado.

**Límite con el resto**: solo depende de `Cine.Nucleo`. No conoce la aplicación de operación ni sus
rutas. No expone ninguna operación que exija cuenta: no vende con tarifa que no corresponda a la
fecha, no verifica ingresos, no cancela funciones, no consulta reportes y no lee datos de otras
compras. La única forma de recuperar una compra desde aquí es no haberla perdido: el código se
muestra una vez y se manda por correo. Promete a su público que el mapa que ve tiene a lo sumo 5
segundos de atraso y que un apartado aceptado le pertenece por 10 minutos.

**Limitaciones**:
- La ventana de 5 segundos deja un caso en el que el comprador toca una butaca que otro acaba de
  tomar. El rechazo del núcleo lo cubre; no rompe ninguna regla, solo se ve como un aviso.
- Si el visitante pierde el token de sesión —cierra el navegador, cambia de dispositivo— pierde sus
  apartados vigentes. Vencen solos y las butacas vuelven a estar libres.

### Componente 3: Cine.Web.Operacion

**Propósito**: darle al personal del cine todo lo que hoy hace con el cuaderno y el lápiz, y los
reportes que hoy no tiene.

**Responsabilidades**:
- Autenticar operador y administradora con usuario y contraseña, y sostener la sesión (RF-30).
- Aplicar el control de rol: negarle al operador lo que es exclusivo de la administradora (RF-32,
  RN-47).
- Vender en taquilla sobre el mismo mapa, con el margen de 20 minutos del canal presencial
  (RF-15), y advertir la butaca aislada sin bloquear la venta (RF-16).
- Buscar compras por código o correo, mostrar qué boletos exigen carné y qué edad aplica, marcar
  ingresos y registrar denegaciones (RF-18 a RF-22).
- Cobrar la diferencia del boleto estudiante sin carné (RF-20).
- Programar, modificar, eliminar y cancelar funciones; registrar películas; marcar butacas no
  vendibles; fijar tarifas (RF-1 a RF-7, RF-23).
- Mostrar los cuatro reportes y exportar el del distribuidor a un archivo de texto separado por
  comas (RF-26 a RF-29).
- Hospedar la tarea de fondo que borra apartados vencidos.

**Límite con el resto**: solo depende de `Cine.Nucleo`. No conoce la aplicación pública ni le
notifica nada; los cambios que produce se ven en los teléfonos por el refresco de 5 segundos de
aquellos. Toda su superficie exige sesión: no tiene ni una ruta anónima. Promete que cada venta,
ingreso, cobro y cancelación queda atribuido a la cuenta que lo hizo (RN-46).

**Limitaciones**:
- Si esta aplicación está caída, la limpieza de apartados vencidos se detiene. No afecta la
  corrección: el núcleo ignora los apartados vencidos al leer el mapa y al pagar, así que una
  butaca con apartado vencido se ve y se vende como libre aunque su fila todavía exista.
- El personal necesita la aplicación arriba para vender. Con la aplicación caída se vuelve al
  papel y se registra después, que es lo que RNF-4 acepta.

### Componente 4: Base de datos SQL Server Express

**Propósito**: guardar el estado del cine y hacer cumplir por sí misma la regla que ningún código
puede garantizar solo: que una butaca de una función pertenece a lo sumo a uno.

**Responsabilidades**:
- Sostener el modelo de datos de la sección siguiente.
- Rechazar por restricción de unicidad la segunda inserción sobre la misma (función, fila, número).
- Rechazar por restricción de unicidad un segundo código de confirmación repetido y una segunda
  compra con la misma clave de idempotencia (RN-23, RN-25).
- Ser la fuente de la hora de vencimiento de los apartados.

**Límite con el resto**: la consumen las dos aplicaciones, siempre a través de `Cine.Nucleo`.
Ninguna aplicación escribe SQL propio. Promete que las restricciones se cumplen aunque el código
que las escribe tenga un error.

**Limitaciones**:
- El límite de 10 GB de la edición Express es irrelevante para 700 boletos diarios, pero es un
  límite y queda escrito.
- Es un punto único de falla. RNF-4 lo acepta: una hora sin sistema es tolerable.

### Componente 5: Notificador de correo

**Propósito**: enviarle al comprador su código de confirmación sin que un fallo de correo invalide
la compra.

**Responsabilidades**:
- Enviar el correo con el código después de que la compra quedó pagada.
- Registrar cada intento con su resultado (REG-6).
- Es un adaptador de la aplicación pública, no del núcleo: se invoca después de que la transacción
  de la compra ya se confirmó.

**Límite con el resto**: nadie espera su respuesta para decidir si una compra es válida. Promete
únicamente dejar registro de si pudo enviar o no.

**Limitaciones**:
- No reintenta. Si el envío falla, el comprador se queda con el código que vio en pantalla y en
  puerta lo localizan por correo o por código (R-11).

## Modelo de datos

Toda hora se guarda en hora local de Costa Rica, que es la única zona del cine, y así «funciones
cuya fecha de inicio cae en miércoles» (RN-12) se evalúa sin conversiones.

### Catálogo

| Tabla | Columnas | Notas |
|---|---|---|
| `Pelicula` | Id, Titulo, DuracionMinutos, ClasificacionEdad | ClasificacionEdad 0 = sin restricción (RN-32) |
| `Sala` | Id, Nombre, TotalButacas | Dos filas: 120 y 60 (RN-1) |
| `ButacaSala` | SalaId, Fila, Numero, EsVendible | Clave (SalaId, Fila, Numero). Plantilla **actual** de la sala |
| `ConfiguracionTarifa` | Id, MontoGeneral, MontoEstudiante, VigenteDesde, CuentaId | Una fila por cambio. Nunca se actualiza una existente |
| `Cuenta` | Id, Usuario, HashContrasena, Rol, Activa | Rol: Operador \| Administradora (RN-45) |

### Cartelera

| Tabla | Columnas | Notas |
|---|---|---|
| `Funcion` | Id, PeliculaId, SalaId, InicioLocal, AforoVendible, Estado | Estado: Activa \| Cancelada. `AforoVendible` se copia al crear y no cambia (RN-3, RN-4) |
| `ButacaNoVendibleFuncion` | FuncionId, Fila, Numero | Copia de las butacas no vendibles al momento de crear la función (RN-2, RN-4) |
| `CancelacionFuncion` | Id, FuncionId, CuentaId, Motivo, CreadaEn, ComprasReembolsadas, MontoDevuelto | Una por función cancelada (REG-4, RN-43) |

`Funcion` no guarda hora de fin: se calcula con la duración de la película, y así RN-6 se verifica
contra el dato vigente del catálogo al programar.

### Ocupación y venta

| Tabla | Columnas | Notas |
|---|---|---|
| `Apartado` | Id, FuncionId, TokenSesion, CreadoEn, VenceEn | Un apartado por intento de compra, con sus 1 a 10 butacas. `VenceEn` se recalcula a «ahora + 10 min» cada vez que se le agrega una butaca (RN-18). `TokenSesion` es la cookie anónima del comprador o el identificador de sesión del operador |
| `OcupacionButaca` | Id, FuncionId, Fila, Numero, Estado, ApartadoId, BoletoId | **UNIQUE (FuncionId, Fila, Numero)**. Estado: Apartada \| Vendida. Butaca libre = **no hay fila** |
| `Compra` | Id, Codigo, FuncionId, Canal, PagadaEn, Correo, CuentaId, EstadoPago, Ajustada, IngresadaEn, ClaveIdempotencia | UNIQUE en Codigo y en ClaveIdempotencia. EstadoPago: Pagada \| Reembolsada (RN-27) |
| `Boleto` | Id, CompraId, FuncionId, Fila, Numero, Tarifa, Monto | Tarifa y Monto grabados al vender (RN-16). Tarifa: General \| Miercoles \| Estudiante |
| `Reembolso` | Id, CompraId, CancelacionId, Monto, CreadoEn | Uno por compra reembolsada |
| `Ingreso` | Id, CompraId, CuentaId, OcurrioEn, Resultado, DiferenciaCobrada | Resultado: Admitido \| DenegadoClasificacion \| DenegadoCarne (REG-5) |
| `EnvioCorreo` | Id, CompraId, IntentadoEn, Exitoso, Error | REG-6 |

Relaciones: `Funcion` tiene muchas `OcupacionButaca`, muchas `Compra` y a lo sumo una
`CancelacionFuncion`. `Compra` tiene de 1 a 10 `Boleto` (RN-21), a lo sumo un `Reembolso` y
cualquier cantidad de `Ingreso` —los denegados también quedan—. Cada `Boleto` corresponde a
exactamente una `OcupacionButaca` en estado Vendida, y esa correspondencia es la que impide vender
la misma butaca dos veces.

**Por qué `Apartado` y `OcupacionButaca` están separados:** un apartado agrupa varias butacas de
una misma función para un mismo comprador y tiene un solo vencimiento; las filas de ocupación son
por butaca y son las que la restricción de unicidad protege. Con las dos tablas, vencer un
apartado es borrar sus filas de ocupación, y el estado del mapa nunca queda a medias.

### Cómo se contesta cada pregunta de «Qué queda registrado»

| Registro | De dónde sale |
|---|---|
| REG-1 boletos y dinero por función, película, horario y canal | `Boleto` unido a `Compra` (canal) y a `Funcion` (fecha, hora, película) |
| REG-2 si la venta en línea descongestionó la fila | `Compra.Canal` contado por `Funcion.InicioLocal`, por día y por franja |
| REG-3 ocupación de cada función | `Boleto` contados por FuncionId, sobre `Funcion.AforoVendible`, excluyendo compras con EstadoPago Reembolsada |
| REG-4 cuántas cancelaciones, por qué, cuánto costaron | `CancelacionFuncion` con su Motivo, ComprasReembolsadas y MontoDevuelto |
| REG-5 cuánta gente llegó sin carné y cuánto se cobró de más | `Ingreso` filtrado por Resultado, sumando DiferenciaCobrada |
| REG-6 si el aviso al comprador funciona | `EnvioCorreo` contado por Exitoso |

El reporte del distribuidor (RF-26) es `Boleto` unido a `Funcion` del mes, agrupado por función,
subtotalizado por película, descartando los boletos de compras con EstadoPago Reembolsada (RN-44).

## Flujo de datos

**Apartar una butaca** (R-1 paso 3, R-5, CA-1):

```
Interfaz               Cine.Nucleo                        SQL Server
   | ApartarAsync(f,[C4],token,apId) |                          |
   |------------------------------->|  BEGIN TRAN              |
   |                               |------------------------->|
   |                               |  ¿función activa, no cerrada, butaca vendible?
   |                               |  ¿el apartado suma más de 10 butacas? (RN-21)
   |                               |  DELETE OcupacionButaca de esa butaca si su apartado venció
   |                               |  INSERT Apartado si apId es null, si no UPDATE VenceEn
   |                               |  INSERT OcupacionButaca(f, C4, Apartada)
   |                               |                          |-- UNIQUE OK   -> COMMIT
   |                               |                          |-- UNIQUE viola-> ROLLBACK
   |<-- Aceptado(apartadoId, vence) |                          |
   |<-- Rechazado(ButacaTomada, mapa actualizado)              |
```

**Pagar** (R-1 pasos 6-8, R-10):

```
1. PagarAsync recibe apartadoId, tarifas elegidas, canal, correo, clave de idempotencia
2. Una transacción:
     - si ya existe Compra con esa clave de idempotencia -> se devuelve esa misma compra (RN-23)
     - se verifica que el apartado exista y no haya vencido (RN-22)
     - se verifica la ventana de venta según canal (RN-28, RN-29)
     - se verifica que la tarifa elegida esté disponible para la fecha (RN-12 a RN-14)
     - INSERT Compra con Codigo generado, INSERT de 1..10 Boleto
     - UPDATE OcupacionButaca: Apartada -> Vendida, ApartadoId -> null, BoletoId
     - DELETE Apartado
3. Confirmada la transacción, la aplicación pública pide el correo y registra EnvioCorreo
```

**Cancelar una función** (R-7):

```
Una transacción:
  UPDATE Funcion SET Estado = Cancelada
  INSERT CancelacionFuncion(cuenta, motivo, ComprasReembolsadas, MontoDevuelto)
  UPDATE Compra SET EstadoPago = Reembolsada  (todas las Pagadas de la función)
  INSERT Reembolso por cada una
  DELETE OcupacionButaca de la función   -> butacas libres
  DELETE Apartado de la función          -> apartados anulados
```

## Manejo de errores

| Recorrido | Qué ve quien usa el sistema | Qué queda registrado | Qué se revierte |
|---|---|---|---|
| R-4 apartado vencido | Al volver, el mapa muestra sus butacas libres o tomadas por otro | Nada. Los apartados vencidos no se conservan (REG, exclusiones) | La transacción del apartado nunca existió; las filas de ocupación se borran |
| R-5 butaca tomada primero | Aviso «esa butaca ya no está disponible» y mapa redibujado en el momento | Nada | La inserción se deshace completa; los demás apartados de ese intento tampoco quedan |
| R-6 fuera de la ventana | En línea: «la venta cerró, puede comprar en taquilla hasta las HH:MM». En taquilla, pasados 20 min: «función cerrada» | Nada | Los apartados del intento se liberan |
| R-7 proyector falla | Confirmación con el número de compras que se van a reembolsar antes de ejecutar | `CancelacionFuncion` completa (REG-4) y un `Reembolso` por compra | Nada se revierte: la cancelación es el resultado buscado. No se puede deshacer (RN-43) |
| R-8 estudiante sin carné | El operador ve la diferencia a cobrar por boleto | `Ingreso` con DiferenciaCobrada, boleto pasa a General, compra Ajustada | Si el cobro no se completa, el boleto no cambia de tarifa |
| R-9 no cumple la edad | «No cumple la edad mínima de la película» | `Ingreso` con DenegadoClasificacion | Nada. No hay reembolso (RN-34) |
| R-10 pago doble | La segunda confirmación muestra la misma compra y el mismo código | Una sola `Compra` | La segunda transacción se descarta por la clave de idempotencia |
| R-11 correo no enviado | El código sigue en pantalla, con aviso de que el correo no salió | `EnvioCorreo` con Exitoso = falso y el error | Nada. La compra es válida |
| R-12 editar función con ventas | «Esta función ya vendió N boletos; solo se puede cancelar» | Nada | La modificación no se aplica |
| Base de datos caída | Mensaje de que el sistema no está disponible; en taquilla, instrucción de anotar y registrar después | Nada, porque no hay dónde | Ninguna compra queda a medias: sin transacción confirmada, no hay compra |
| Fallo entre el pago y el correo | Compra confirmada en pantalla | `Compra` pagada, `EnvioCorreo` fallido o ausente | Nada. RNF-3 se cumple: la compra no se pierde |

Regla general de errores: los casos de negocio esperables se devuelven como resultado con motivo y
se muestran en el idioma del negocio; las fallas técnicas se registran con su detalle y al usuario
se le dice qué hacer, no qué se rompió.

## Decisiones mayores

### 1. Cómo se impide la doble venta de una butaca

**Por qué es mayor:** es el requisito central (RNF-1, CA-1). Define dónde vive el estado del
apartado y si el sistema puede correr en más de un proceso.

| | A: unicidad en base de datos | B: bloqueo en memoria | C: cola por función |
|---|---|---|---|
| **Experiencia de uso** | Rechazo inmediato con mapa fresco | Igual, marginalmente más rápido | Espera perceptible el viernes lleno |
| **Rendimiento** | Suficiente; el motor resuelve el conflicto | El mejor | El peor por función |
| **Recursos** | Ninguno extra | Ninguno, pero ata a un solo proceso | Cola y su monitoreo |
| **Complejidad** | Baja | Baja al inicio, alta al reconciliar dos verdades | Alta: cola llena, consumidor caído, reintentos |
| **Riesgo** | Depende del reloj del servidor | Alto: dos instancias rompen RNF-1 en silencio | Sobredimensionado para 180 butacas |

**Elección: A** — cumple CA-1 y RNF-1 sin infraestructura extra, y es lo único que sigue
funcionando cuando dos aplicaciones separadas escriben a la vez, que es lo que la decisión 2
impuso.

### 2. Forma de la aplicación

**Por qué es mayor:** define el límite entre lo que ve el comprador y lo que ve el personal, y qué
queda expuesto a internet. Cambiarlo después es reescribir el despliegue.

| | A: un despliegue, dos interfaces | B: dos aplicaciones separadas | C: renderizado en servidor sin API |
|---|---|---|---|
| **Experiencia de uso** | Igual para ambos públicos | Igual para ambos públicos | El mapa exige recargar la página |
| **Rendimiento** | Suficiente | Suficiente; una caída no arrastra a la otra | Menos tráfico, peor sensación en el mapa |
| **Recursos** | Un proceso, una base | Dos despliegues, dos configuraciones | Los menores |
| **Complejidad** | Baja | Alta: hay que compartir las reglas explícitamente | La más baja |
| **Riesgo** | Interfaz de operación en el mismo host | Duplicar reglas entre canales | Rechazos frecuentes al pagar, contra RNF-6 |

**Elección: B** — el aislamiento entre el público de internet y la operación del cine se consideró
más valioso que el despliegue único: una caída de la aplicación pública no deja a la taquilla sin
vender, que es lo que RNF-4 pide preservar. El riesgo de duplicar reglas, que es el costo real de
esta opción, se neutraliza con la decisión 3.

### 3. Dónde viven las reglas con dos aplicaciones

**Por qué es mayor:** con dos despliegues hay dos caminos para vender una butaca. Si cada uno trae
su lógica, RN-12 a RN-16 y RN-28 a RN-31 se cumplen en un canal y no en el otro.

| | A: núcleo compartido como biblioteca | B: la pública es la única dueña de la venta | C: reglas duplicadas |
|---|---|---|---|
| **Experiencia de uso** | Idéntica en ambos canales | Idéntica, pero taquilla depende de la pública | Divergente sin que nadie se dé cuenta |
| **Rendimiento** | Sin salto de red | Un salto por operación de taquilla | Sin salto |
| **Recursos** | Dos despliegues y un paquete versionado | Los mismos dos despliegues | Los mismos |
| **Complejidad** | Media: desplegar ambas con la misma versión | Media-alta: autenticación entre aplicaciones y errores de red en medio de una venta | Baja al escribir, alta al mantener |
| **Riesgo** | Desincronizar versiones | Anula la razón de haber elegido B en la decisión 2 | Que taquilla cobre distinto que internet |

**Elección: A** — conserva el aislamiento que motivó la decisión 2, deja una sola fuente de las
reglas, y la unicidad de la decisión 1 sigue protegiendo la doble venta aunque las dos
aplicaciones escriban al mismo tiempo.

### 4. Cómo se guarda la ocupación del mapa

**Por qué es mayor:** es el dato que se lee en cada toque del mapa y el que sostiene CA-1. Cambia
el volumen de escritura y cómo se congela el aforo de RN-3 y RN-4.

| | A: solo butacas ocupadas | B: pre-crear todas las butacas | C: mapa serializado en un campo |
|---|---|---|---|
| **Experiencia de uso** | Igual | Igual | Igual mientras funcione |
| **Rendimiento** | Escribe solo lo vendido | ~10 000 filas por semana de cartelera | Una escritura por toque, sobre el mismo campo |
| **Recursos** | El menor | Mayor, sin llegar a importar | El menor |
| **Complejidad** | Media: el mapa combina disposición de sala y filas ocupadas | La más baja para leer y reportar | Baja |
| **Riesgo** | La combinación mal hecha desincroniza el mapa | Programar una función se vuelve pesado | **Rompe CA-1**: dos toques a butacas distintas se pisan |

**Elección: A** — mantiene la inserción con unicidad de la decisión 1, no escribe 10 000 filas por
semana para vender 700 boletos, y el aforo guardado en la función es exactamente lo que RN-3 y
RN-4 piden congelar. El riesgo de A se contiene porque esa combinación vive en un solo lugar, el
núcleo de la decisión 3.

### 5. Cómo se mantiene fresco el mapa en el teléfono

**Por qué es mayor:** el viernes lleno, determina cuántas veces le rebota la butaca al comprador. Y
dos de las tres opciones cambian la infraestructura.

| | A: consulta cada 5 segundos | B: empuje desde el servidor | C: una sola carga |
|---|---|---|---|
| **Experiencia de uso** | La butaca tomada se apaga antes de tocarla | La mejor: instantánea | La peor justo en la función que se llena |
| **Rendimiento** | ~10 consultas por segundo con 50 compradores | Menos tráfico, 50 conexiones sostenidas | El menor tráfico |
| **Recursos** | Ninguno extra | Conexiones persistentes y un canal entre las dos aplicaciones | Ninguno |
| **Complejidad** | Baja | Alta: reconexión, y taquilla vive en otra aplicación y también debe notificar | La más baja |
| **Riesgo** | Ventana de 5 segundos, cubierta por el rechazo del servidor | La pieza que más se rompe en producción | Rechazos frecuentes al pagar |

**Elección: A** — cumple el objetivo con lo que ya hay, y evita resolver cómo la aplicación de
taquilla notifica a los teléfonos, que es el costo real de B con dos despliegues separados.

### 6. Plataforma y motor de base de datos

**Por qué es mayor:** la decisión 3 exige una biblioteca compartida por las dos aplicaciones, y eso
solo es literal si ambas hablan el mismo lenguaje. El motor, además, tiene que aceptar dos
procesos escribiendo butacas a la vez.

| | A: C# .NET + SQL Server Express | B: TypeScript + PostgreSQL | C: .NET + SQLite |
|---|---|---|---|
| **Experiencia de uso** | Sin diferencia | Sin diferencia | Bloqueos visibles el viernes |
| **Rendimiento** | Suficiente | Suficiente | Escritores concurrentes se serializan sobre un archivo |
| **Recursos** | Motor instalado; límite de 10 GB | Motor instalado | Un archivo, cero instalación |
| **Complejidad** | Media: más ceremonia, tipos y transacciones fuertes | Baja: la biblioteca compartida es el mismo paquete | La más baja |
| **Riesgo** | Ninguno relevante al tamaño | Ninguno relevante | **Incompatible con la decisión 2**: dos procesos escritores sobre un archivo |

**Elección: A** — .NET con `Cine.Nucleo` como biblioteca de clases referenciada por las dos
aplicaciones ASP.NET Core, sobre SQL Server Express, que soporta los dos escritores concurrentes
que la decisión 2 impone y hace cumplir la restricción de unicidad de la decisión 1. SQLite queda
descartado por esa misma razón, no por tamaño.

## Otras decisiones

| Decisión | Opciones consideradas | Elección | Razón |
|---|---|---|---|
| Duración del apartado | 5, 10, 15 minutos | 10 minutos | RN-18. Alcanza para elegir tarifa y pagar sin bloquear butacas del viernes |
| Formato del código de confirmación | Número correlativo, identificador largo, código corto legible | `CV-` más 5 caracteres sin vocales ni caracteres ambiguos | RF-18: el operador lo escucha y lo digita; se evita confundir O con 0 |
| Dónde corre la limpieza de apartados vencidos | En la pública, en la de operación, en un proceso aparte | En la de operación | Está arriba mientras haya funciones, y su caída no afecta la corrección (el núcleo ignora lo vencido al leer) |
| Cómo se ignora un apartado vencido | Limpiar antes de cada lectura, o filtrar por fecha al leer | Al leer el mapa se filtra por fecha; al apartar se borra dentro de la transacción la ocupación vencida de esa butaca; la tarea de fondo borra el resto | RNF-5: la corrección no depende de que la tarea de fondo corra, y la restricción de unicidad no puede tropezar con una fila que ya debería no existir |
| Butacas de un mismo intento | Un apartado por butaca, o uno por intento con varias butacas | Uno por intento | RN-21 y RN-22 se enuncian sobre la compra completa; un solo vencimiento evita que una butaca del grupo caiga antes que las otras |
| Destocar una butaca antes de pagar | No permitirlo, o liberarla | Liberarla con `LiberarButacaAsync` | El comprador se equivoca al elegir; sin esto tendría que esperar 10 minutos o abandonar la compra |
| Fuente de la hora | Reloj del cliente, del servidor de aplicación, del motor de base de datos | Del motor de base de datos | Dos aplicaciones separadas pueden tener relojes distintos; los vencimientos deben ser comparables entre ellas |
| Zona horaria | Guardar en UTC y convertir, o guardar en hora local | Hora local de Costa Rica | RN-12 se enuncia sobre el día calendario del cine; convertir agrega un error posible sin ningún beneficio |
| Identificación del visitante anónimo | Sin identificación, cookie de sesión, identificador en la dirección | Cookie de sesión con token opaco | Hace falta saber de quién es cada apartado (RN-19) sin crear cuentas (fuera de alcance) |
| Idempotencia del pago | Sin protección, bloqueo de botón en la interfaz, clave de idempotencia en la base | Clave de idempotencia con restricción de unicidad | RN-23 y CA-5 deben cumplirse aunque la interfaz falle o el comprador reintente |
| Contraseñas | Texto plano, hash simple, hash con sal y costo | Hash con sal y factor de costo, con la implementación estándar de ASP.NET Core Identity | RF-30. No se escribe criptografía propia |
| Historial de tarifas | Actualizar la fila de tarifas, o insertar una nueva por cambio | Insertar una nueva por cambio, con la cuenta que la hizo | Permite explicar el monto de un boleto viejo, aunque RN-16 ya lo proteja al grabarlo en el boleto |
| Exportación del reporte del distribuidor | Solo pantalla, texto separado por comas, hoja de cálculo, PDF | Pantalla más texto separado por comas | RF-26. Es lo que se puede abrir en cualquier parte sin biblioteca extra |
| Semilla de datos | Manual, o migración con las dos salas y sus butacas | Migración con las dos salas, sus butacas y una cuenta de administradora | RN-1 es un dato fijo del cine, no algo que alguien deba digitar |
| Advertencia de butaca aislada | Bloquear, advertir, no hacer nada | Advertir sin bloquear | RN-26. Es criterio de quien atiende, no una regla que se pueda imponer |

## Decisiones dejadas abiertas

Decisiones de la solución que quedan para quien implemente. Ninguna bloquea la construcción.

| Qué no se decidió | Quién lo decide y cuándo |
|---|---|
| Dónde se despliegan las dos aplicaciones y bajo qué nombres de host | Quien implemente, al preparar el despliegue. La única restricción del diseño es que ninguna aplicación conozca la dirección de la otra |
| Si la aplicación pública queda detrás de un proxy inverso y con qué certificado | Quien implemente, al desplegar |
| Cómo se respalda la base de datos y cada cuánto | Quien implemente, junto con la administradora. RNF-3 exige que una compra pagada no se pierda; el diseño no fija el mecanismo |
| Cuánto historial se conserva antes de archivar | La administradora, cuando el volumen lo haga notar. Con 700 boletos diarios no es un problema del primer año |
| Aspecto visual y biblioteca de interfaz de las dos aplicaciones | Quien implemente. RNF-6 es la única exigencia: el mapa debe leerse en un teléfono sin ampliar |
| Si el proveedor de correo es un servicio externo o un servidor propio | Quien implemente. El componente 5 lo aísla detrás de un adaptador |
| Cómo se versiona y distribuye `Cine.Nucleo` entre las dos aplicaciones: referencia de proyecto en una misma solución o paquete NuGet interno | Quien implemente, en la primera iteración. La referencia de proyecto alcanza mientras las dos aplicaciones se desplieguen juntas |
