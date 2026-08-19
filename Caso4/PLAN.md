# Plan de construcción: Venta de butacas del Cine Variedades

**Objetivo:** dejar corriendo un prototipo del sistema de venta de butacas donde el comprador
elige su butaca desde el teléfono y paga, la taquilla vende sobre ese mismo mapa, la puerta
verifica el ingreso, la administradora programa la cartelera y consulta sus reportes, y la
cancelación de una función reembolsa a todos.

**Arquitectura:** dos aplicaciones ASP.NET Core desplegadas por separado —`Cine.Web.Publica` para
el comprador y `Cine.Web.Operacion` para el personal con cuenta— que no se conocen entre sí y
comparten la biblioteca de dominio `Cine.Nucleo` y una sola base de datos. La butaca ocupada es
una fila de `OcupacionButaca` protegida por `UNIQUE (FuncionId, Fila, Numero)`: esa restricción,
y no código de coordinación, es lo que impide vender la misma butaca dos veces sin importar qué
aplicación la origine.

**Stack:** C# sobre .NET, ASP.NET Core para las dos aplicaciones, `Cine.Nucleo` como biblioteca de
clases referenciada por proyecto, Entity Framework Core para persistencia y migraciones, y
SQL Server como motor. En desarrollo la base corre sobre SQL Server LocalDB (instancia
`MSSQLLocalDB`); el destino de despliegue es SQL Server Express, y entre uno y otro solo cambia la
cadena de conexión (`DISENO.md`, decisión 6 y su nota de entorno). Contraseñas con hash con sal y
factor de costo de la implementación estándar de ASP.NET Core Identity.

**Restricciones globales:** *(copiadas de «Fuera de alcance» de `ESPECIFICACION.md`)*
- Más de un cine. El sistema conoce un cine y sus dos salas.
- Más de una semana de cartelera a futuro. Se programa la semana en curso y la siguiente.
- Cobro real. El pago se simula: no hay conexión a ningún medio de pago ni se guarda ningún dato de tarjeta.
- Devolución real de dinero. El reembolso se registra como hecho.
- Boleto impreso, código de barras o código QR.
- Cuenta de usuario para el comprador. Solo operador y administradora tienen cuenta.
- Cancelación o cambio de compra a pedido del comprador. La compra es firme (RN-24).
- Venta de confitería, abonos, promociones por volumen y tarjetas de regalo.
- Notificación por mensajería o teléfono. El único aviso automático es un correo.
- Asignación automática de butacas. El comprador siempre elige.
- Control de acceso físico. El sistema dice quién compró qué; la puerta la controla una persona.

**Documentos de referencia:** `../Caso3/ESPECIFICACION.md` y `../Caso3/DISENO.md`. Toda referencia
RN, REG, RF, RNF, CA y R de este plan apunta a ellos. El código del prototipo vive en esta misma
carpeta.

## Cómo usar este plan
- Una pieza por conversación. Al cerrar la pieza, cerrar también la conversación: el
  contexto arranca limpio y barato en la siguiente.
- El encargo de cada pieza referencia `ESPECIFICACION.md` y `DISENO.md`; no los repite.
- Una pieza queda cerrada cuando su comprobación se corrió y el resultado quedó anotado
  en su Evidencia.
- Lo que la construcción revele que falta en la especificación o el diseño se corrige
  primero en ese documento, y después en el código.

## Piezas

| # | Pieza | Depende de | Estado |
|---|---|---|---|
| 1 | La dueña ve la cartelera y el mapa de butacas | — | cerrada |
| 2 | Compra en línea a tarifa general | 1 | cerrada |
| 3 | Tarifas y edad mínima en la compra en línea | 2 | cerrada |
| 4 | Venta en taquilla con cuenta | 3 | pendiente |
| 5 | Ingreso a la sala en la puerta | 4 | pendiente |
| 6 | La administradora programa la cartelera | 4 | pendiente |
| 7 | Cancelación de función con reembolso a todos | 6 | pendiente |
| 8 | Los cuatro reportes de la administradora | 7 | pendiente |
| 9 | Correo de confirmación | 3 | pendiente |

**Razón del orden:** primero las dependencias reales; entre piezas independientes, primero la que
despeja más incógnitas del diseño. La pieza 1 levanta la solución, la base y el núcleo dentro del
recorrido que la dueña pidió ver. La pieza 2 va de segunda porque resuelve la incógnita central
—la unicidad como defensa contra la doble venta—, y todo lo demás se apoya en que eso funcione.
La pieza 4 abre el segundo canal sobre el mismo mapa, que es la prueba de que las reglas viven en
un solo lugar, y habilita las piezas 5 y 6, que exigen cuenta. La 7 necesita funciones con ventas
de los dos canales para que el reembolso masivo signifique algo, y la 8 cuenta lo que las
anteriores generaron, con los reembolsos excluidos. La 9 es independiente desde la pieza 3 y va al
final porque un fallo de correo no invalida ninguna compra.

## Detalle

### Pieza 1: La dueña ve la cartelera y el mapa de butacas

**Qué tiene que ser cierto**
- Existe una solución .NET con `Cine.Nucleo` como biblioteca de clases y `Cine.Web.Publica` como
  aplicación ASP.NET Core que la referencia por proyecto.
- Una migración de Entity Framework Core crea en LocalDB las tablas `Pelicula`, `Sala`,
  `ButacaSala`, `Funcion`, `ButacaNoVendibleFuncion` y `OcupacionButaca`, esta última con la
  restricción `UNIQUE (FuncionId, Fila, Numero)`.
- La semilla de la migración deja la Sala 1 con 120 butacas y la Sala 2 con 60 (RN-1), al menos
  tres butacas no vendibles, tres películas —una con clasificación 0 y una con clasificación 12—
  y las funciones de una semana de jueves a miércoles, con al menos una que inicie miércoles.
- `Funcion` guarda su `AforoVendible` al crearse, y sus butacas no vendibles quedan copiadas en
  `ButacaNoVendibleFuncion` (RN-2, RN-3, RN-4).
- Cualquiera consulta la cartelera de la semana sin identificarse, agrupada por día, con película,
  sala, hora y clasificación (RF-6).
- El mapa de una función muestra cada butaca como libre, apartada, vendida o no vendible, y las
  no vendibles no se pueden seleccionar (RF-9).
- El mapa vuelve a consultar el servidor cada 5 segundos y se redibuja sin recargar la página.
- Toda hora se guarda y se muestra en hora local de Costa Rica.

**Con qué se comprueba**
- `dotnet ef database update` crea la base; `SELECT COUNT(*) FROM Sala` devuelve 2 y
  `SELECT COUNT(*) FROM ButacaSala` devuelve 180.
- `dotnet run` levanta la aplicación pública; abrir la raíz muestra las funciones sembradas
  agrupadas por día, sin pedir ninguna identificación.
- Abrir la función de la Sala 1 dibuja 120 butacas; las tres no vendibles se ven distintas y no
  responden al toque.
- Con la ventana del navegador en 390 píxeles de ancho, el mapa completo se distingue butaca por
  butaca sin ampliar y sin desplazamiento horizontal (RNF-6).
- Insertar a mano `INSERT INTO OcupacionButaca (FuncionId, Fila, Numero, Estado) VALUES (1,'C',4,'Vendida')`
  y ver C4 marcada como vendida en la pantalla ya abierta dentro de los 5 segundos siguientes.
- Abrir la dirección de una función inexistente muestra «función no encontrada», no un error del
  servidor.
- Prueba automatizada: `ObtenerMapaAsync` de una función de la Sala 1 devuelve 120 butacas, con
  las no vendibles marcadas como `NoVendible` y el resto como `Libre`.

**Toca:** `Cine.Nucleo`, `Cine.Web.Publica`, base de datos.

**Interfaces**
- Consume: nada.
- Produce:
  - `Task<IReadOnlyList<FuncionEnCartelera>> ObtenerCarteleraAsync(DateOnly desde, DateOnly hasta)`
  - `Task<MapaFuncion> ObtenerMapaAsync(int funcionId)`
  - `FuncionEnCartelera { int FuncionId, string Titulo, int ClasificacionEdad, int DuracionMinutos, string SalaNombre, DateTime InicioLocal, int AforoVendible }`
  - `MapaFuncion { int FuncionId, string Titulo, string SalaNombre, DateTime InicioLocal, int ClasificacionEdad, IReadOnlyList<ButacaEstado> Butacas }`
  - `ButacaEstado { string Fila, int Numero, EstadoButaca Estado }` con
    `EstadoButaca { Libre, Apartada, Vendida, NoVendible }`
  - `Butaca { string Fila, int Numero }`
  - `CineDbContext` y la cadena de conexión con nombre `CineDb`.

**Evidencia** *(18 de agosto de 2026)*

- Base creada desde cero con `dotnet dotnet-ef database drop --force` y `database update`
  (migración `20260819002628_InicialCatalogoYCartelera`). Conteos en LocalDB: `Sala` 2,
  `ButacaSala` 180, `ButacaSala` con `EsVendible = 0` 3, `Pelicula` 3, `Funcion` 0.
- Arrancada la aplicación, la semilla de cartelera creó 14 funciones de la semana en curso
  (jueves 13 a miércoles 19 de agosto), incluida una que inicia miércoles. Un segundo arranque
  crea 0: la semilla no repite lo que ya sembró.
- `GET /` devuelve la cartelera agrupada por día con las 14 funciones, sin pedir identificación.
- `GET /funcion/1` (Sala 1) dibuja 120 butacas en 10 filas —120 elementos con `data-butaca`— y
  `GET /api/funciones/1/mapa` responde 117 `Libre` y 3 `NoVendible`. `Funcion.AforoVendible` de
  esa función es 117 y el de la Sala 2 es 60.
- Insertada a mano la fila `INSERT INTO OcupacionButaca (FuncionId, Fila, Numero, Estado)
  VALUES (1,'C',4,'Vendida')`, el endpoint que consulta la pantalla devuelve `C4 => Vendida`. Es
  el mismo dato que el mapa redibuja cada 5 segundos con `wwwroot/js/mapa.js`.
- La segunda inserción de esa misma butaca la rechaza el motor:
  «Cannot insert duplicate key row in object 'dbo.OcupacionButaca' with unique index
  'IX_OcupacionButaca_FuncionId_Fila_Numero'. The duplicate key value is (1, C, 4).» La
  restricción que sostiene CA-1 ya está puesta, aunque quien la usa es la pieza 2.
- `GET /funcion/9999` responde 404 con la página «Función no encontrada», no con un error del
  servidor.
- Medido en el motor de diseño del navegador, una fila del mapa ocupa 316 px de ancho —letra de
  fila incluida—, así que las 12 butacas caben enteras en la pantalla de 390 px de un teléfono
  sin ampliar (RNF-6); por debajo de 430 px la hoja de estilo las reduce a 20 px y la fila baja a
  unos 280 px. La captura sin ampliar se tomó con Edge en modo headless, que impuso un área de
  492 px: por eso la imagen recorta la fila aunque el ancho medido sea 316 px.
- `dotnet test`: 7 pruebas, todas pasan —mapa de 120 butacas con las no vendibles marcadas, aforo
  congelado, butaca vendida en el mapa, cartelera de jueves a miércoles ordenada, función
  inexistente sin mapa, semana de cartelera y semilla que no se repite—.

---

### Pieza 2: Compra en línea a tarifa general

**Qué tiene que ser cierto**
- Apartar una butaca inserta su fila en `OcupacionButaca` dentro de una transacción corta; la
  segunda inserción sobre la misma butaca la rechaza la restricción de unicidad (RN-19, CA-1).
- Un apartado retiene sus butacas 10 minutos, y agregarle una butaca renueva el plazo a 10 minutos
  para todas las del intento (RN-18).
- Un intento con más de 10 butacas se rechaza (RN-21).
- La ocupación de un apartado vencido se ignora al leer el mapa y se borra dentro de la
  transacción al apartar esa misma butaca (RN-20, RNF-5).
- El comprador puede soltar una butaca que apartó antes de pagar, y esa butaca vuelve a libre (RF-10).
- Pagar registra una `Compra` con código de confirmación único de la forma `CV-` más 5 caracteres
  sin vocales ni caracteres ambiguos, con sus `Boleto` a tarifa general, pasa sus ocupaciones a
  Vendida y borra el `Apartado`, todo en una transacción (RF-13, RN-22, RN-25).
- La clave de idempotencia tiene restricción de unicidad: dos pagos con la misma clave dejan una
  sola compra, un solo código y un solo cobro (RN-23).
- Un rechazo devuelve su motivo y el mapa actualizado, no una excepción (RF-12).
- Cada boleto graba la tarifa aplicada y el monto cobrado en el momento de la compra (RN-16).

**Con qué se comprueba**
- Prueba automatizada: dos `ApartarAsync` simultáneos sobre la butaca C4 de la misma función —uno
  devuelve Aceptado y el otro Rechazado con motivo `ButacaTomada` y el mapa actualizado, y
  `SELECT COUNT(*) FROM OcupacionButaca WHERE FuncionId=@f AND Fila='C' AND Numero=4` devuelve 1
  (CA-1, RNF-1).
- Prueba automatizada: con un apartado creado con vencimiento en el pasado, `ObtenerMapaAsync`
  muestra esa butaca `Libre` y otro token la aparta con éxito (CA-2, RN-20).
- Prueba automatizada: `PagarAsync` dos veces con la misma clave de idempotencia deja una sola
  fila en `Compra`, el mismo código en las dos respuestas y la misma cantidad de `Boleto` (CA-5).
- Prueba automatizada: `PagarAsync` sobre un apartado vencido devuelve Rechazado con motivo
  `ApartadoVencido` y no crea ninguna `Compra` (RN-22).
- Prueba automatizada: apartar 11 butacas devuelve Rechazado con motivo `LimiteButacas` (RN-21).
- En el navegador: elegir dos butacas libres, pagar y ver el código en pantalla; en la base queda
  1 fila en `Compra`, 2 en `Boleto`, 2 ocupaciones en estado Vendida y 0 filas en `Apartado` para
  ese intento.
- En el navegador: apartar tres butacas, soltar una, y verla libre en el mapa mientras las otras
  dos siguen apartadas.

**Toca:** `Cine.Nucleo`, `Cine.Web.Publica`, base de datos.

**Interfaces**
- Consume: `ObtenerMapaAsync`, `MapaFuncion`, `Butaca`, `CineDbContext` de la pieza 1.
- Produce:
  - `Task<ResultadoApartado> ApartarAsync(int funcionId, IReadOnlyList<Butaca> butacas, string tokenSesion, int? apartadoId)`
  - `Task<ResultadoOperacion> LiberarButacaAsync(int apartadoId, Butaca butaca)`
  - `Task<ResultadoCompra> PagarAsync(int apartadoId, IReadOnlyList<LineaTarifa> lineas, Canal canal, string? correo, int? cuentaId, bool edadDeclarada, string claveIdempotencia)`
  - `ResultadoApartado { bool Aceptado, int? ApartadoId, DateTime? VenceEn, MotivoRechazo? Motivo, MapaFuncion? Mapa }`
  - `ResultadoOperacion { bool Exitoso, MotivoRechazo? Motivo }`
  - `ResultadoCompra { bool Exitoso, int? CompraId, string? Codigo, decimal Total, MotivoRechazo? Motivo }`
  - `LineaTarifa { string Fila, int Numero, Tarifa Tarifa }` con `Tarifa { General, Miercoles, Estudiante }`
  - `MotivoRechazo { ButacaTomada, ApartadoVencido, FuncionCerrada, FuncionCancelada, ButacaNoVendible, LimiteButacas, TarifaNoDisponible, EdadNoDeclarada, CompraYaIngresada }`
  - `Canal { EnLinea, Taquilla }`
  - Tablas `Apartado`, `Compra`, `Boleto`.

**Evidencia** *(18 de agosto de 2026)*

- Migración `20260819010244_ApartadoCompraBoleto` aplicada sobre la base existente: agrega
  `Apartado`, `Compra` y `Boleto`, con índices únicos en `Compra.Codigo` y
  `Compra.ClaveIdempotencia`, y la referencia de `OcupacionButaca` a su apartado.
- `dotnet test`: 16 pruebas, todas pasan. Las de esta pieza:
  - Dos `ApartarAsync` simultáneos sobre C4 desde dos conexiones: exactamente uno acepta, el otro
    devuelve `ButacaTomada` con el mapa adjunto donde C4 figura `Apartada`, y queda una sola fila
    en `OcupacionButaca` para esa butaca (CA-1, RNF-1).
  - Con el vencimiento movido al pasado, el mapa muestra la butaca `Libre` y otro token la aparta
    con éxito (CA-2, RN-20).
  - Dos `PagarAsync` con la misma clave de idempotencia: una `Compra`, el mismo código en las dos
    respuestas, dos `Boleto`, cero `Apartado` y dos ocupaciones `Vendida` (CA-5).
  - `PagarAsync` sobre un apartado vencido: `ApartadoVencido`, cero compras y la butaca sin
    ocupación (RN-22).
  - Apartar 11 butacas: `LimiteButacas`, sin dejar ninguna ocupación (RN-21).
  - Agregar una butaca al mismo intento conserva el apartado y renueva su plazo (RN-18).
  - Soltar una de tres butacas la devuelve a `Libre` y deja las otras dos apartadas (RF-10).
  - Apartar A1, que es no vendible: `ButacaNoVendible` (RN-2).
  - El boleto vendido graba `Tarifa = General` y su monto (RN-16).
- Contra la aplicación corriendo, sobre la función 2 (Sala 2):
  - `POST /api/funciones/2/apartar` con B5 devuelve `aceptado: true`, `apartadoId: 1` y su
    vencimiento; agregar B6 al mismo apartado devuelve el mismo `apartadoId` con el plazo
    renovado.
  - Un segundo comprador que pide B5 recibe `aceptado: false`, `motivo: ButacaTomada` y el mapa
    actualizado con B5 en `Apartada` (R-5, RF-12).
  - `POST /api/apartados/1/liberar` con B6 la devuelve a `Libre` mientras B5 sigue `Apartada`.
  - `POST /api/apartados/1/pagar` dos veces con la clave `clave-navegador-1` devuelve las dos
    veces el código `CV-84TGJ` y el total 3500 (R-10, CA-5).
  - En la base quedaron: `Compra` 1 —código `CV-84TGJ`, canal `EnLinea`, estado `Pagada`, correo
    `cliente@ejemplo.cr`—, `Boleto` 1 con `B 5 General 3500.00`, `Apartado` 0, y B5 se ve
    `Vendida` en el mapa.
- La pantalla de compra muestra la selección, el total, el plazo que corre, el campo de correo y
  el botón de pagar, y el código al confirmar. La captura se tomó con la hoja de estilo incrustada
  porque Edge en modo headless no carga recursos por HTTP en esta máquina; el recorrido de la
  pantalla se comprobó contra los mismos endpoints que la página llama.

---

### Pieza 3: Tarifas y edad mínima en la compra en línea

**Qué tiene que ser cierto**
- Las tarifas disponibles de una función salen de su fecha de inicio: si cae miércoles, solo la
  tarifa `Miercoles`, que vale la mitad de la general; cualquier otro día, `General` y
  `Estudiante` (RN-12, RN-13, RN-14).
- El monto de cada butaca se calcula con la `ConfiguracionTarifa` vigente y queda grabado en el
  boleto; un cambio posterior de tarifas no altera boletos ya vendidos (RN-16, RF-8).
- Nunca se aplica más de una tarifa a la misma butaca (RN-15).
- Una función con clasificación mayor que cero exige la declaración de edad mínima antes de
  completar la compra en línea (RF-17, RN-33).
- La venta en línea se rechaza a partir del instante en que la función inicia, y ese rechazo
  libera los apartados del intento (RN-28, R-6).
- La aplicación pública muestra la tarifa elegible por butaca y el total antes de pagar.

**Con qué se comprueba**
- Prueba automatizada sobre una función que inicia miércoles: `TarifasDisponiblesAsync` devuelve
  solo `Miercoles` con monto igual a la mitad de la tarifa general, y `PagarAsync` con una línea
  de tarifa `Estudiante` devuelve Rechazado con motivo `TarifaNoDisponible` (CA-3).
- Prueba automatizada: pagar un boleto a tarifa general, insertar una `ConfiguracionTarifa` nueva
  con otro monto, y verificar que el `Boleto` vendido conserva el monto original (CA-4).
- Prueba automatizada con una función que inicia a las 19:00: `PagarAsync` con la hora del sistema
  en 19:01 devuelve Rechazado con motivo `FuncionCerrada`, y sus butacas quedan sin filas en
  `OcupacionButaca` (CA-7, canal en línea).
- Prueba automatizada: `PagarAsync` con `edadDeclarada` en falso sobre una película de
  clasificación 12 devuelve Rechazado con motivo `EdadNoDeclarada` y no crea `Compra`.
- En el navegador: la función de martes ofrece General y Estudiante con montos distintos; la de
  miércoles no ofrece ninguna de las dos y cobra la mitad de la general por butaca.
- En el navegador: la función de clasificación 12 no deja pagar sin marcar la declaración de edad,
  y la de clasificación 0 no la pide.

**Toca:** `Cine.Nucleo`, `Cine.Web.Publica`, base de datos.

**Interfaces**
- Consume: `PagarAsync`, `LineaTarifa`, `Tarifa`, `MotivoRechazo`, `ResultadoCompra` de la pieza 2.
- Produce:
  - `Task<IReadOnlyList<OpcionTarifa>> TarifasDisponiblesAsync(int funcionId)`
  - `OpcionTarifa { Tarifa Tarifa, decimal Monto }`
  - Tabla `ConfiguracionTarifa { Id, MontoGeneral, MontoEstudiante, VigenteDesde, CuentaId }` con su
    primera fila sembrada.

**Evidencia** *(18 de agosto de 2026)*

- Migración `20260819022907_ConfiguracionTarifa` con su primera fila sembrada: general 3500,
  estudiante 2500, vigente desde el 1 de enero de 2026. El miércoles sale de ahí, a la mitad: 1750.
- `dotnet test`: 24 pruebas, todas pasan. Las de esta pieza:
  - Una función de miércoles devuelve una sola tarifa, `Miercoles` con monto 1750; pagarla con
    `Estudiante` da `TarifaNoDisponible`, y pagarla con `Miercoles` cobra 1750 y lo graba en el
    boleto (CA-3, RN-12, RN-13).
  - Una función de otro día ofrece `General` 3500 y `Estudiante` 2500, y no ofrece `Miercoles`.
  - Una compra de dos butacas con una tarifa distinta en cada una cobra 3500 más 2500 y graba
    cada monto en su boleto (RN-15).
  - Vendido un boleto a 3500 y cambiada después la tarifa general a 5000, el boleto viejo
    conserva 3500 y la compra siguiente ya cobra 5000 (CA-4, RN-16).
  - Sobre una película de clasificación 12, pagar sin declarar la edad da `EdadNoDeclarada` y no
    crea compra; declarándola, la misma compra pasa. Una película de clasificación 0 no la pide
    (RF-17, RN-33).
  - Una función que inició hace un minuto: el pago en línea da `FuncionCerrada` y el rechazo deja
    la función sin ocupaciones ni apartados, y sin compra (CA-7 en línea, R-6).
  - Una función que inició hace 21 minutos ya no admite ni apartar: `FuncionCerrada` (RN-30).
- Contra la aplicación corriendo:
  - La pantalla del miércoles publica una sola tarifa —`Miercoles` a 1750— y el aviso «Función de
    miércoles: toda butaca a mitad de precio, sin tarifa estudiante»; la del jueves publica
    general ₡3 500 y estudiante ₡2 500.
  - La función de clasificación 12 muestra la casilla «Declaro que cumplo la edad mínima de 12
    años» y sin marcarla el botón de pagar queda inhabilitado; la de clasificación 0 no la muestra.
  - Compra de dos butacas del jueves, una general y otra estudiante: código `CV-T3GSN`, total
    6000, y en la base `G 1 General 3500.00` y `G 2 Estudiante 2500.00`.
  - En la función del miércoles: pagar con `Estudiante` devuelve `TarifaNoDisponible`, pagar sin
    declarar edad devuelve `EdadNoDeclarada`, y pagar bien devuelve el código `CV-7J7SX` con total
    1750, grabado como `F 3 Miercoles 1750.00`.
  - Apartar en una función que ya inició hace rato devuelve `FuncionCerrada`.
- La semilla de cartelera pasó a sembrar la semana en curso **y la siguiente** —28 funciones—,
  que es lo que la especificación permite programar (RN-7). Sin la segunda semana, un martes casi
  toda la cartelera ya pasó y no quedaba ninguna función que se pudiera comprar.

---

### Pieza 4: Venta en taquilla con cuenta

**Qué tiene que ser cierto**
- Existe `Cine.Web.Operacion` como aplicación ASP.NET Core separada, que referencia `Cine.Nucleo`
  y no conoce la dirección de la aplicación pública.
- Operador y administradora entran con usuario y contraseña, con hash con sal y factor de costo de
  ASP.NET Core Identity; ninguna ruta de esta aplicación es anónima (RF-30).
- La taquilla vende sobre el mismo mapa y con las mismas reglas de tarifa que la venta en línea,
  con canal `Taquilla` y la cuenta atribuida en la compra (RF-15, RN-46).
- La taquilla acepta ventas hasta 20 minutos después del inicio de la función y las rechaza
  después (RN-29, RN-30).
- Una selección que deje una butaca libre aislada entre dos ocupadas de la misma fila advierte al
  operador sin impedir la venta (RF-16, RN-26).
- Una tarea de fondo hospedada en esta aplicación borra los apartados vencidos y sus filas de
  ocupación.

**Con qué se comprueba**
- Entrar con la cuenta de administradora sembrada abre la aplicación; pedir cualquier ruta sin
  sesión redirige al ingreso.
- Vender dos butacas en taquilla deja una `Compra` con `Canal = Taquilla` y `CuentaId` no nulo, y
  dos `Boleto` con su monto.
- Con una función que inicia a las 19:00: la venta en taquilla se acepta con la hora del sistema
  en 19:19 y se rechaza con motivo `FuncionCerrada` en 19:21 (CA-7, canal taquilla).
- Con B2 y B4 vendidas, seleccionar B3 muestra la advertencia de butaca aislada y la venta se
  completa cuando el operador confirma.
- Apartar una butaca desde la aplicación pública y verla apartada en el mapa de operación dentro
  de los 5 segundos siguientes, sin recargar la página.
- Crear un apartado con vencimiento en el pasado y verificar que la tarea de fondo borra sus filas
  de `OcupacionButaca` dentro del minuto siguiente.

**Toca:** `Cine.Nucleo`, `Cine.Web.Operacion`, base de datos.

**Interfaces**
- Consume: `ObtenerMapaAsync`, `ObtenerCarteleraAsync` de la pieza 1; `ApartarAsync`, `PagarAsync`,
  `Canal`, `LineaTarifa` de la pieza 2; `TarifasDisponiblesAsync` de la pieza 3.
- Produce:
  - Aplicación `Cine.Web.Operacion` con ingreso por sesión y dos roles.
  - Tabla `Cuenta { Id, Usuario, HashContrasena, Rol, Activa }` con `Rol { Operador, Administradora }`,
    sembrada con una cuenta de cada rol.
  - `LimpiadorApartados`, servicio de fondo.
  - `Task<bool> DejaButacaAisladaAsync(int funcionId, IReadOnlyList<Butaca> seleccion)`.

**Evidencia**

---

### Pieza 5: Ingreso a la sala en la puerta

**Qué tiene que ser cierto**
- El operador localiza una compra por código de confirmación o por correo (RF-18).
- La pantalla muestra sala, función, butacas, la tarifa de cada boleto, la edad mínima de la
  película y si la compra ya ingresó (RF-19).
- Marcar una compra como ingresada funciona una sola vez; el segundo intento avisa que ya ingresó
  (RF-21, RN-38).
- El operador registra un ingreso denegado indicando si fue por clasificación o por carné, y esos
  ingresos no generan reembolso (RF-22, RN-34, RN-37).
- Cobrar la diferencia de un boleto de tarifa estudiante sin carné convierte ese boleto a tarifa
  general con el monto general vigente, marca la compra como ajustada y registra la diferencia
  cobrada (RF-20, RN-36).
- Cada ingreso queda atribuido a la cuenta que lo atendió (REG-5, RN-46).

**Con qué se comprueba**
- Buscar por el código `CV-XXXXX` y buscar por el correo de esa misma compra devuelven la misma
  compra con sus boletos.
- Prueba automatizada: sobre un boleto de tarifa estudiante, `CobrarDiferenciaAsync` deja el
  boleto con tarifa `General` y monto igual a la tarifa general vigente, la `Compra` con
  `Ajustada` en verdadero, y una fila en `Ingreso` con la diferencia cobrada (CA-8).
- Prueba automatizada: `RegistrarIngresoAsync` con resultado Admitido dos veces sobre el mismo
  código devuelve la segunda vez el motivo `CompraYaIngresada` y deja un solo ingreso admitido.
- Prueba automatizada: registrar un ingreso denegado por clasificación deja una fila en `Ingreso`
  con ese resultado, cero filas en `Reembolso` y la compra con `EstadoPago = Pagada`.
- En la aplicación de operación: buscar una compra con dos boletos de estudiante muestra el aviso
  de que exigen carné y la edad mínima de la película.

**Toca:** `Cine.Nucleo`, `Cine.Web.Operacion`, base de datos.

**Interfaces**
- Consume: `Compra`, `Boleto`, `MotivoRechazo` de la pieza 2; `Cuenta`, sesión y roles de la pieza
  4; `ConfiguracionTarifa` de la pieza 3.
- Produce:
  - `Task<Compra?> BuscarCompraAsync(string codigoOCorreo)`
  - `Task<ResultadoIngreso> RegistrarIngresoAsync(string codigo, int cuentaId, ResultadoIngresoTipo tipo)`
  - `Task<ResultadoCompra> CobrarDiferenciaAsync(int boletoId, int cuentaId)`
  - `ResultadoIngresoTipo { Admitido, DenegadoClasificacion, DenegadoCarne }`
  - `ResultadoIngreso { bool Exitoso, bool YaHabiaIngresado, MotivoRechazo? Motivo }`
  - Tabla `Ingreso { Id, CompraId, CuentaId, OcurrioEn, Resultado, DiferenciaCobrada }`.

**Evidencia**

---

### Pieza 6: La administradora programa la cartelera

**Qué tiene que ser cierto**
- La administradora registra películas con título, duración y clasificación por edad (RF-3, RN-32).
- La administradora programa funciones indicando película, sala, fecha y hora, para la semana en
  curso y la siguiente (RF-1, RN-5, RN-7).
- El sistema rechaza una función que se solape con otra de la misma sala o que no deje 15 minutos
  de limpieza entre el fin de una y el inicio de la siguiente (RF-2, RN-6).
- La administradora modifica o elimina funciones sin boletos vendidos, y solo esas; una función
  con ventas solo admite cancelación (RF-4, RN-8, RN-9).
- La administradora marca butacas de una sala como no vendibles, y el cambio solo afecta a las
  funciones creadas después (RF-5, RN-4).
- La administradora fija la tarifa general y la estudiante insertando una fila nueva de
  `ConfiguracionTarifa`, sin modificar las anteriores (RF-7, RN-11, RN-14).
- El operador recibe negativa en todas estas operaciones (RF-32, RN-47).

**Con qué se comprueba**
- Con una función de las 19:00 de una película de 100 minutos en la Sala 1: programar otra función
  en esa sala a las 20:54 se rechaza y a las 20:55 se acepta (RN-6).
- Programar una función que empiece durante otra de la misma sala se rechaza; la misma hora en la
  otra sala se acepta.
- Intentar mover de sala una función con 12 boletos vendidos se rechaza con un mensaje que indica
  los 12 boletos, y la función queda sin cambios (R-12, RN-8).
- Eliminar una función sin boletos vendidos la borra; intentar eliminar una con ventas se rechaza.
- Marcar C4 de la Sala 1 como no vendible: el `AforoVendible` de las funciones ya programadas no
  cambia, y una función creada después queda con un aforo una unidad menor y con C4 en
  `ButacaNoVendibleFuncion` (RN-4).
- Entrar con la cuenta de operador y abrir la pantalla de tarifas devuelve negativa por falta de
  permiso (CA-10).
- Fijar una tarifa general nueva agrega una fila a `ConfiguracionTarifa` y deja intacta la
  anterior; un boleto vendido antes conserva su monto.

**Toca:** `Cine.Nucleo`, `Cine.Web.Operacion`, base de datos.

**Interfaces**
- Consume: `Funcion`, `Pelicula`, `Sala`, `ButacaSala`, `ButacaNoVendibleFuncion`, `Butaca` de la
  pieza 1; `ConfiguracionTarifa` de la pieza 3; sesión, roles y `Cuenta` de la pieza 4.
- Produce:
  - `Task<ResultadoProgramacion> ProgramarFuncionAsync(int peliculaId, int salaId, DateTime inicioLocal)`
  - `Task<ResultadoOperacion> ModificarFuncionAsync(int funcionId, DateTime nuevoInicio, int nuevaSalaId)`
  - `Task<ResultadoOperacion> EliminarFuncionAsync(int funcionId)`
  - `Task<int> RegistrarPeliculaAsync(string titulo, int duracionMinutos, int clasificacionEdad)`
  - `Task<ResultadoOperacion> MarcarButacaNoVendibleAsync(int salaId, Butaca butaca, bool esVendible)`
  - `Task<ResultadoOperacion> FijarTarifasAsync(decimal montoGeneral, decimal montoEstudiante, int cuentaId)`
  - `ResultadoProgramacion { bool Exitoso, int? FuncionId, MotivoRechazo? Motivo, int BoletosVendidos }`
  - `MotivoRechazo` se extiende con `SolapeDeSala`, `LimpiezaInsuficiente`, `FuncionConVentas`,
    `FueraDeVentanaDeProgramacion`.

**Evidencia**

---

### Pieza 7: Cancelación de función con reembolso a todos

**Qué tiene que ser cierto**
- El operador o la administradora cancela una función indicando un motivo (RF-23, RN-39).
- La cancelación reembolsa en un solo acto todas las compras pagadas de la función, sin excluir
  ninguna, libera todas sus butacas y anula sus apartados vigentes (RF-24, RN-40, RN-41).
- Queda registrada la cancelación con quién la hizo, cuándo, el motivo, cuántas compras se
  reembolsaron y cuánto dinero (RF-25, REG-4).
- Una función cancelada no admite ninguna venta nueva por ningún canal y no vuelve a estar activa
  (RN-31, RN-43).
- Una función puede cancelarse aunque su hora de inicio ya haya pasado (RN-42).
- Antes de ejecutar, la pantalla confirma cuántas compras se van a reembolsar.

**Con qué se comprueba**
- Sembrar 38 compras pagadas en una función y cancelarla: las 38 quedan con
  `EstadoPago = Reembolsada`, hay 38 filas en `Reembolso`, `OcupacionButaca` de esa función queda
  en 0 filas, `Apartado` de esa función queda en 0 filas, y `CancelacionFuncion` registra 38
  compras y un monto igual a la suma de los boletos de esas compras (CA-6).
- Intentar apartar o pagar en esa función después de cancelarla devuelve Rechazado con motivo
  `FuncionCancelada`, en la aplicación pública y en la de taquilla.
- No existe operación que devuelva la función al estado Activa (RN-43).
- Cancelar una función cuyo inicio ya pasó se acepta y produce el mismo resultado (RN-42).
- La cancelación queda atribuida a la cuenta que la hizo, y el motivo se ve en el registro.

**Toca:** `Cine.Nucleo`, `Cine.Web.Operacion`, base de datos.

**Interfaces**
- Consume: `Compra`, `Boleto`, `Apartado`, `OcupacionButaca`, `MotivoRechazo` de la pieza 2;
  `Cuenta` y sesión de la pieza 4; `Funcion` de la pieza 1.
- Produce:
  - `Task<ResultadoCancelacion> CancelarFuncionAsync(int funcionId, int cuentaId, string motivo)`
  - `ResultadoCancelacion { bool Exitoso, int ComprasReembolsadas, decimal MontoDevuelto, MotivoRechazo? Motivo }`
  - Tablas `CancelacionFuncion { Id, FuncionId, CuentaId, Motivo, CreadaEn, ComprasReembolsadas, MontoDevuelto }`
    y `Reembolso { Id, CompraId, CancelacionId, Monto, CreadoEn }`.
  - `Funcion.Estado` con valores `Activa` y `Cancelada`.

**Evidencia**

---

### Pieza 8: Los cuatro reportes de la administradora

**Qué tiene que ser cierto**
- El reporte del distribuidor muestra, para un mes, cada función con fecha, hora, sala, película,
  boletos vendidos y dinero recaudado, subtotalizado por película, excluyendo los boletos de
  compras reembolsadas, y se exporta a un archivo de texto separado por comas (RF-26, RN-44).
- El reporte de ocupación muestra cada función como boletos vendidos sobre aforo vendible, y se
  agrupa por franja horaria y por día de la semana (RF-27, REG-3).
- El reporte por canal muestra boletos y dinero por canal y por día (RF-28, REG-2).
- El reporte de cancelaciones muestra las funciones canceladas de un período con su motivo, las
  compras reembolsadas y el dinero devuelto (RF-29, REG-4).
- Los cuatro son exclusivos de la administradora (RN-47).

**Con qué se comprueba**
- El subtotal por película del reporte del distribuidor iguala la suma de las funciones que lo
  componen, y el total del mes iguala la suma de los subtotales (CA-9).
- Cancelar una función del mes y volver a pedir el reporte: sus boletos ya no aparecen y el total
  baja exactamente en el monto de esas compras (RN-44).
- El archivo exportado abre en una hoja de cálculo con las mismas filas y montos que la pantalla.
- Una función de la Sala 2 con 12 boletos vendidos reporta ocupación de 12 sobre 60.
- Un día con compras de los dos canales reporta cada canal por separado, y la suma de los dos
  iguala los boletos vendidos de ese día.
- Un operador que pide cualquiera de los cuatro reportes recibe negativa.

**Toca:** `Cine.Nucleo`, `Cine.Web.Operacion`.

**Interfaces**
- Consume: `Boleto`, `Compra`, `Canal` de la pieza 2; `Funcion` y su `AforoVendible` de la pieza 1;
  `CancelacionFuncion` y `Reembolso` de la pieza 7; roles de la pieza 4.
- Produce:
  - `Task<ReporteDistribuidor> ReporteDistribuidorAsync(int anio, int mes)`
  - `Task<ReporteOcupacion> ReporteOcupacionAsync(DateOnly desde, DateOnly hasta)`
  - `Task<ReporteCanal> ReporteCanalAsync(DateOnly desde, DateOnly hasta)`
  - `Task<ReporteCancelaciones> ReporteCancelacionesAsync(DateOnly desde, DateOnly hasta)`
  - Exportación del reporte del distribuidor a texto separado por comas.

**Evidencia**

---

### Pieza 9: Correo de confirmación

**Qué tiene que ser cierto**
- Después de que una compra en línea quedó pagada, y solo si el comprador dejó correo, la
  aplicación pública pide el envío del código a un adaptador de correo, fuera de la transacción de
  la compra (RF-14).
- Cada intento de envío queda registrado con su resultado y, si falló, con el error (REG-6).
- Un fallo de envío no revierte ni invalida la compra: el código sigue en pantalla con el aviso de
  que el correo no salió (R-11).
- La compra sigue localizable en la puerta por código o por correo.

**Con qué se comprueba**
- Comprar dejando un correo válido deja una fila en `EnvioCorreo` con `Exitoso` en verdadero y el
  mensaje entregado al servidor de correo de prueba.
- Configurar el adaptador contra una dirección de servidor inválida y comprar: la `Compra` queda
  pagada, la pantalla muestra el código con el aviso de que el correo no salió, y `EnvioCorreo`
  queda con `Exitoso` en falso y el error registrado (R-11).
- La compra de ese caso se localiza en la aplicación de operación buscando por su correo.
- Comprar sin dejar correo no crea ninguna fila en `EnvioCorreo`.

**Toca:** `Cine.Web.Publica`, `Cine.Nucleo` (registro del envío), base de datos.

**Interfaces**
- Consume: `ResultadoCompra` y el código de confirmación de la pieza 2; `BuscarCompraAsync` de la
  pieza 5 para la comprobación en puerta.
- Produce:
  - `INotificadorCorreo` con `Task<ResultadoEnvio> EnviarCodigoAsync(int compraId, string correo, string codigo)`
  - `ResultadoEnvio { bool Exitoso, string? Error }`
  - Tabla `EnvioCorreo { Id, CompraId, IntentadoEn, Exitoso, Error }`.

**Evidencia**

---

## Cobertura

| Requisito o recorrido | Pieza |
|---|---|
| RF-1 programar funciones | 6 |
| RF-2 rechazo por solape y limpieza | 6 |
| RF-3 registrar películas | 6 |
| RF-4 modificar o eliminar funciones sin ventas | 6 |
| RF-5 marcar butacas no vendibles | 6 |
| RF-6 consultar cartelera sin identificarse | 1 |
| RF-7 fijar tarifas | 6 |
| RF-8 tarifas disponibles y monto por butaca | 3 |
| RF-9 mapa de butacas con su estado | 1 |
| RF-10 apartar y soltar butacas | 2 |
| RF-11 liberación de apartados vencidos | 2 (al leer y al apartar), 4 (tarea de fondo) |
| RF-12 rechazo con mapa actualizado | 2 |
| RF-13 pago, compra pagada y código | 2 |
| RF-14 correo del código y registro del envío | 9 |
| RF-15 venta en taquilla sobre el mismo mapa | 4 |
| RF-16 advertencia de butaca aislada | 4 |
| RF-17 declaración de edad mínima | 3 |
| RF-18 localizar compra por código o correo | 5 |
| RF-19 boletos que exigen carné y edad mínima | 5 |
| RF-20 cobro de la diferencia del boleto estudiante | 5 |
| RF-21 marcar ingresada y avisar si ya lo estaba | 5 |
| RF-22 registrar ingreso denegado | 5 |
| RF-23 cancelar función con motivo | 7 |
| RF-24 reembolso masivo y liberación de butacas | 7 |
| RF-25 registro de la cancelación | 7 |
| RF-26 reporte del distribuidor y su exportación | 8 |
| RF-27 reporte de ocupación | 8 |
| RF-28 reporte por canal y por día | 8 |
| RF-29 reporte de cancelaciones | 8 |
| RF-30 ingreso con usuario y contraseña | 4 |
| RF-31 atribución a la cuenta | 4 (ventas), 5 (ingresos y cobros), 7 (cancelaciones) |
| RF-32 negativa al operador en lo exclusivo | 6 |
| R-1 compra en línea que termina bien | 1, 2, 3, 9 |
| R-2 venta en taquilla | 4 |
| R-3 ingreso a la sala | 5 |
| R-4 el apartado vence sin pago | 2, 4 |
| R-5 otro comprador tomó la butaca primero | 2 |
| R-6 se intenta comprar cuando ya es tarde | 3 (en línea), 4 (taquilla) |
| R-7 cancelación con reembolso | 7 |
| R-8 estudiante sin carné en la puerta | 5 |
| R-9 menor de edad en función restringida | 5 |
| R-10 el pago se confirma dos veces | 2 |
| R-11 el correo de confirmación no llega | 9 |
| R-12 cambiar una función que ya tiene ventas | 6 |
| CA-1 dos solicitudes sobre la misma butaca | 2 |
| CA-2 apartado vencido a los 10 minutos | 2 |
| CA-3 función de miércoles | 3 |
| CA-4 boleto conserva su monto | 3 |
| CA-5 doble confirmación de pago | 2 |
| CA-6 cancelación de función con 38 compras | 7 |
| CA-7 ventana de venta a las 19:01, 19:19 y 19:21 | 3 (en línea), 4 (taquilla) |
| CA-8 diferencia cobrada al estudiante sin carné | 5 |
| CA-9 subtotales del reporte del distribuidor | 8 |
| CA-10 operador que intenta fijar tarifas | 6 |
| RNF-1 50 compradores sin doble venta | 2 |
| RNF-3 una compra pagada no se pierde | 2 |
| RNF-5 los apartados no sobreviven al reinicio | 2, 4 |
| RNF-6 el mapa se lee en un teléfono | 1 |

RNF-2 (700 boletos diarios y 56 funciones semanales) y RNF-4 (una hora de indisponibilidad
tolerable) son propiedades de tamaño y de operación que el diseño ya resolvió con la elección de
motor y de despliegue; ninguna pieza las construye ni las comprueba en el prototipo.

## Fuera del plan

- **Despliegue de las dos aplicaciones, sus nombres de host, el proxy inverso y su certificado.**
  `DISENO.md` los dejó abiertos para quien despliegue; el prototipo corre en la máquina de
  desarrollo sobre LocalDB.
- **Respaldo de la base de datos y archivado del historial.** Decisiones abiertas del diseño, que
  la administradora toma con quien implemente cuando el sistema entre en operación.
- **Proveedor real de correo.** La pieza 9 construye el adaptador y su registro; qué servicio se
  contrata queda para el despliegue, que es donde el diseño lo dejó.
- **Distribución de `Cine.Nucleo` como paquete NuGet interno.** El diseño acepta la referencia de
  proyecto mientras las dos aplicaciones se desplieguen juntas, y eso es lo que el prototipo hace.
- **Aspecto visual acabado de las dos aplicaciones.** La única exigencia que el plan comprueba es
  RNF-6: el mapa se lee en un teléfono sin ampliar.
