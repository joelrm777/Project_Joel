# Ficha de aprobación · Boleta de Kilometraje Digital

**Joel Ramírez** · SINT-732 · 8 de septiembre de 2026

**Qué es.** Automatiza el trámite con el que hoy un colaborador cobra el
kilometraje recorrido entre tiendas, hoy resuelto a mano en Excel, impreso y
firmado en papel.

**Cómo se hace hoy.** Excel manual, impreso, firmado por el colaborador y la
jefatura, y llevado a mano a finanzas.

---

## Lo esencial

| | |
|---|---|
| **Eje de valor** | Antes/después estimado |
| **La comparación** | De ~1 semana de trámite físico con pagos atrasados, a un trámite digital con aprobación por correo, sobre ~500 boletas al mes |
| **Recorrido principal** | Parte de la cédula del colaborador; termina cuando finanzas recibe la boleta aprobada con el monto a cancelar |
| **Queda afuera** | Impresión y firma física, el pago en sí (transferencia), y una segunda aprobación de finanzas |
| **La decisión difícil** | Cuando falta la distancia entre dos tiendas, el sistema bloquea la boleta y notifica al administrador y a finanzas, en vez de dejar que el colaborador la ingrese a mano |
| **Datos para la demo** | Sintéticos: el ERP de RH y el AD de la compañía se simulan |
| **Horas por semana** | No definidas por el estudiante |
| **Punto de partida** | Desde cero |

## El núcleo

| Pieza | | Cómo se cumple acá |
|---|---|---|
| Prototipo de extremo a extremo | ✔ | De la cédula del colaborador hasta que finanzas recibe la boleta aprobada |
| Persistencia en base de datos real | ✔ | Boletas, tarifas por tipo de vehículo, distancias entre las 40 tiendas |
| Pruebas sobre las reglas del negocio | ✔ | Cálculo de tarifa, bloqueo por distancia faltante, aprobación de jefatura obligatoria, reenvío tras rechazo |
| CLAUDE.md y bitácora con entradas de gobernanza | ✔ | Aplica igual que a todos |
| Integración continua y skill de arranque | ✔ | Aplica igual que a todos |
| Los dos documentos | ✔ | Aplica igual que a todos |

## Banderas

- **Dependencias externas simuladas.** El ERP de RH y el AD de la compañía son
  sistemas reales, pero para esta demostración se simulan con datos de prueba,
  ya que no se puede mostrar en clase información real de colaboradores.

## Supuestos que quedaron declarados

- La antigüedad del vehículo para la tarifa se calcula como año actual menos
  año del modelo.
- Los datos del colaborador no se guardan de forma permanente; se consultan al
  ERP de RH cada vez y solo viven dentro de la boleta mientras esta existe (7
  días).
- El envío de notificaciones (aprobación, boleta bloqueada por distancia
  faltante) se simula, sin conectarse a un servidor de correo real.

## Preguntas para el docente

- ¿Cómo se resuelve la entrega dado que la definición del caso se está haciendo
  el mismo día que el calendario oficial marca como fecha de entrega?

---

*Enunciado completo en `PROYECTO.md`.*
