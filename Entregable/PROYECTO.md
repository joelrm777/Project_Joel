# Boleta de Kilometraje Digital

**Joel Ramírez** · SINT-732 Laboratorio Ejecutivo en Claude Code
Entrega y presentación: martes 8 de septiembre de 2026, Sesión 8

> Este es mi enunciado del proyecto del curso. Reemplaza a los casos semilla de la
> consigna oficial; todo lo demás de esa consigna —el núcleo, la rúbrica de nueve
> criterios y las restricciones— aplica igual.


## 1. Qué es y para quién

Automatiza el trámite con el que hoy un colaborador cobra el kilometraje recorrido
entre tiendas de la compañía: hoy se hace a mano en una hoja de Excel que hay que
imprimir, firmar y llevar físicamente a la jefatura y luego a finanzas.

- **Quién lo usa:** alrededor de 1.000 colaboradores que cobran kilometraje, unas
  60 jefaturas que aprueban las boletas de su gente, un administrador que mantiene
  las tarifas y las distancias entre tiendas, y el equipo de finanzas, que recibe
  las boletas ya aprobadas con el monto a pagar.
- **Qué dispara el uso:** cuando un colaborador necesita cobrar el kilometraje de
  los viajes que hizo entre tiendas, para el mes en curso o los dos meses
  anteriores.

## 2. El eje de valor

**Eje declarado:** antes/después estimado.

Hoy el trámite se resuelve en papel: la persona llena la hoja de Excel, la
imprime, la firma, se la lleva a firmar a la jefatura, y luego la lleva a mano a
finanzas para que le paguen. Ese recorrido físico tarda alrededor de una semana
en procesarse, y produce pagos atrasados. El estudiante no midió esas horas ni
ese atraso con un instrumento; es su estimación de lo que ocurre hoy.

| | Hoy | Con el prototipo | Origen del número |
|---|---|---|---|
| Tiempo de trámite hasta que finanzas recibe la boleta | ~1 semana (papel, firmas físicas) | (a definir durante el proyecto) | estimado |
| Boletas procesadas por mes | ~500 | ~500 | estimado |

Sobre este eje se van a argumentar el criterio 1 —oportunidad— y el criterio 6
—hoja de ruta y retorno—. Los números finales se construyen durante el proyecto;
lo que queda fijo acá es contra qué se comparan.

## 3. El recorrido principal

1. El colaborador ingresa su número de cédula.
2. El sistema trae de un sistema externo (el ERP de RH de la compañía) los datos
   del colaborador: jefatura, departamento, puesto, nombre y correo.
3. El colaborador completa los datos del vehículo con el que viajó: tipo de
   transporte, placa, tracción, modelo (año), cilindraje y tipo de combustible.
4. El sistema calcula el costo por kilómetro según la tabla vigente de tarifas,
   cruzando tipo de vehículo, antigüedad **(supuesto: antigüedad = año actual −
   año del modelo)**, cilindraje y combustible.
5. El colaborador marca las tiendas por las que pasó en el viaje —puede ser un
   recorrido por varias tiendas seguidas, no necesariamente un viaje redondo entre
   dos—.
6. El sistema calcula la distancia total sumando los tramos entre tiendas
   consecutivas, usando la tabla de distancias registrada entre tiendas.
7. El sistema calcula el monto total (distancia × costo por km) y arma la boleta.
8. La boleta se envía por correo a la jefatura correspondiente para su
   aprobación.
9. Si la jefatura rechaza la boleta, se notifica al colaborador para que la
   corrija y la reenvíe.
10. Si la jefatura aprueba, la boleta —con el monto ya definido— llega al equipo
    de finanzas.
11. La boleta queda guardada en el sistema durante 7 días.

**Queda afuera a propósito:**
- Imprimir o firmar cualquier documento en papel.
- El pago en sí (la transferencia o el desembolso): el sistema llega hasta que
  finanzas recibe la boleta aprobada con el monto a cancelar.
- Una segunda aprobación por parte de finanzas: finanzas solo recibe el monto ya
  aprobado por la jefatura.

## 4. Las reglas que valen

1. El costo por kilómetro se calcula siempre con la tabla vigente de tarifas
   (tipo de vehículo, antigüedad, cilindraje, combustible); no se puede ingresar
   ni sobrescribir a mano.
2. No se puede completar ni enviar una boleta si falta la distancia registrada
   entre dos tiendas del recorrido: el sistema bloquea la boleta y avisa al
   administrador y a finanzas para que carguen el dato pendiente.
3. Una boleta no puede llegar a finanzas sin que la jefatura correspondiente al
   colaborador la haya aprobado explícitamente.
4. Una boleta rechazada por la jefatura vuelve al colaborador para que la
   corrija; no se reenvía a finanzas hasta que sea corregida y vuelta a aprobar.
5. La tracción del vehículo (sencilla, doble tracción, no aplica) es un dato
   informativo: no participa del cálculo del costo por kilómetro.

**La decisión difícil.** Qué hacer cuando falta la distancia entre dos tiendas
—el caso típico es una tienda que acaba de abrir—. Se consideraron dos caminos:
bloquear la boleta hasta que alguien cargue el dato, o dejar que el colaborador
ingrese la distancia a mano para ese viaje puntual y marcarla para revisión. El
estudiante eligió bloquear la boleta y notificar al administrador y a finanzas,
porque prefiere frenar el trámite a arriesgarse a pagar un monto calculado sobre
un dato inventado por el colaborador. Esto es lo que va a defender en la Sesión 8.

## 5. Los datos

- **Qué persiste:** las boletas (con sus tramos de viaje, el monto y su estado de
  aprobación), la tabla de tarifas por tipo de vehículo, y la tabla de distancias
  entre las 40 tiendas de la compañía. **(supuesto)** Los datos del colaborador no
  se guardan de forma permanente en el sistema: se consultan en el ERP de RH cada
  vez, y solo queda una copia dentro de cada boleta mientras esta existe.
- **De dónde salen para la demostración:** sintéticos. El ERP de RH y el
  directorio de correo (AD) son sistemas reales de la compañía, pero para poder
  mostrar el prototipo en clase sin datos confidenciales, ambos se simulan con
  una versión de prueba que se comporta igual.
- **Confidencialidad:** la cédula y el correo de cada colaborador son datos
  personales; no se puede usar información real de empleados de la compañía en la
  demostración.

## 6. Frontera técnica

- **Depende de:** el ERP de Recursos Humanos de la compañía (vía una API, para
  traer los datos del colaborador por cédula) y el directorio de la compañía —AD—
  (para el correo institucional al que se envían las aprobaciones).
- **Acceso real:** no, para la demostración ambos se simulan con datos de prueba;
  la conexión real queda configurable para cuando la compañía lo quiera adoptar.
- **Restricciones impuestas:** no usar datos reales ni confidenciales de
  colaboradores de la compañía en ningún momento del curso.

## 7. Qué debe ser cierto cuando entregue

1. **La oportunidad está comparada.** El tiempo de trámite de hoy (~1 semana en
   papel, con pagos atrasados) contra el tiempo del trámite digital, sobre ~500
   boletas mensuales.
2. **La arquitectura se decidió antes que el código.** Deberá quedar documentado
   cómo se organizan los módulos (cálculo de tarifas, gestión de distancias,
   flujo de aprobación, integración con el ERP y el AD simulados), sin decidirlo
   todavía en este documento.
3. **El prototipo funciona de extremo a extremo y persiste datos de verdad.** El
   recorrido completo descrito en la sección 3, con las boletas, tarifas y
   distancias guardadas en una base de datos real.
4. **Las reglas del negocio están cubiertas por pruebas que corren en cada
   push.** Las cinco reglas de la sección 4.
5. **El proceso de construcción quedó registrado.** CLAUDE.md, bitácora e
   historial de commits.
6. **La gobernanza quedó registrada en la bitácora.** Dado que el sistema mueve
   dinero de verdad, lo que siempre hay que revisar es que el monto calculado use
   la tarifa y la distancia correctas, y que ninguna boleta llegue a finanzas sin
   aprobación real de la jefatura.
7. **La decisión de adoptar está fundamentada.** Se defendería ante la jefatura
   de finanzas y ante RH de la compañía, que son quienes hoy cargan con el
   trámite en papel.
8. **La presentación defiende decisiones.** De 10 a 12 minutos, Sesión 8.

## 8. El núcleo en este proyecto

| Pieza | Cómo se cumple acá |
|---|---|
| Prototipo de extremo a extremo | El recorrido de la sección 3, de la cédula del colaborador hasta que finanzas recibe la boleta aprobada |
| Persistencia en base de datos real | Boletas, tabla de tarifas y tabla de distancias entre tiendas |
| Pruebas sobre las reglas del negocio | Las cinco reglas de la sección 4 |
| CLAUDE.md propio y bitácora con entradas de gobernanza | Aplica igual que a todos |
| Integración continua y skill de arranque | Aplica igual que a todos |
| Los dos documentos | Aplica igual que a todos |

**Excepciones abiertas.** Ninguna: las cinco piezas del núcleo tienen una
equivalencia clara en este caso.

## 9. Calendario

Horas disponibles por semana: **no definidas por el estudiante (ver bandera en
la ficha)**. Semanas hasta la entrega: **este enunciado se define el mismo día
en que el calendario oficial marca la Sesión 8 de entrega; el calendario de
cinco semanas de la consigna no aplica tal cual a este caso.**

| Para la sesión | Qué tengo que tener listo |
|---|---|
| 4 · 11 de agosto | (a resolver con el docente, dado el desfase de calendario) |
| 5 · 18 de agosto | (a resolver con el docente) |
| 6 · 25 de agosto | (a resolver con el docente) |
| 7 · 1.º de septiembre | (a resolver con el docente) |
| 8 · 8 de septiembre | Repositorio completo y presentación |

## 10. Supuestos declarados

- La antigüedad del vehículo para efectos de la tarifa se calcula como año actual
  menos año del modelo.
- Los datos del colaborador no se persisten de forma permanente; se consultan al
  ERP de RH cada vez y solo viven dentro de la boleta mientras esta existe (7
  días).
- El ERP de RH y el AD se simulan para la demostración, con la integración real
  dejada configurable para más adelante.
- El envío de las notificaciones de aprobación y de boleta pendiente por
  distancia faltante se simula (no se conecta a un servidor de correo real de la
  compañía) para efectos de la demostración.
- No se definió un número de horas semanales disponibles; queda como pendiente
  crítico frente al calendario real de entrega.

## 11. Lo que este documento no decide

A propósito. Estas decisiones son mías y llegan después:

- La arquitectura: módulos, responsabilidades y contratos entre ellos.
- El modelo de datos.
- El stack: lenguaje, framework y motor de base de datos.

El criterio 2 de la rúbrica evalúa exactamente estas decisiones, así que tomarlas
temprano y sin fundamento no adelanta nada.
