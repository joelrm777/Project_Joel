# Especificación: Boleta de Kilometraje Digital

## Resumen
Sistema que reemplaza el trámite en papel con el que un colaborador cobra el kilometraje
recorrido entre tiendas de la compañía: hoy se llena a mano en Excel, se imprime, se firma y
se lleva físicamente a jefatura y luego a finanzas.

## Glosario

| Término | Definición |
|---|---|
| Boleta | Unidad de trámite que un colaborador envía para cobrar el kilometraje de uno o más viajes, todos con el mismo vehículo declarado. |
| Viaje | Recorrido puntual hecho por el colaborador en una fecha específica, compuesto por la secuencia de tiendas que visitó. Se llama «recorrido» en el enunciado original del proyecto; acá se usa «viaje» para distinguirlo de la boleta completa, que puede agrupar varios. |
| Tramo | Distancia registrada entre dos tiendas consecutivas dentro de un viaje. |
| Tienda | Cada uno de los 40 puntos de la compañía entre los que puede darse un viaje. |
| Colaborador | Empleado que registra viajes y cobra kilometraje. Una jefatura, el administrador o finanzas también son colaboradores para su propio kilometraje, además de su rol principal. |
| Jefatura | Responsable de aprobar o rechazar las boletas de los colaboradores a su cargo; también puede armar y cobrar sus propias boletas, que aprueba otra jefatura designada (nunca puede aprobarse a sí misma). |
| Administrador | Mantiene la tabla de tarifas y la tabla de distancias entre tiendas; también puede armar y cobrar sus propias boletas. |
| Finanzas | Recibe las boletas ya aprobadas, con el monto a cancelar; también puede armar y cobrar sus propias boletas. |
| ERP de RH | Sistema externo (simulado para la demo) que provee los datos del colaborador a partir de su cédula: jefatura, departamento, puesto, nombre y correo. |
| AD | Directorio corporativo (simulado para la demo) que provee el correo institucional y sirve de inicio de sesión único (SSO) para el sistema. |
| Tabla de tarifas | Tabla vigente que define el costo por kilómetro según tipo de transporte, antigüedad, cilindraje y combustible. «Tipo de transporte» y «tipo de vehículo» se usan como sinónimos en el enunciado original; acá se usa siempre «tipo de transporte». |
| Tabla de distancias | Tabla que registra la distancia de cada tramo entre tiendas. |
| Antigüedad | Año actual menos año del modelo del vehículo declarado. |

## Objetivos
- Reemplazar el trámite en papel por un flujo digital de extremo a extremo, desde que el
  colaborador ingresa su cédula hasta que finanzas recibe la boleta aprobada.
- Reducir el tiempo de trámite frente a la ~1 semana actual en papel, sobre un volumen de
  ~500 boletas mensuales.
- Calcular el monto a pagar de forma automática y consistente, sin que nadie pueda ingresar
  o sobrescribir a mano la tarifa ni la distancia.
- Dejar registro suficiente para auditar, boleta por boleta y hasta un año después, que el
  monto se calculó con la tarifa y la distancia correctas, y que hubo aprobación real de
  jefatura.

## Fuera de alcance
- Imprimir o firmar cualquier documento en papel.
- Ejecutar el pago en sí (la transferencia o el desembolso).
- Una segunda aprobación por parte de finanzas.
- Diseñar para alta disponibilidad o para picos de concurrencia: el volumen (~500
  boletas/mes, ~1.000 colaboradores, ~60 jefaturas) es bajo y parejo.
- La integración real con el ERP de RH y el AD de la compañía: para el curso quedan
  simulados; la integración real es una decisión de adopción futura, fuera de este proyecto.

## Reglas del negocio

1. RN-1: El costo por kilómetro sale siempre de la tabla vigente de tarifas, cruzando tipo
   de transporte, antigüedad, cilindraje y combustible del vehículo declarado; nunca se
   ingresa ni se sobrescribe a mano.
2. RN-2: La antigüedad se calcula como año actual − año del modelo. Si supera el año más
   viejo que tiene definido la tabla vigente, se le aplica la tarifa de ese último tramo de
   antigüedad.
3. RN-3: La tracción del vehículo (sencilla, doble tracción, no aplica) es un dato
   informativo; no participa del cálculo del costo por kilómetro.
4. RN-4: Cada boleta declara un único vehículo; todos los viajes que agrupa esa boleta usan
   ese mismo vehículo y su tarifa.
5. RN-5: Si falta la distancia registrada entre dos tiendas consecutivas de cualquier viaje
   de la boleta, la boleta completa se bloquea —no se puede completar ni enviar— y se
   notifica al administrador y a finanzas para que carguen el dato pendiente.
6. RN-6: Una boleta solo puede incluir viajes cuya fecha caiga en el mes en curso o en los
   dos meses anteriores; un viaje más viejo que eso no se puede incluir.
7. RN-7: No se puede registrar un viaje (misma fecha y misma secuencia de tiendas) que ya
   exista en otra boleta del mismo colaborador o en la misma boleta.
8. RN-8: Mientras la boleta esté pendiente de decisión de la jefatura, el colaborador puede
   editarla o retirarla.
9. RN-9: La jefatura aprueba o rechaza la boleta completa —todo o nada—; no hay aprobación
   parcial por viaje.
10. RN-10: Una boleta no puede llegar a finanzas sin que la jefatura correspondiente al
    colaborador la haya aprobado explícitamente.
11. RN-11: Al rechazar una boleta, la jefatura debe registrar un motivo; ese motivo se
    comunica al colaborador.
12. RN-12: Una boleta rechazada vuelve al colaborador para que la corrija; no se reenvía a
    finanzas hasta que sea corregida y vuelta a aprobar.
13. RN-13: Una boleta rechazada que no se corrige ni se reenvía en 7 días se descarta
    automáticamente.
14. RN-14: Si la jefatura no decide en el plazo configurado (por defecto 2 días hábiles)
    desde el envío, se manda un recordatorio automático; si sigue sin decidir, el
    recordatorio se repite cada ese mismo plazo hasta que decida.
15. RN-15: Una boleta ya aprobada y enviada a finanzas no se puede deshacer ni anular por
    nadie dentro del sistema.
16. RN-16: Si el administrador cambia la tabla de tarifas mientras hay boletas ya enviadas y
    pendientes, esas boletas mantienen la tarifa con la que se calcularon; la tarifa nueva
    solo aplica a boletas creadas después del cambio.
17. RN-17: Una cédula que el ERP de RH no reconoce (inexistente o inactiva) se rechaza de
    entrada, con mensaje claro, sin continuar el trámite.
18. RN-18: Una jefatura, el administrador o finanzas pueden armar y cobrar boletas propias,
    igual que cualquier colaborador (les aplican RN-1 a RN-17 sin excepción), pero nunca
    pueden aprobar su propia boleta; en ese caso la aprueba otra jefatura designada para ese
    fin (RN-10 sigue exigiendo aprobación de la jefatura correspondiente, que nunca coincide
    con quien la envió).

## Qué queda registrado

1. REG-1: De cada boleta, mientras existe en su forma completa (ver retención): colaborador
   identificado, jefatura, vehículo declarado, cada viaje con su fecha y su secuencia de
   tiendas, cada tramo con su distancia, monto total, tarifa aplicada, estado (pendiente,
   aprobada, rechazada o descartada), fecha de envío y fecha de decisión. Permite
   reconstruir cómo se armó el monto y medir el tiempo de trámite.
2. REG-2: La boleta completa —con el detalle de tramos y la identificación del
   colaborador— persiste 7 días desde que llega a finanzas aprobada. Pasado ese plazo, se
   conserva un resumen por boleta (colaborador, jefatura, monto, tarifa aplicada, distancia
   total, fechas de envío y de decisión, estado) durante una ventana móvil de 12 meses, sin
   el detalle de cada tramo. Ese resumen permite auditar boleta por boleta, y construir
   reportes de monto, volumen y tiempo promedio de trámite por jefatura y por periodo, hasta
   un año después de ocurrida.

## Salidas que consume alguien más

| Quién | Qué recibe | Formato | Frecuencia |
|---|---|---|---|
| Jefatura | Aviso de que tiene una boleta pendiente de su aprobación | Correo (solo aviso; la decisión se toma dentro del sistema) | Al enviarse la boleta, y recordatorios cada 2 días (parametrizable) mientras siga pendiente |
| Colaborador | Aviso de rechazo con el motivo, o de boleta bloqueada por falta de distancia | Correo | Por boleta, al ocurrir el evento |
| Finanzas | Boleta aprobada con el monto a cancelar | Entrada en el sistema | Por boleta, en tiempo real al aprobarse |
| Administrador y Finanzas | Aviso de boleta bloqueada por falta de distancia entre tiendas | Correo | Por boleta, al bloquearse |

## Recorridos

**Recorrido principal (termina bien):**
1. El colaborador inicia sesión (SSO contra el AD); el sistema toma la cédula asociada a esa
   sesión, la valida contra el ERP de RH (RN-17) y trae jefatura, departamento, puesto,
   nombre y correo.
2. El colaborador declara el vehículo del viaje (tipo, placa, tracción, modelo/año,
   cilindraje, combustible) — único para toda la boleta (RN-4).
3. El colaborador agrega uno o más viajes (cada uno con su fecha y su secuencia de
   tiendas), dentro de la ventana de mes en curso + dos meses anteriores (RN-6) y sin
   duplicar viajes ya registrados (RN-7).
4. El sistema calcula el costo por km de cada viaje (RN-1, RN-2, RN-3) y la distancia total
   sumando los tramos entre tiendas consecutivas.
5. El sistema arma el monto total de la boleta y la envía por correo a la jefatura
   correspondiente.
6. Mientras está pendiente, el colaborador puede seguir editándola o retirarla (RN-8).
7. La jefatura la aprueba completa (RN-9, RN-10).
8. Finanzas recibe la boleta aprobada con el monto, en tiempo real.
9. La boleta completa persiste 7 días desde que finanzas la recibe (REG-2); luego solo
   queda su resumen, hasta un año.

**Boleta bloqueada por falta de distancia:** en el paso 3–4, si falta la distancia entre dos
tiendas consecutivas de algún viaje, la boleta se bloquea, se notifica a administrador y
finanzas (RN-5), y el colaborador no puede enviarla hasta que se cargue el dato.

**Boleta rechazada:** en el paso 7, la jefatura rechaza con motivo obligatorio (RN-11); el
colaborador la corrige y la reenvía (RN-12), o si no lo hace en 7 días, se descarta (RN-13).

**Jefatura no responde:** si pasa el plazo configurado sin decisión, se manda un
recordatorio; si sigue sin decidir, se repite (RN-14).

**Cédula no reconocida:** en el paso 1, si el ERP de RH no reconoce la cédula (o
corresponde a alguien que ya no trabaja en la compañía), el sistema rechaza de entrada con
mensaje claro y no continúa el trámite (RN-17).

**Cambio de tarifa a mitad de trámite:** si el administrador actualiza la tabla de tarifas
mientras hay boletas pendientes, esas boletas conservan la tarifa con la que se calcularon
(RN-16).

**Jefatura, administrador o finanzas como colaboradores:** siguen el mismo recorrido
principal para su propio kilometraje (RN-18, RF-17); la única diferencia es a quién se le
asigna la aprobación, que nunca es la misma persona que la envió.

## Requisitos funcionales

1. RF-1: El sistema debe permitir a un colaborador iniciar una boleta y traer sus datos del
   ERP de RH a partir de la cédula asociada a su sesión ya autenticada (RN-17) — no de una
   cédula de texto libre, para que un colaborador no pueda armar una boleta a nombre de otra
   persona.
2. RF-2: El sistema debe permitir declarar un único vehículo por boleta (RN-4).
3. RF-3: El sistema debe permitir agregar uno o más viajes a una boleta, cada uno con fecha
   y secuencia de tiendas, validando la ventana de tiempo (RN-6) y la ausencia de
   duplicados (RN-7).
4. RF-4: El sistema debe calcular automáticamente el costo por km y el monto de cada viaje
   y de la boleta completa, sin permitir ingreso manual de tarifa o distancia (RN-1, RN-2,
   RN-3).
5. RF-5: El sistema debe bloquear el envío de una boleta si falta la distancia entre dos
   tiendas de algún viaje, y notificar a administrador y finanzas (RN-5).
6. RF-6: El sistema debe permitir al colaborador editar o retirar una boleta mientras esté
   pendiente de decisión (RN-8).
7. RF-7: El sistema debe enviar la boleta por correo a la jefatura correspondiente para su
   aprobación, y permitirle aprobar o rechazar la boleta completa desde el sistema (RN-9,
   RN-10).
8. RF-8: El sistema debe exigir un motivo al rechazar, comunicarlo al colaborador, y permitir
   la corrección y reenvío (RN-11, RN-12); debe descartar automáticamente una boleta
   rechazada no corregida en 7 días (RN-13).
9. RF-9: El sistema debe enviar recordatorios automáticos a la jefatura cuando no decide
   dentro del plazo configurado, repitiéndolos mientras siga sin decidir (RN-14).
10. RF-10: El sistema debe entregar a finanzas, en tiempo real, cada boleta aprobada con su
    monto (RN-10).
11. RF-11: El sistema debe impedir deshacer o anular una boleta ya aprobada y enviada a
    finanzas (RN-15).
12. RF-12: El sistema debe mantener la tarifa con la que se calculó una boleta aunque la
    tabla de tarifas cambie después, mientras la boleta siga pendiente (RN-16).
13. RF-13: El sistema debe permitir al administrador mantener la tabla de tarifas y la tabla
    de distancias entre tiendas.
14. RF-14: El sistema debe conservar el detalle completo de cada boleta 7 días desde que
    llega a finanzas, y después un resumen auditable por boleta durante una ventana móvil
    de 12 meses (REG-1, REG-2).
15. RF-15: El sistema debe permitir consultar, a partir del resumen, montos, volumen de
    boletas y tiempo promedio de trámite por jefatura y por periodo.
16. RF-16: El inicio de sesión de jefatura, administrador, finanzas y colaborador debe
    hacerse contra el mismo directorio corporativo (AD, simulado) que provee el correo
    institucional — sin una credencial separada propia del sistema.
17. RF-17: El sistema debe permitir que una jefatura, el administrador o finanzas armen,
    editen, envíen y retiren sus propias boletas, y consulten su propio historial, con las
    mismas reglas que un colaborador (RN-1 a RN-17); esas boletas las aprueba otra jefatura,
    nunca quien las envió (RN-18).
18. RF-18: El sistema debe permitir a una jefatura consultar, además de la cola de boletas
    pendientes de su decisión, el historial de boletas que ya aprobó.

## Requisitos no funcionales

1. RNF-1: El volumen esperado (~500 boletas/mes, ~1.000 colaboradores, ~60 jefaturas) es
   bajo y parejo, sin picos conocidos; el sistema no necesita diseñarse para alta
   concurrencia.
2. RNF-2: Una caída del sistema de hasta una hora no es crítica: no genera daño real más
   allá de que el colaborador reintente después. No se requiere alta disponibilidad.

## Dependencias

- ERP de RH de la compañía (simulado para la demo): provee los datos del colaborador a
  partir de su cédula.
- AD / directorio corporativo de la compañía (simulado para la demo): provee el correo
  institucional y el inicio de sesión único (SSO).

## Preguntas abiertas

Ninguna. La única pregunta abierta (PA-1, unidad del plazo de recordatorio) se resolvió:
días hábiles, incorporado a RN-14.
