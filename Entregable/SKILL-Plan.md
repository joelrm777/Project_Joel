---
name: escribir-plan
description: Use when a project has an approved ESPECIFICACION.md and DISENO.md and the next step is building — produces PLAN.md, the construction plan: an ordered list of vertical slices, each with its acceptance conditions and a concrete check written before any code exists. Triggers on "plan de construcción", "escribir el plan", "PLAN.md", "planear la construcción", or when a design is approved and the user wants to start building.
---

# Escribir el plan de construcción

De un diseño aprobado a un plan ejecutable: `PLAN.md`, la lista ordenada de piezas con la
comprobación de cada una escrita antes de construirla. Sin escribir código.

Sigue el estándar de `writing-plans` (Superpowers), adaptado: **el plan se escribe para un
implementador con cero contexto** — cada pieza tiene que poder ejecutarse en una conversación
nueva que solo leyó los documentos y esa pieza. La adaptación del curso: el plan corta en
rebanadas y define la comprobación; el paso a paso interno de cada pieza es del agente que la
construya, dirigido por quien conduce.

**Anunciar al arrancar:** «Estoy usando la habilidad escribir-plan para producir el plan de
construcción.»

---

## Proceso

1. Leer `ESPECIFICACION.md` y `DISENO.md`. Si falta alguno, detenerse y decirlo: el plan se
   deriva de esos documentos, no los reemplaza.
2. Si la especificación tiene preguntas abiertas sin resolver, listarlas antes de planear.
   Una pieza afectada por una pregunta abierta queda marcada con el número de la pregunta.
3. Derivar las piezas como **rebanadas verticales** (ver reglas de corte) y proponer la lista
   con su orden y la razón del orden. Someterla a aprobación antes de detallar.
4. Detallar cada pieza: qué tiene que ser cierto cuando está lista, y con qué se comprueba.
   Presentar el detalle completo y ajustar hasta que se apruebe.
5. **PUNTO DE CONTROL:** escribir `PLAN.md`, revisarlo con las pasadas de revisión y someterlo
   a la compuerta del usuario.

**Estado terminal:** el plan escrito y revisado. NO construir ninguna pieza, ni crear
andamiaje de proyecto, ni invocar habilidades de implementación.

---

## Reglas de corte

- **Cada pieza es una rebanada vertical**: un recorrido del negocio atravesado entero, de la
  vista al dato guardado. Una pieza que se enuncia como una capa —«toda la base de datos»,
  «todas las vistas»— está mal cortada.
- **La primera pieza es el recorrido completo más simple.** El arranque del proyecto (crear el
  repositorio de código, la base vacía, la aplicación que levanta) va dentro de esa primera
  pieza, no como pieza propia: su comprobación es el recorrido funcionando, no «el proyecto
  compila».
- **Una pieza se construye en una conversación.** Es la unidad de administración del tiempo y
  del contexto: si el detalle de una pieza no cabe en una sesión de trabajo enfocada, se parte
  en dos. Piezas chicas mantienen el contexto corto y el costo bajo.
- **El orden**: primero las dependencias reales —lo que otras piezas necesitan construido—;
  entre piezas independientes, primero la que despeja más incógnitas del diseño.
- **Cobertura completa**: cada requisito funcional y cada recorrido de la especificación cae
  en alguna pieza, o queda listado en «Fuera del plan» con su razón. Nada desaparece en
  silencio.

## La comprobación de cada pieza

- **Se escribe antes de construir.** Escrita después se acomoda a lo construido, y entonces no
  comprueba nada.
- **Es concreta y observable**: una acción y su resultado esperado. Una llamada al servicio y
  la respuesta que debe dar —incluido el caso que falla—; el recorrido en la vista con sus
  estados; los datos que siguen ahí después de reiniciar.
- **Se puede correr sin las piezas siguientes.** Si la comprobación de la pieza 2 necesita la
  pieza 5, el orden está mal o el corte está mal.
- Si el proyecto tiene pruebas automatizadas, correrlas forma parte de la comprobación; la
  evidencia mínima sigue siendo observable sin ellas.

---

## Plantilla de PLAN.md

Las secciones sin contenido se borran; no se rellenan con «no aplica».

```markdown
# Plan de construcción: <nombre del sistema>

**Objetivo:** [una frase: qué queda construido cuando el plan termina]

**Arquitectura:** [dos o tres frases, resumidas de DISENO.md — no decisiones nuevas]

**Stack:** [las tecnologías que DISENO.md ya eligió, copiadas de ahí. Si una quedó sin
elegir, va como pendiente ligado a la decisión abierta del diseño — el plan no la decide]

**Restricciones globales:** [las restricciones de alcance que aplican a todas las piezas,
una línea cada una, copiadas textuales de la especificación]

## Cómo usar este plan
- Una pieza por conversación. Al cerrar la pieza, cerrar también la conversación: el
  contexto arranca limpio y barato en la siguiente.
- El encargo de cada pieza referencia ESPECIFICACION.md y DISENO.md; no los repite.
- Una pieza queda cerrada cuando su comprobación se corrió y el resultado quedó anotado
  en su Evidencia.
- Lo que la construcción revele que falta en la especificación o el diseño se corrige
  primero en ese documento, y después en el código.

## Piezas
| # | Pieza | Depende de | Estado |
|---|---|---|---|
| 1 | ... | — | pendiente |

## Detalle

### Pieza 1: <nombre, en lenguaje del negocio>
**Qué tiene que ser cierto**
- ...

**Con qué se comprueba**
- ...

**Toca** *(componentes del diseño involucrados)*: ...

**Interfaces**
- Consume: [lo que esta pieza usa de piezas anteriores — nombres y formas exactos]
- Produce: [lo que piezas posteriores van a usar de esta — nombres y formas exactos.
  Quien construya una pieza solo lee la suya; este bloque es cómo se entera de los
  nombres que las vecinas esperan]

**Evidencia** *(vacía al escribir el plan; se llena al cerrar la pieza, con fecha)*

## Cobertura *(si aplica)*
| Requisito o recorrido | Pieza |
|---|---|

## Fuera del plan
Lo que la especificación o el diseño describen y este plan no construye, con la razón.
```

---

## Sin marcadores

Estas frases son **fallas del plan**; no se escriben nunca:

- «por definir», «pendiente», «se detalla después»
- «manejar los errores apropiadamente», «validar lo que corresponda», «cubrir los casos borde»
- «similar a la pieza N» — se escribe completo: quien construye una pieza no leyó las otras
- una comprobación que describe una intención en vez de una acción con resultado

Lo único que legítimamente queda abierto es un pendiente ligado por número a una decisión
abierta de `DISENO.md` o a una pregunta abierta de `ESPECIFICACION.md`.

## Revisión del plan escrito

Después de escribir el archivo, revisarlo con ojos nuevos y corregir sobre la marcha:

1. **Cortes.** Ninguna pieza es una capa disfrazada, y ninguna comprobación necesita piezas
   posteriores.
2. **Comprobaciones.** Cada una es una acción con resultado observable, no una intención
   («funciona bien» no se puede correr).
3. **Cobertura.** Recorrer la especificación sección por sección: cada requisito y cada
   recorrido tiene pieza o está en «Fuera del plan». Listar los huecos y cerrarlos.
4. **Marcadores.** Buscar las frases de la sección anterior. Si aparece una, corregirla.
5. **Consistencia de nombres.** Los componentes y el vocabulario son los de los documentos, y
   lo que una pieza «Produce» coincide exactamente con lo que otra «Consume» — el mismo nombre
   con dos formas en dos piezas es un defecto del plan, no del que construye.

### Compuerta de revisión del usuario

> «Escribí `PLAN.md`. Revisalo y decime si querés cambiar algo.»

Esperar la respuesta. Si pide cambios, hacerlos y volver a revisar. Terminar solo con la
aprobación.

---

## Reglas duras

- **No se escribe código, no se crea andamiaje, no se construye ninguna pieza.** El estado
  terminal es `PLAN.md` escrito y revisado.
- **Rebanadas, no capas.** Sin excepciones: el corte por capa es el error que esta habilidad
  existe para impedir.
- **El plan no re-decide el diseño.** Si al planear aparece una decisión de diseño sin tomar,
  se pregunta o se anota como pendiente ligada al documento; no se resuelve en silencio.
- **Toda comprobación se escribe antes y es observable.**
- **La plantilla no es un formulario.** Un plan largo porque el sistema es grande está bien;
  largo por rellenar secciones, no.
- **No hacer commit por cuenta propia.**
