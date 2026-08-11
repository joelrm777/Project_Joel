# Especificación: Venta de butacas del Cine Variedades

## Resumen

Sistema de venta de butacas numeradas para un cine de dos salas. Sustituye el cuaderno y el
mapa a lápiz de la taquilla, y agrega venta por internet donde el comprador elige su butaca
desde el teléfono, con el objetivo de descongestionar la fila de viernes y sábado.

## Glosario

Un solo término por concepto. Donde la conversación usó dos, se deja constancia del descartado.

| Término | Definición |
|---|---|
| **Cine** | El Cine Variedades. Hay uno solo en el sistema. |
| **Sala** | Auditorio físico. Existen dos: Sala 1 con 120 butacas y Sala 2 con 60. |
| **Butaca** | Puesto individual identificado por fila y número dentro de una sala (ej. C4). Se descarta «asiento». |
| **Butaca no vendible** | Butaca de una sala que nunca se vende (accesibilidad, vista tapada). No cuenta en el aforo vendible. |
| **Aforo vendible** | Cantidad de butacas de una sala que sí se pueden vender: total menos butacas no vendibles. |
| **Película** | Título exhibido, con su duración y su clasificación. |
| **Clasificación** | Edad mínima para ingresar a una película. Cero significa sin restricción. |
| **Función** | Exhibición de una película en una sala a una fecha y hora concretas. Se descarta «proyección». |
| **Cartelera** | Conjunto de funciones programadas para una semana, de jueves a miércoles. |
| **Tarifa** | Regla de precio aplicable a una butaca. Existen tres: general, miércoles y estudiante. |
| **Apartado** | Butaca de una función retenida temporalmente para un comprador que aún no pagó. Se descarta «reserva». |
| **Compra** | Operación por la que uno o más boletos de una misma función quedan pagados. Se descarta «orden» y «venta». |
| **Boleto** | Una butaca de una función vendida dentro de una compra, con la tarifa y el monto que se le cobró. |
| **Código de confirmación** | Identificador corto que se le entrega al comprador y con el que se localiza su compra. |
| **Canal** | Origen de la compra: en línea o taquilla. |
| **Comprador** | Persona que compra, por cualquiera de los dos canales. No tiene cuenta en el sistema. |
| **Operador** | Persona que atiende taquilla y puerta. Tiene cuenta en el sistema. |
| **Administradora** | La dueña. Tiene cuenta y además programa cartelera, fija tarifas y consulta reportes. |
| **Ingreso** | Acto de admitir a la sala a quien presenta una compra, verificando lo que haga falta. |
| **Cancelación de función** | Acto de dejar una función sin efecto, que obliga a reembolsar todas sus compras. |
| **Reembolso** | Devolución del dinero de una compra. Queda registrado; el movimiento de dinero real ocurre fuera del sistema. |

## Objetivos

- Que el comprador elija su butaca concreta desde el teléfono y quede pagada.
- Que exista un único mapa de butacas por función, compartido entre internet y taquilla, de modo
  que ninguna butaca se venda dos veces.
- Que la taquilla registre sus ventas en el sistema y el cuaderno deje de usarse.
- Que la administradora pueda cancelar una función y reembolsar a todos sus compradores en un
  solo acto.
- Que la administradora obtenga sin trabajo manual el reporte mensual que le pide el distribuidor.
- Que la administradora pueda consultar después qué tan llenas fueron sus funciones, cuánto se
  vendió por cada canal y qué le costaron las cancelaciones.

## Fuera de alcance

Cada punto es una decisión adoptada, no un supuesto.

- **Más de un cine.** El sistema conoce un cine y sus dos salas.
- **Más de una semana de cartelera a futuro.** Se programa la semana en curso y la siguiente.
  No hay cartelera histórica editable.
- **Cobro real.** El pago se simula: el sistema registra que la compra quedó pagada. No hay
  conexión a ningún medio de pago, ni se guarda ningún dato de tarjeta.
- **Devolución real de dinero.** El reembolso se registra como hecho; quien mueve la plata es la
  administradora fuera del sistema.
- **Boleto impreso, código de barras o código QR.** La compra se localiza por código de
  confirmación o por correo, leído por el operador.
- **Cuenta de usuario para el comprador.** Solo operador y administradora tienen cuenta. La
  consecuencia aceptada es que el comprador no tiene historial ni puede operar sobre su compra
  sin pasar por taquilla.
- **Cancelación o cambio de compra a pedido del comprador.** La compra es firme (RN-24).
- **Venta de confitería, abonos, promociones por volumen y tarjetas de regalo.**
- **Notificación por mensajería o teléfono.** El único aviso automático es un correo.
- **Asignación automática de butacas.** El comprador siempre elige.
- **Control de acceso físico.** El sistema dice quién compró qué; la puerta la controla una persona.

## Reglas del negocio

Cada regla se puede comprobar como cierta o falsa sobre los datos del sistema.

### Salas y butacas

1. **RN-1:** La Sala 1 tiene 120 butacas y la Sala 2 tiene 60. La cantidad y la disposición de
   butacas de una sala no cambian por función.
2. **RN-2:** Una butaca no vendible en una función no puede aparecer en ninguna compra de esa
   función ni ser apartada, en ningún canal.
3. **RN-3:** El aforo vendible de una función es el total de butacas de su sala menos las no
   vendibles, y queda fijado cuando la función se crea.
4. **RN-4:** Marcar o desmarcar una butaca como no vendible solo afecta a las funciones creadas
   después del cambio. Ninguna función ya programada altera su aforo vendible.

### Cartelera y funciones

5. **RN-5:** Una función pertenece a exactamente una sala, una película, una fecha y una hora de
   inicio.
6. **RN-6:** Dos funciones de la misma sala no pueden solaparse. Entre el fin de una función
   (inicio más duración de la película) y el inicio de la siguiente deben mediar al menos 15
   minutos de limpieza.
7. **RN-7:** Una semana de cartelera empieza el jueves y termina el miércoles siguiente.
8. **RN-8:** Una función con al menos un boleto vendido no puede modificarse en su película, su
   sala, su fecha ni su hora. La única operación posible sobre ella es la cancelación (RN-39).
9. **RN-9:** Una función sin boletos vendidos puede modificarse o eliminarse libremente.
10. **RN-10:** Solo la administradora crea, modifica y elimina funciones. Quién puede cancelarlas
    lo fija RN-39.

### Tarifas y precios

11. **RN-11:** La tarifa general es un monto único del cine, fijado por la administradora.
12. **RN-12:** En las funciones cuya fecha de inicio cae en miércoles, toda butaca se vende a la
    mitad de la tarifa general, en ambos canales, sin excepción.
13. **RN-13:** En las funciones cuya fecha de inicio cae en miércoles, la tarifa estudiante no
    se ofrece. Los miércoles no se verifica ningún carné.
14. **RN-14:** La tarifa estudiante es un monto fijado por la administradora, menor que la tarifa
    general, y solo está disponible en funciones que no inician miércoles.
15. **RN-15:** Nunca se aplica más de una tarifa a la misma butaca.
16. **RN-16:** El monto cobrado y la tarifa aplicada quedan grabados en cada boleto en el momento
    de la compra. Un cambio posterior de tarifas no altera boletos ya vendidos.

### Compra y apartado

17. **RN-17:** Una butaca de una función está en exactamente uno de estos estados: libre,
    apartada o vendida.
18. **RN-18:** Elegir una butaca libre la deja apartada para quien la eligió durante 10 minutos.
    Agregar otra butaca al mismo intento de compra renueva ese plazo a 10 minutos para todas las
    butacas de ese intento.
19. **RN-19:** Una butaca apartada no puede ser apartada ni vendida por nadie más mientras el
    apartado esté vigente, ni en línea ni en taquilla.
20. **RN-20:** Si el apartado vence sin que la compra quede pagada, la butaca vuelve a estar
    libre y el apartado deja de existir.
21. **RN-21:** Una compra contiene entre 1 y 10 boletos, todos de la misma función.
22. **RN-22:** Una compra solo puede quedar pagada si todas sus butacas están apartadas para ella
    y ninguno de esos apartados venció.
23. **RN-23:** Confirmar el pago dos veces sobre la misma compra produce una sola compra pagada,
    con un solo código de confirmación y un solo cobro.
24. **RN-24:** Una compra pagada no puede ser cancelada, modificada ni reembolsada a pedido del
    comprador. La única vía de reembolso es la cancelación de la función (RN-40).
25. **RN-25:** Cada compra pagada tiene un código de confirmación único e irrepetible.
26. **RN-26:** En taquilla, si la compra deja una butaca libre aislada entre dos ocupadas de la
    misma fila, el sistema lo advierte al operador pero no impide la venta.
27. **RN-27:** Una compra está en uno de dos estados de pago: pagada o reembolsada, y pasa de
    pagada a reembolsada una sola vez y solo por cancelación de su función. Además lleva dos
    marcas independientes del estado de pago: *ajustada* (se cobró alguna diferencia en puerta) e
    *ingresada* (ya se admitió a la sala).

### Ventana de venta

28. **RN-28:** En línea se puede comprar hasta el instante en que la función inicia.
29. **RN-29:** En taquilla se puede comprar hasta 20 minutos después de que la función inició.
30. **RN-30:** Pasados esos 20 minutos la función queda cerrada y no admite ninguna venta nueva
    por ningún canal.
31. **RN-31:** Una función cancelada no admite ninguna venta nueva, sin importar la hora.

### Clasificación y verificación en puerta

32. **RN-32:** Toda película tiene una clasificación por edad. Cero significa sin restricción.
33. **RN-33:** En línea, antes de pagar una función con clasificación mayor que cero, se le
    muestra al comprador la edad mínima y él declara que la cumple. La compra no se completa sin
    esa declaración.
34. **RN-34:** En puerta, el operador no admite a quien no cumpla la edad mínima de la película.
    Ese ingreso queda registrado como denegado por clasificación y no genera reembolso.
35. **RN-35:** Un boleto con tarifa estudiante exige que el operador vea el carné al ingresar.
36. **RN-36:** Si quien presenta un boleto de tarifa estudiante no muestra carné, el operador le
    cobra en taquilla la diferencia hasta la tarifa general. Al cobrarla, el boleto pasa a tarifa
    general y la compra queda marcada como ajustada.
37. **RN-37:** Si no paga la diferencia, no ingresa. Ese ingreso queda registrado como denegado
    por carné y no genera reembolso.
38. **RN-38:** Una compra se marca como ingresada una sola vez. Un segundo intento de ingreso con
    el mismo código le avisa al operador que esa compra ya ingresó.

### Cancelación y reembolso

39. **RN-39:** Solo la administradora y el operador pueden cancelar una función, y deben indicar
    un motivo.
40. **RN-40:** Cancelar una función reembolsa todas sus compras pagadas en un solo acto, sin
    excluir ninguna, y sin que el comprador tenga que reclamar.
41. **RN-41:** Cancelar una función libera todas sus butacas y anula los apartados vigentes.
42. **RN-42:** Una función puede cancelarse aunque ya haya iniciado.
43. **RN-43:** Una función cancelada no puede volver a estar activa. Si hay que reprogramarla, se
    crea una función nueva.
44. **RN-44:** Los boletos de compras reembolsadas no cuentan como vendidos en ningún reporte.

### Cuentas

45. **RN-45:** Existen exactamente dos roles con cuenta: operador y administradora. La
    administradora puede todo lo que puede el operador.
46. **RN-46:** Vender en taquilla, verificar ingresos, cobrar diferencias y cancelar funciones
    exige haber entrado con una cuenta. Todas esas operaciones quedan atribuidas a la cuenta que
    las hizo.
47. **RN-47:** Programar cartelera, fijar tarifas, marcar butacas no vendibles y consultar
    reportes es exclusivo de la administradora.

## Qué queda registrado

Lo que se guarda de cada operación, y qué se podrá contestar gracias a eso.

1. **REG-1:** De cada boleto: función, butaca, tarifa aplicada, monto cobrado y canal.
   → Permite contestar cuántos boletos y cuánto dinero por función, por película, por horario y
   por canal.
2. **REG-2:** De cada compra: código de confirmación, función, canal, fecha y hora en que quedó
   pagada, correo del comprador cuando lo hay, cuenta que la registró cuando fue en taquilla, su
   estado de pago y sus marcas de ajustada e ingresada (RN-27).
   → Permite contestar si la venta en línea descongestionó la fila del viernes, comparando
   boletos por canal por día y por franja horaria.
3. **REG-3:** De cada función: aforo vendible al momento de programarla y boletos vendidos.
   → Permite contestar qué porcentaje de la sala se llenó en cada función, y por lo tanto qué
   días y qué horarios rinden.
4. **REG-4:** De cada cancelación de función: quién la hizo, cuándo, el motivo, cuántas compras
   se reembolsaron y cuánto dinero.
   → Permite contestar cuántas funciones se cayeron en el año, por qué, y cuánto costaron.
5. **REG-5:** De cada ingreso: compra, cuenta que lo atendió, fecha y hora, y resultado
   (admitido, denegado por clasificación, denegado por carné, diferencia cobrada).
   → Permite contestar cuánta gente llegó sin carné y cuánto se cobró de más en puerta.
6. **REG-6:** De cada correo de confirmación: si se pudo enviar o no.
   → Permite contestar si el aviso al comprador está funcionando.

Lo que **no** se guarda, por decisión:

- Apartados vencidos o abandonados. No interesan y no sobreviven a un reinicio del sistema.
- Nombre, teléfono, cédula, institución o número de carné del comprador. El correo es el único
  dato personal, y solo cuando la compra fue en línea.
- Cualquier dato de medio de pago.
- Qué butacas miró el comprador antes de decidirse.

## Salidas que consume alguien más

| Quién | Qué recibe | Formato | Frecuencia |
|---|---|---|---|
| Distribuidor de películas | Detalle por función del mes: fecha, hora, sala, película, boletos vendidos y dinero recaudado, con subtotal por película | Tabla en pantalla, exportable a un archivo de texto separado por comas | Mensual |
| Administradora | Ocupación por función y por franja horaria | Tabla en pantalla | Cuando la consulta |
| Administradora | Boletos y dinero por canal, por día | Tabla en pantalla | Cuando la consulta |
| Administradora | Funciones canceladas con motivo, compras reembolsadas y dinero devuelto | Tabla en pantalla | Cuando la consulta |

## Recorridos

### R-1 — Compra en línea que termina bien

1. El comprador ve la cartelera de la semana y elige una función que no esté cerrada ni cancelada.
2. El sistema le muestra el mapa de la función con cada butaca libre, apartada, vendida o no
   vendible.
3. El comprador toca entre 1 y 10 butacas libres. Cada toque las aparta por 10 minutos.
4. Elige una tarifa por butaca entre las disponibles para esa función.
5. Si la película tiene clasificación mayor que cero, declara que cumple la edad mínima.
6. El sistema le muestra el total y él confirma el pago.
7. La compra queda pagada, las butacas quedan vendidas, se genera el código de confirmación y se
   le muestra en pantalla.
8. Si dejó un correo, se le envía el código.

### R-2 — Venta en taquilla

1. El operador entra con su cuenta y elige la función.
2. Ve el mismo mapa que ve internet, con el mismo estado en ese instante.
3. Marca las butacas, elige tarifas y confirma el pago recibido.
4. Si la selección deja una butaca aislada, el sistema lo advierte y él decide si continúa.
5. La compra queda pagada y el operador le dicta el código al comprador.

### R-3 — Ingreso a la sala

1. El operador busca la compra por código de confirmación, o por correo si el comprador lo perdió.
2. El sistema le muestra sala, función, butacas, tarifa de cada boleto y si la compra ya ingresó.
3. Si hay boletos de tarifa estudiante, pide carné.
4. Si la película tiene edad mínima, la verifica.
5. Marca la compra como ingresada y deja pasar.

### R-4 — El apartado vence sin pago

1. El comprador aparta butacas y abandona la compra, o su teléfono se queda sin conexión.
2. Al cumplirse los 10 minutos las butacas vuelven a estar libres.
3. No queda compra registrada. Si el comprador vuelve, empieza de nuevo, y las butacas pueden
   estar tomadas.

### R-5 — Otro comprador tomó la butaca primero

1. Dos compradores tienen abierto el mapa de la misma función.
2. El primero toca C4 y queda apartada para él.
3. El segundo toca C4. El sistema le informa que ya no está disponible y le refresca el mapa.
4. El segundo elige otra butaca.

### R-6 — Se intenta comprar cuando ya es tarde

1. El comprador abre el mapa a las 18:58 de una función de las 19:00 y se demora.
2. Intenta pagar a las 19:01. El sistema rechaza la compra porque la venta en línea cerró, y
   libera sus apartados.
3. Se le indica que puede comprar en taquilla hasta las 19:20.
4. A las 19:21 la función queda cerrada y ni la taquilla puede venderla.

### R-7 — Falla el proyector: cancelación con reembolso

1. La función está en curso, la sala sentada, y el proyector se apaga.
2. El operador o la administradora cancela la función indicando el motivo.
3. El sistema reembolsa todas las compras pagadas de esa función, libera las butacas y anula los
   apartados vigentes.
4. Queda registrado quién canceló, cuándo, el motivo, cuántas compras y cuánto dinero.
5. Los boletos reembolsados dejan de contar en el reporte al distribuidor.
6. Si hay que reprogramar la película, la administradora crea una función nueva.

### R-8 — Estudiante sin carné en la puerta

1. El comprador presenta una compra con dos boletos de tarifa estudiante y no trae carné.
2. El operador le cobra la diferencia hasta la tarifa general por cada boleto.
3. Los boletos pasan a tarifa general, la compra queda marcada como ajustada y él ingresa.
4. Si no paga la diferencia, no ingresa, el ingreso queda registrado como denegado por carné y no
   hay reembolso.

### R-9 — Menor de edad en una función restringida

1. El comprador declaró en línea que cumplía la edad mínima, pero en puerta se ve que no.
2. El operador no lo admite.
3. El ingreso queda registrado como denegado por clasificación. No hay reembolso.

### R-10 — El pago se confirma dos veces

1. El comprador toca «pagar» y, al no ver respuesta, lo toca otra vez.
2. El sistema reconoce que es la misma compra: queda una sola compra pagada, un solo código y un
   solo cobro.

### R-11 — El correo de confirmación no llega

1. La compra queda pagada y el código se muestra en pantalla.
2. El envío del correo falla y queda registrado como fallido.
3. La compra sigue siendo válida: en la puerta el operador la localiza por correo o por código.

### R-12 — Se intenta cambiar una función que ya tiene ventas

1. La administradora quiere mover a otra sala una función que ya vendió 12 boletos.
2. El sistema rechaza el cambio.
3. Si de todos modos hay que sacarla, la cancela con motivo —lo que reembolsa a los 12— y crea
   una función nueva.

## Requisitos funcionales

### Cartelera

1. **RF-1:** La administradora programa funciones indicando película, sala, fecha y hora, para la
   semana en curso y la siguiente.
2. **RF-2:** El sistema rechaza una función que se solape con otra de la misma sala, o que no deje
   15 minutos de limpieza (RN-6).
3. **RF-3:** La administradora registra películas con título, duración y clasificación.
4. **RF-4:** La administradora modifica o elimina funciones sin boletos vendidos, y solo esas.
5. **RF-5:** La administradora marca butacas de una sala como no vendibles.
6. **RF-6:** Cualquiera puede consultar la cartelera de la semana sin identificarse.

### Tarifas

7. **RF-7:** La administradora fija la tarifa general y la tarifa estudiante.
8. **RF-8:** El sistema determina las tarifas disponibles para una función según su fecha
   (RN-12, RN-13, RN-14) y calcula el monto de cada butaca.

### Venta

9. **RF-9:** El sistema muestra el mapa de butacas de una función con el estado de cada una.
10. **RF-10:** El comprador aparta butacas libres y el sistema las retiene 10 minutos. También
    puede soltar una butaca que apartó antes de pagar, y esa butaca vuelve a estar libre.
11. **RF-11:** El sistema libera automáticamente los apartados vencidos.
12. **RF-12:** El sistema rechaza apartar o vender una butaca que no esté libre, y le devuelve al
    solicitante el estado actualizado del mapa.
13. **RF-13:** El comprador confirma el pago y el sistema registra la compra como pagada, genera
    su código de confirmación y lo muestra.
14. **RF-14:** El sistema envía el código por correo cuando el comprador dejó uno, y registra si
    el envío falló.
15. **RF-15:** El operador registra ventas de taquilla sobre el mismo mapa, con las mismas reglas
    de tarifa y de ventana de venta que la venta en línea, salvo el margen de 20 minutos.
16. **RF-16:** El sistema advierte al operador cuando su selección deja una butaca aislada, sin
    bloquear la venta.
17. **RF-17:** El sistema exige la declaración de edad mínima antes de completar una compra en
    línea de una función con clasificación mayor que cero.

### Puerta

18. **RF-18:** El operador localiza una compra por código de confirmación o por correo.
19. **RF-19:** El sistema le muestra al operador qué boletos exigen carné y qué edad mínima
    aplica.
20. **RF-20:** El operador cobra la diferencia de un boleto de tarifa estudiante sin carné, y el
    sistema reconvierte ese boleto a tarifa general y marca la compra como ajustada (RN-36).
21. **RF-21:** El operador marca una compra como ingresada, y el sistema avisa si ya lo estaba.
22. **RF-22:** El operador registra un ingreso denegado indicando si fue por clasificación o por
    carné.

### Cancelación

23. **RF-23:** El operador o la administradora cancela una función indicando motivo.
24. **RF-24:** La cancelación reembolsa todas las compras pagadas de la función en un solo acto,
    libera las butacas y anula los apartados vigentes.
25. **RF-25:** El sistema registra la cancelación con su autor, su motivo, la cantidad de compras
    reembolsadas y el monto devuelto.

### Reportes

26. **RF-26:** La administradora obtiene, para un mes, el detalle por función con boletos y dinero,
    subtotalizado por película, excluyendo boletos reembolsados, y lo puede exportar a un archivo
    de texto separado por comas.
27. **RF-27:** La administradora consulta la ocupación de cada función como boletos vendidos sobre
    aforo vendible, agrupable por franja horaria y por día de la semana.
28. **RF-28:** La administradora consulta boletos y dinero por canal y por día.
29. **RF-29:** La administradora consulta las funciones canceladas de un período con su motivo,
    compras reembolsadas y dinero devuelto.

### Cuentas

30. **RF-30:** Operador y administradora entran al sistema con usuario y contraseña.
31. **RF-31:** El sistema atribuye a una cuenta cada venta de taquilla, ingreso, cobro de
    diferencia y cancelación.
32. **RF-32:** El sistema niega al operador las operaciones exclusivas de la administradora
    (RN-47).

## Requisitos no funcionales

1. **RNF-1:** El sistema soporta hasta 50 compradores mirando y apartando butacas al mismo tiempo
   sin vender la misma butaca dos veces.
2. **RNF-2:** El sistema soporta hasta 700 boletos vendidos por día y hasta 56 funciones por
   semana.
3. **RNF-3:** Una compra ya pagada no se puede perder. Es el único dato cuya pérdida es
   inaceptable.
4. **RNF-4:** Una hora de indisponibilidad es tolerable: la taquilla puede vender a mano y
   registrar después. No se requiere redundancia ni respaldo en caliente.
5. **RNF-5:** Los apartados no necesitan sobrevivir a un reinicio del sistema. Si se pierden, sus
   butacas quedan libres.
6. **RNF-6:** El mapa de butacas se muestra en pantalla de teléfono sin necesidad de ampliar para
   distinguir una butaca de otra.

## Criterios de aceptación

Solo para los requisitos cuya comprobación no se deduce de su enunciado.

| ID | Criterio | Requisito asociado |
|---|---|---|
| CA-1 | Dos solicitudes simultáneas sobre la misma butaca libre: una la aparta, la otra recibe rechazo con el mapa actualizado. Nunca las dos. | RF-12, RNF-1 |
| CA-2 | Con un apartado hecho a las 10:00 y sin pago, a las 10:10:01 la butaca aparece libre para otro comprador. | RF-11, RN-20 |
| CA-3 | Una función del miércoles no ofrece tarifa estudiante y toda butaca se cobra a la mitad de la general. | RN-12, RN-13 |
| CA-4 | Cambiada la tarifa general, un boleto vendido antes conserva su monto original. | RN-16 |
| CA-5 | Dos confirmaciones de pago sobre la misma compra dejan una sola compra y un solo código. | RN-23, RF-13 |
| CA-6 | Cancelada una función con 38 compras pagadas, las 38 quedan reembolsadas, las butacas libres, y el reporte del mes no cuenta esos boletos. | RF-24, RN-44 |
| CA-7 | Con la función iniciada a las 19:00: en línea se rechaza a las 19:01; en taquilla se acepta a las 19:19 y se rechaza a las 19:21. | RN-28, RN-29, RN-30 |
| CA-8 | Cobrada la diferencia de un boleto estudiante, ese boleto reporta tarifa general y su monto sube, y la compra figura como ajustada. | RF-20, RN-36 |
| CA-9 | El reporte mensual del distribuidor suma, por película, los mismos boletos y montos que el detalle por función que lo compone. | RF-26 |
| CA-10 | Un operador que intenta fijar tarifas recibe negativa. | RF-32, RN-47 |

## Preguntas abiertas

Ninguna. Todo lo que la entrevista dejó sin definir quedó resuelto en este documento: como regla
del negocio, como restricción de alcance explícita, o como requisito. Lo que quedó abierto son
decisiones de la solución, no del negocio, y viven en `DISENO.md`.

## Dependencias

- Un servicio de correo para enviar el código de confirmación. Su indisponibilidad no invalida
  ninguna compra (R-11).

## Referencias

- `Consigna Caso Practico 3 - SINT-732.docx` — contexto, frases de la dueña y alcance impuesto.
- `escribir-diseno-SKILL.txt` — habilidad que fija el proceso y la estructura de este documento.
- `PROMPT.md` — encargo inicial.
